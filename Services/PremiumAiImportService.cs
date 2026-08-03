using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public sealed record AiImportReview(
    AiProcessingJob Job,
    AiSuggestionSet SuggestionSet,
    AiImportProposal? ImportProposal,
    AiChecklistStructureProposal? ChecklistProposal);

public sealed class PremiumAiImportService
{
    public const string FeatureKey = "premium-ai-import-intelligence";
    public const string MappingSchemaVersion = "register-mapping-v1";
    public const string ChecklistSchemaVersion = "checklist-draft-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly VectorDbContext _db;
    private readonly IFeatureAccessService _features;
    private readonly IUserActionPermissionService _permissions;
    private readonly IImportFieldRegistry _fieldRegistry;
    private readonly IImportTabularReader _reader;
    private readonly IFileStorageService _files;
    private readonly IAiStructuredOutputProvider _provider;
    private readonly IDocumentExtractionProvider _documents;
    private readonly IAiPromptRegistry _prompts;
    private readonly IAiRedactionService _redaction;
    private readonly IAiSourceSafetyService _sourceSafety;
    private readonly PremiumAiOptions _options;
    private readonly ChecklistImportConversionService _checklists;

    public PremiumAiImportService(
        VectorDbContext db,
        IFeatureAccessService features,
        IUserActionPermissionService permissions,
        IImportFieldRegistry fieldRegistry,
        IImportTabularReader reader,
        IFileStorageService files,
        IAiStructuredOutputProvider provider,
        IDocumentExtractionProvider documents,
        IAiPromptRegistry prompts,
        IAiRedactionService redaction,
        IAiSourceSafetyService sourceSafety,
        Microsoft.Extensions.Options.IOptions<PremiumAiOptions> options,
        ChecklistImportConversionService checklists)
    {
        _db = db;
        _features = features;
        _permissions = permissions;
        _fieldRegistry = fieldRegistry;
        _reader = reader;
        _files = files;
        _provider = provider;
        _documents = documents;
        _prompts = prompts;
        _redaction = redaction;
        _sourceSafety = sourceSafety;
        _options = options.Value;
        _checklists = checklists;
    }

    public async Task<bool> CanUseAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        return HasManagerRole(user.AppRole?.Name)
            && await _features.CanUseFeatureAsync(VectorFeatures.AiImportIntelligence, cancellationToken)
            && await _permissions.HasPermissionAsync(user, UserActionPermissions.ImportsAiAssist, cancellationToken)
            && await _db.CompanyAiUsagePolicies.AsNoTracking().AnyAsync(policy =>
                policy.CompanyId == user.CompanyId &&
                policy.MonthlyHardLimitUsd > 0 &&
                policy.PerJobLimitUsd > 0 &&
                policy.MaxConcurrentJobs > 0 &&
                policy.EnabledFeaturesJson.Contains(FeatureKey), cancellationToken);
    }

    public async Task<AiImportReview?> GetLatestReviewAsync(AppUser user, int importBatchId, CancellationToken cancellationToken = default)
    {
        var job = await _db.AiProcessingJobs
            .AsNoTracking()
            .Include(item => item.SuggestionSets).ThenInclude(set => set.Suggestions)
            .Where(job => job.CompanyId == user.CompanyId && job.ImportBatchId == importBatchId)
            .OrderByDescending(job => job.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var set = job?.SuggestionSets.OrderByDescending(item => item.CreatedAtUtc).FirstOrDefault();
        if (job is null || set is null) return null;
        var import = await _db.AiImportProposals.AsNoTracking().FirstOrDefaultAsync(proposal =>
            proposal.CompanyId == user.CompanyId && proposal.AiSuggestionSetId == set.Id, cancellationToken);
        var checklist = await _db.AiChecklistStructureProposals.AsNoTracking().FirstOrDefaultAsync(proposal =>
            proposal.CompanyId == user.CompanyId && proposal.AiSuggestionSetId == set.Id, cancellationToken);
        return new AiImportReview(job, set, import, checklist);
    }

    public async Task<AiImportReview> RequestAsync(
        AppUser user,
        int importBatchId,
        bool noPatientDataConfirmed,
        CancellationToken cancellationToken = default)
    {
        await RequireAccessAsync(user, cancellationToken);
        if (!noPatientDataConfirmed)
            throw new InvalidOperationException("Confirm that the source contains no patient-identifiable clinical data before using AI assistance.");
        var batch = await _db.ImportBatches
            .Include(item => item.SourceAssetFile)
            .Include(item => item.ColumnMappings)
            .SingleOrDefaultAsync(item =>
                item.Id == importBatchId &&
                item.CompanyId == user.CompanyId &&
                item.SourceAssetFile != null &&
                item.SourceAssetFile.CompanyId == user.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("The import batch was not found for this company.");
        if (batch.Status is ImportBatchStatuses.Committed or ImportBatchStatuses.RolledBack or ImportBatchStatuses.PartiallyRolledBack)
            throw new InvalidOperationException("AI assistance cannot change a completed or removed import.");

        var schema = string.Equals(batch.TargetType, ImportTargetTypes.Checklist, StringComparison.OrdinalIgnoreCase)
            ? ChecklistJsonSchema
            : MappingJsonSchema;
        var schemaVersion = string.Equals(batch.TargetType, ImportTargetTypes.Checklist, StringComparison.OrdinalIgnoreCase)
            ? ChecklistSchemaVersion
            : MappingSchemaVersion;
        var existingJob = await _db.AiProcessingJobs
            .AsNoTracking()
            .Where(job =>
                job.CompanyId == user.CompanyId &&
                job.ImportBatchId == batch.Id &&
                job.InputHash == batch.FileHash &&
                job.PromptVersion == _prompts.PromptVersion &&
                job.SchemaVersion == schemaVersion &&
                (job.Status == AiProcessingStatuses.Queued ||
                 job.Status == AiProcessingStatuses.Running ||
                 job.Status == AiProcessingStatuses.AwaitingReview))
            .OrderByDescending(job => job.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingJob is not null)
        {
            var existingReview = await GetLatestReviewAsync(user, batch.Id, cancellationToken);
            if (existingReview?.Job.Id == existingJob.Id) return existingReview;
            throw new InvalidOperationException("This import is already being processed.");
        }

        var policy = await RequireBudgetAsync(user.CompanyId, cancellationToken);
        var source = await BuildSourceAsync(batch, cancellationToken);
        var reservedCost = EstimateCost(
            Math.Max(1, source.Length / 4),
            Math.Clamp(_options.MaximumOutputTokens, 256, 8000));
        var currentMonthCost = await CurrentMonthCostAsync(user.CompanyId, cancellationToken);
        if (reservedCost > policy.PerJobLimitUsd || currentMonthCost + reservedCost > policy.MonthlyHardLimitUsd)
            throw new InvalidOperationException("This request would exceed the company AI per-job or monthly hard limit.");
        if (policy.MonthlySoftLimitUsd > 0 && currentMonthCost + reservedCost >= policy.MonthlySoftLimitUsd)
        {
            _db.AuditLogs.Add(Audit(user, "Premium AI monthly soft limit warning", nameof(CompanyAiUsagePolicy), policy.Id,
                $"Estimated monthly AI usage reached the configured soft limit before job creation. Hard limits remain enforced."));
        }
        var job = new AiProcessingJob
        {
            CompanyId = user.CompanyId,
            RequestedByUserId = user.Id,
            ImportBatchId = batch.Id,
            FeatureKey = FeatureKey,
            SourceType = batch.SourceAssetFile!.ContentType,
            InputHash = batch.FileHash,
            Provider = "AzureOpenAI",
            Deployment = _options.OpenAiDeployment,
            Model = _options.OpenAiModel,
            PromptVersion = _prompts.PromptVersion,
            SchemaVersion = schemaVersion,
            Status = AiProcessingStatuses.Running,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.AiProcessingJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);
        _db.AuditLogs.Add(Audit(user, "Premium AI source privacy confirmed", nameof(ImportBatch), batch.Id,
            $"Job {job.Id}: patient-identifiable data exclusion confirmed; staff source values remain outside the AI prompt."));

        Exception? lastFailure = null;
        var maximumAttempts = Math.Clamp(_options.MaximumAttempts, 1, 3);
        for (var attemptNumber = 1; attemptNumber <= maximumAttempts; attemptNumber++)
        {
            var attempt = new AiJobAttempt
            {
                CompanyId = user.CompanyId,
                AiProcessingJobId = job.Id,
                AttemptNumber = attemptNumber,
                Status = AiProcessingStatuses.Running,
                StartedAtUtc = DateTime.UtcNow
            };
            _db.AiJobAttempts.Add(attempt);
            job.AttemptCount = attemptNumber;
            await _db.SaveChangesAsync(cancellationToken);
            try
            {
                var result = await _provider.CompleteAsync(new AiStructuredOutputRequest(
                    string.Equals(batch.TargetType, ImportTargetTypes.Checklist, StringComparison.OrdinalIgnoreCase)
                        ? _prompts.ChecklistSystemPrompt
                        : _prompts.MappingSystemPrompt,
                    source,
                    string.Equals(batch.TargetType, ImportTargetTypes.Checklist, StringComparison.OrdinalIgnoreCase)
                        ? "acuityops_checklist_import"
                        : "acuityops_register_mapping",
                    schema,
                    job.CorrelationId), cancellationToken);
                var cost = EstimateCost(result.InputTokens, result.OutputTokens);
                if (cost > policy.PerJobLimitUsd)
                    throw new InvalidOperationException("The AI result exceeded this company's per-job cost limit and was not offered for review.");
                var monthCost = await CurrentMonthCostAsync(user.CompanyId, cancellationToken);
                if (monthCost + cost > policy.MonthlyHardLimitUsd)
                    throw new InvalidOperationException("The company AI monthly hard limit has been reached.");

                attempt.ProviderRequestId = result.ProviderRequestId;
                attempt.InputTokens = result.InputTokens;
                attempt.OutputTokens = result.OutputTokens;
                attempt.EstimatedCostUsd = cost;
                attempt.Status = AiProcessingStatuses.Completed;
                attempt.CompletedAtUtc = DateTime.UtcNow;
                _db.AiUsageLedgers.Add(new AiUsageLedger
                {
                    CompanyId = user.CompanyId,
                    AiProcessingJobId = job.Id,
                    FeatureKey = FeatureKey,
                    Provider = result.Provider,
                    Model = result.Model,
                    InputTokens = result.InputTokens,
                    OutputTokens = result.OutputTokens,
                    EstimatedCostUsd = cost,
                    RecordedAtUtc = DateTime.UtcNow
                });
                var review = string.Equals(batch.TargetType, ImportTargetTypes.Checklist, StringComparison.OrdinalIgnoreCase)
                    ? PersistChecklistProposal(user, batch, job, result.Json)
                    : PersistMappingProposal(user, batch, job, result.Json);
                job.Status = AiProcessingStatuses.AwaitingReview;
                job.CompletedAtUtc = DateTime.UtcNow;
                _db.AuditLogs.Add(Audit(user, "Premium AI import suggestions generated", nameof(ImportBatch), batch.Id,
                    $"Job {job.Id} generated reviewable suggestions. No register, checklist, login, assignment or publication record was created."));
                await _db.SaveChangesAsync(cancellationToken);
                return review;
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                lastFailure = ex;
                attempt.Status = AiProcessingStatuses.Failed;
                attempt.FailureCode = ex is JsonException ? "SchemaRejected" : "ProviderFailure";
                attempt.FailureSummary = SafeFailure(ex);
                attempt.CompletedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        job.Status = AiProcessingStatuses.Failed;
        job.FailureCode = "BoundedAttemptsExhausted";
        job.FailureSummary = SafeFailure(lastFailure);
        job.CompletedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(Audit(user, "Premium AI import assistance failed", nameof(ImportBatch), batch.Id,
            $"Job {job.Id} failed safely. Deterministic guided import remains available."));
        await _db.SaveChangesAsync(cancellationToken);
        throw new InvalidOperationException("AI assistance could not produce a safe structured result. Continue with guided import.");
    }

    public async Task ReviewSuggestionAsync(
        AppUser user,
        int suggestionId,
        string decision,
        string? correctedValue,
        string? note,
        CancellationToken cancellationToken = default)
    {
        await RequireAccessAsync(user, cancellationToken);
        if (!AiHumanDecisions.All.Contains(decision))
            throw new InvalidOperationException("Choose Accept, Correct, Reject, or Defer.");
        var suggestion = await _db.AiSuggestions
            .Include(item => item.AiSuggestionSet)!.ThenInclude(set => set!.AiProcessingJob)
            .SingleOrDefaultAsync(item =>
                item.Id == suggestionId &&
                item.CompanyId == user.CompanyId &&
                item.AiSuggestionSet!.CompanyId == user.CompanyId &&
                item.AiSuggestionSet.AiProcessingJob!.CompanyId == user.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("The AI suggestion was not found for this company.");
        if (!string.Equals(suggestion.Status, AiSuggestionStatuses.PendingReview, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(suggestion.Status, AiSuggestionStatuses.Deferred, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This suggestion already has a final review decision.");
        if (decision == AiHumanDecisions.Correct && string.IsNullOrWhiteSpace(correctedValue))
            throw new InvalidOperationException("Enter the corrected value.");
        if (suggestion.Confidence < 0.70m
            && decision is (AiHumanDecisions.Accept or AiHumanDecisions.Correct)
            && string.IsNullOrWhiteSpace(note))
            throw new InvalidOperationException("A reviewer note is required for low-confidence suggestions.");

        var finalValue = decision == AiHumanDecisions.Correct ? correctedValue!.Trim() : ReadProposedString(suggestion.ProposedValueJson);
        if (suggestion.Kind == "ColumnMapping" && decision is AiHumanDecisions.Accept or AiHumanDecisions.Correct)
        {
            if (_fieldRegistry.FindField(finalValue) is null)
                throw new InvalidOperationException("The selected correction is not a canonical AcuityOps field.");
            var job = suggestion.AiSuggestionSet!.AiProcessingJob!;
            var mapping = await _db.ImportColumnMappings.SingleOrDefaultAsync(item =>
                item.CompanyId == user.CompanyId &&
                item.ImportBatchId == job.ImportBatchId &&
                item.SourceColumnIndex.ToString() == suggestion.SourceLocator, cancellationToken)
                ?? throw new InvalidOperationException("The deterministic source column is unavailable.");
            mapping.TargetFieldKey = finalValue;
            mapping.SuggestionReason = $"AI suggestion reviewed by {user.FullName}.";
            mapping.IsIgnored = false;
            mapping.IsUserConfirmed = false;
        }
        else if (suggestion.Kind == "ChecklistStructure" && decision is AiHumanDecisions.Accept or AiHumanDecisions.Correct)
        {
            var proposal = await _db.AiChecklistStructureProposals.SingleAsync(item =>
                item.CompanyId == user.CompanyId && item.AiSuggestionSetId == suggestion.AiSuggestionSetId, cancellationToken);
            var draft = ParseChecklistDraft(proposal.StructureJson, decision == AiHumanDecisions.Correct ? correctedValue : null);
            await _checklists.PrepareSuggestedDraftAsync(user, proposal.ImportBatchId, draft, cancellationToken);
            proposal.Status = AiSuggestionStatuses.Applied;
        }

        suggestion.Status = decision switch
        {
            AiHumanDecisions.Accept => AiSuggestionStatuses.Accepted,
            AiHumanDecisions.Correct => AiSuggestionStatuses.Corrected,
            AiHumanDecisions.Reject => AiSuggestionStatuses.Rejected,
            _ => AiSuggestionStatuses.Deferred
        };
        _db.AiHumanDecisions.Add(new AiHumanDecision
        {
            CompanyId = user.CompanyId,
            AiSuggestionId = suggestion.Id,
            Decision = decision,
            CorrectedValueJson = decision == AiHumanDecisions.Correct ? JsonSerializer.Serialize(finalValue) : null,
            ReviewNote = note?.Trim(),
            ReviewedByUserId = user.Id,
            ReviewedAtUtc = DateTime.UtcNow
        });
        suggestion.AiSuggestionSet!.ReviewedByUserId = user.Id;
        suggestion.AiSuggestionSet.ReviewedAtUtc = DateTime.UtcNow;
        var hasUnresolvedSuggestions = await _db.AiSuggestions.AnyAsync(item =>
            item.CompanyId == user.CompanyId &&
            item.AiSuggestionSetId == suggestion.AiSuggestionSetId &&
            item.Id != suggestion.Id &&
            (item.Status == AiSuggestionStatuses.PendingReview || item.Status == AiSuggestionStatuses.Deferred),
            cancellationToken);
        suggestion.AiSuggestionSet.Status =
            decision == AiHumanDecisions.Defer || hasUnresolvedSuggestions
                ? AiSuggestionStatuses.PendingReview
                : AiSuggestionStatuses.Reviewed;
        _db.AuditLogs.Add(Audit(user, $"AI suggestion {decision.ToLowerInvariant()}", nameof(AiSuggestion), suggestion.Id,
            $"Suggestion {suggestion.Id} was {decision.ToLowerInvariant()} by a human reviewer. Final Block 5 validation and commit remain required."));
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RequireAccessAsync(AppUser user, CancellationToken cancellationToken)
    {
        if (!HasManagerRole(user.AppRole?.Name))
            throw new UnauthorizedAccessException("Staff users cannot request or review AI import suggestions.");
        if (!await _features.CanUseFeatureAsync(VectorFeatures.AiImportIntelligence, cancellationToken))
            throw new UnauthorizedAccessException("Premium AI Import Intelligence requires the Premium plan.");
        if (!await _permissions.HasPermissionAsync(user, UserActionPermissions.ImportsAiAssist, cancellationToken))
            throw new UnauthorizedAccessException("You do not have permission to request or review AI import suggestions.");
        if (!_provider.IsConfigured)
            throw new InvalidOperationException("Premium AI is not configured for this environment. Guided import remains available.");
    }

    private async Task<CompanyAiUsagePolicy> RequireBudgetAsync(int companyId, CancellationToken cancellationToken)
    {
        var policy = await _db.CompanyAiUsagePolicies.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CompanyId == companyId, cancellationToken)
            ?? throw new UnauthorizedAccessException("AI usage is disabled until a company AI usage policy is approved.");
        IReadOnlyList<string> features;
        try { features = JsonSerializer.Deserialize<List<string>>(policy.EnabledFeaturesJson, JsonOptions) ?? []; }
        catch (JsonException) { features = []; }
        if (!features.Contains(FeatureKey, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Premium AI Import Intelligence is not enabled for this company.");
        var active = await _db.AiProcessingJobs.CountAsync(job =>
            job.CompanyId == companyId &&
            (job.Status == AiProcessingStatuses.Queued || job.Status == AiProcessingStatuses.Running), cancellationToken);
        if (active >= policy.MaxConcurrentJobs) throw new InvalidOperationException("The company AI concurrency limit has been reached.");
        if (await CurrentMonthCostAsync(companyId, cancellationToken) >= policy.MonthlyHardLimitUsd)
            throw new InvalidOperationException("The company AI monthly hard limit has been reached.");
        return policy;
    }

    private static bool HasManagerRole(string? roleName) =>
        CurrentUserService.IsSeniorAccessRole(roleName) ||
        string.Equals(roleName, "Operational Management", StringComparison.OrdinalIgnoreCase);

    private async Task<decimal> CurrentMonthCostAsync(int companyId, CancellationToken cancellationToken)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var costs = await _db.AiUsageLedgers.AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.RecordedAtUtc >= start)
            .Select(item => item.EstimatedCostUsd)
            .ToListAsync(cancellationToken);
        return costs.Sum();
    }

    private decimal EstimateCost(int inputTokens, int outputTokens) =>
        Math.Round(
            inputTokens / 1_000_000m * _options.EstimatedInputCostPerMillionTokensUsd +
            outputTokens / 1_000_000m * _options.EstimatedOutputCostPerMillionTokensUsd, 6);

    private async Task<string> BuildSourceAsync(ImportBatch batch, CancellationToken cancellationToken)
    {
        var target = _fieldRegistry.FindTarget(batch.TargetType);
        var canonical = target?.Fields.Select(field => new
        {
            field.Key, field.Label, field.DataType, field.IsRequired, field.Aliases, field.IsDuplicateKey
        }) ?? [];
        var extension = Path.GetExtension(batch.SourceAssetFile!.OriginalFileName);
        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(batch.TargetType, ImportTargetTypes.Checklist, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PDF extraction is supported only for checklist imports.");
            if (!_documents.IsConfigured)
                throw new InvalidOperationException("Checklist PDF extraction is not configured. Spreadsheet checklist import remains available.");
            await using var stream = await _files.OpenReadAsync(batch.SourceAssetFile.StoragePath, cancellationToken);
            var extracted = await _documents.ExtractLayoutAsync(stream, batch.SourceAssetFile.ContentType, cancellationToken);
            RejectProhibitedPatientData(extracted.Markdown);
            return JsonSerializer.Serialize(new
            {
                source = new { fileName = _redaction.Minimize(batch.OriginalFileName), batch.FileHash, type = "PDF", warnings = extracted.Warnings },
                extractedMarkdown = _redaction.Minimize(extracted.Markdown)
            }, JsonOptions);
        }

        if (batch.HeaderRowNumber is null || batch.ColumnMappings.Count == 0)
            throw new InvalidOperationException("Read the source columns before requesting AI mapping assistance.");
        var tabular = await _reader.ReadAsync(batch.SourceAssetFile, batch.SelectedWorksheet, batch.HeaderRowNumber.Value, cancellationToken);
        foreach (var column in tabular.Columns) RejectProhibitedPatientData(column.Heading);
        var omitSourceValues = string.Equals(batch.TargetType, ImportTargetTypes.Staff, StringComparison.OrdinalIgnoreCase);
        if (!omitSourceValues)
        {
            foreach (var sample in tabular.Columns.SelectMany(column => column.Samples.Take(3)))
                RejectProhibitedPatientData(sample);
        }
        return JsonSerializer.Serialize(new
        {
            source = new
            {
                fileName = omitSourceValues ? "staff-import" : _redaction.Minimize(batch.OriginalFileName),
                batch.FileHash,
                batch.TargetType,
                worksheet = omitSourceValues ? "staff-worksheet" : _redaction.Minimize(tabular.Worksheet),
                batch.ParserContractVersion
            },
            columns = tabular.Columns.Select(column => new
            {
                column.Index,
                heading = _redaction.Minimize(column.Heading),
                samples = omitSourceValues
                    ? Array.Empty<string>()
                    : column.Samples.Take(3).Select(_redaction.Minimize)
            }),
            canonicalFields = canonical
        }, JsonOptions);
    }

    private AiImportReview PersistMappingProposal(AppUser user, ImportBatch batch, AiProcessingJob job, string json)
    {
        var response = JsonSerializer.Deserialize<MappingResponse>(json, JsonOptions)
            ?? throw new JsonException("The mapping result is empty.");
        if (!string.Equals(response.Domain, batch.TargetType, StringComparison.OrdinalIgnoreCase))
            throw new JsonException("The suggested domain does not match the selected deterministic import target.");
        var sourceIndexes = batch.ColumnMappings.Select(item => item.SourceColumnIndex).ToHashSet();
        if (response.Mappings.Count == 0 || response.Mappings.Any(item =>
                !sourceIndexes.Contains(item.SourceColumnIndex) ||
                _fieldRegistry.FindField(item.CanonicalFieldKey) is null))
            throw new JsonException("The provider returned an unknown source column or canonical field.");
        var set = new AiSuggestionSet
        {
            CompanyId = user.CompanyId,
            AiProcessingJobId = job.Id,
            TargetType = batch.TargetType,
            Confidence = response.Mappings.Average(item => item.Confidence),
            WarningsJson = JsonSerializer.Serialize(response.Warnings, JsonOptions)
        };
        var proposal = new AiImportProposal
        {
            CompanyId = user.CompanyId,
            ImportBatchId = batch.Id,
            AiSuggestionSet = set,
            ProposedDomain = response.Domain
        };
        foreach (var mapping in response.Mappings.OrderBy(item => item.SourceColumnIndex))
        {
            set.Suggestions.Add(new AiSuggestion
            {
                CompanyId = user.CompanyId,
                Kind = "ColumnMapping",
                SourceLocator = mapping.SourceColumnIndex.ToString(),
                TargetKey = mapping.CanonicalFieldKey,
                ProposedValueJson = JsonSerializer.Serialize(mapping.CanonicalFieldKey),
                Confidence = Math.Clamp(mapping.Confidence, 0, 1),
                Explanation = mapping.Explanation,
                WarningCodesJson = JsonSerializer.Serialize(mapping.Warnings, JsonOptions),
                SortOrder = mapping.SourceColumnIndex
            });
            proposal.ColumnProposals.Add(new AiImportColumnProposal
            {
                CompanyId = user.CompanyId,
                SourceColumnKey = mapping.SourceColumnIndex.ToString(),
                CanonicalFieldKey = mapping.CanonicalFieldKey,
                ProposedTransformationKey = mapping.TransformationKey,
                Confidence = Math.Clamp(mapping.Confidence, 0, 1),
                EvidenceJson = JsonSerializer.Serialize(new[] { mapping.Explanation }, JsonOptions),
                WarningCodesJson = JsonSerializer.Serialize(mapping.Warnings, JsonOptions)
            });
        }
        _db.AiSuggestionSets.Add(set);
        _db.AiImportProposals.Add(proposal);
        return new AiImportReview(job, set, proposal, null);
    }

    private AiImportReview PersistChecklistProposal(AppUser user, ImportBatch batch, AiProcessingJob job, string json)
    {
        var response = JsonSerializer.Deserialize<ChecklistResponse>(json, JsonOptions)
            ?? throw new JsonException("The checklist result is empty.");
        var draft = new ChecklistImportDraft(
            response.Name.Trim(),
            response.Layout,
            response.Sections.Select(section => new ChecklistImportSection(
                section.Name.Trim(),
                section.Items.Select(item => new ChecklistImportItem(
                    item.Prompt.Trim(), NullIfBlank(item.ParentPrompt), item.ResponseType,
                    item.IsRequired, item.AffectsReadiness, NullIfBlank(item.RegisterSource),
                    item.Columns.Select(column => new ChecklistImportColumn(
                        column.Heading.Trim(), column.ResponseType, column.IsRequired,
                        column.AffectsReadiness, NullIfBlank(column.RegisterSource))).ToList())).ToList())).ToList());
        ValidateChecklistDraft(draft);
        var structureJson = JsonSerializer.Serialize(draft, JsonOptions);
        var set = new AiSuggestionSet
        {
            CompanyId = user.CompanyId,
            AiProcessingJobId = job.Id,
            TargetType = ImportTargetTypes.Checklist,
            Confidence = Math.Clamp(response.Confidence, 0, 1),
            WarningsJson = JsonSerializer.Serialize(response.Warnings, JsonOptions)
        };
        set.Suggestions.Add(new AiSuggestion
        {
            CompanyId = user.CompanyId,
            Kind = "ChecklistStructure",
            SourceLocator = "document",
            TargetKey = "checklist.structure",
            ProposedValueJson = JsonSerializer.Serialize(response.Name),
            Confidence = Math.Clamp(response.Confidence, 0, 1),
            Explanation = response.Explanation,
            WarningCodesJson = JsonSerializer.Serialize(response.Warnings, JsonOptions),
            SortOrder = 1
        });
        var proposal = new AiChecklistStructureProposal
        {
            CompanyId = user.CompanyId,
            ImportBatchId = batch.Id,
            AiSuggestionSet = set,
            ProposedName = draft.Name,
            StructureJson = structureJson,
            SourceCitationJson = JsonSerializer.Serialize(response.Citations, JsonOptions)
        };
        _db.AiSuggestionSets.Add(set);
        _db.AiChecklistStructureProposals.Add(proposal);
        return new AiImportReview(job, set, null, proposal);
    }

    private static ChecklistImportDraft ParseChecklistDraft(string json, string? correctedName)
    {
        var draft = JsonSerializer.Deserialize<ChecklistImportDraft>(json, JsonOptions)
            ?? throw new InvalidOperationException("The reviewed checklist draft is unavailable.");
        if (!string.IsNullOrWhiteSpace(correctedName)) draft = draft with { Name = correctedName.Trim() };
        ValidateChecklistDraft(draft);
        return draft;
    }

    private static void ValidateChecklistDraft(ChecklistImportDraft draft)
    {
        if (draft.Name.Length is < 2 or > 160 || !ChecklistImportLayouts.All.Contains(draft.Layout))
            throw new JsonException("The checklist name or layout is invalid.");
        if (draft.Sections.Count is < 1 or > 100 || draft.Sections.Any(section =>
                string.IsNullOrWhiteSpace(section.Name) || section.Name.Length > 160 ||
                section.Items.Count > 500))
            throw new JsonException("The checklist section structure is invalid.");
        if (draft.Sections.SelectMany(section => section.Items).Any(item =>
                string.IsNullOrWhiteSpace(item.Prompt) || item.Prompt.Length > 240 ||
                item.Columns.Count > 50 ||
                item.ResponseType.Contains('<') ||
                item.Prompt.Contains("<script", StringComparison.OrdinalIgnoreCase)))
            throw new JsonException("The checklist item structure is invalid.");
    }

    private static string ReadProposedString(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.String
            ? document.RootElement.GetString() ?? string.Empty
            : document.RootElement.GetRawText();
    }

    private void RejectProhibitedPatientData(string value)
    {
        if (_sourceSafety.Inspect(value).ContainsProhibitedPatientData)
            throw new InvalidOperationException(
                "AI assistance cannot process patient-identifiable clinical data. Remove those fields and use an approved operational import file.");
    }

    private static string SafeFailure(Exception? exception) => exception switch
    {
        JsonException => "The provider output did not match the required AcuityOps schema.",
        TaskCanceledException => "The AI provider request exceeded its configured time limit.",
        HttpRequestException => "The AI provider request failed.",
        InvalidOperationException => "The AI request did not pass an AcuityOps safety or validation rule.",
        _ => "The AI provider did not return a usable result."
    };
    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AuditLog Audit(AppUser user, string action, string entityType, int entityId, string details) => new()
    {
        CompanyId = user.CompanyId,
        AppUserId = user.Id,
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        Details = details,
        CreatedAtUtc = DateTime.UtcNow
    };

    private sealed record MappingResponse(string Domain, IReadOnlyList<MappingSuggestion> Mappings, IReadOnlyList<string> Warnings);
    private sealed record MappingSuggestion(int SourceColumnIndex, string CanonicalFieldKey, string TransformationKey, decimal Confidence, string Explanation, IReadOnlyList<string> Warnings);
    private sealed record ChecklistResponse(string Name, string Layout, decimal Confidence, string Explanation, IReadOnlyList<string> Warnings, IReadOnlyList<string> Citations, IReadOnlyList<ChecklistSectionResponse> Sections);
    private sealed record ChecklistSectionResponse(string Name, IReadOnlyList<ChecklistItemResponse> Items);
    private sealed record ChecklistItemResponse(string Prompt, string ParentPrompt, string ResponseType, bool IsRequired, bool AffectsReadiness, string RegisterSource, IReadOnlyList<ChecklistColumnResponse> Columns);
    private sealed record ChecklistColumnResponse(string Heading, string ResponseType, bool IsRequired, bool AffectsReadiness, string RegisterSource);

    private const string MappingJsonSchema = """
    {"type":"object","additionalProperties":false,"required":["domain","mappings","warnings"],"properties":{
      "domain":{"type":"string"},"warnings":{"type":"array","items":{"type":"string"}},
      "mappings":{"type":"array","items":{"type":"object","additionalProperties":false,
        "required":["sourceColumnIndex","canonicalFieldKey","transformationKey","confidence","explanation","warnings"],
        "properties":{"sourceColumnIndex":{"type":"integer"},"canonicalFieldKey":{"type":"string"},
        "transformationKey":{"type":"string","enum":["trim","date","integer","boolean","status","tenant-reference"]},
        "confidence":{"type":"number","minimum":0,"maximum":1},"explanation":{"type":"string"},
        "warnings":{"type":"array","items":{"type":"string"}}}}}}}
    """;

    private const string ChecklistJsonSchema = """
    {"type":"object","additionalProperties":false,"required":["name","layout","confidence","explanation","warnings","citations","sections"],"properties":{
      "name":{"type":"string"},"layout":{"type":"string","enum":["ExplicitColumns","Matrix","OneSheetPerSection","SectionedSheet"]},
      "confidence":{"type":"number","minimum":0,"maximum":1},"explanation":{"type":"string"},
      "warnings":{"type":"array","items":{"type":"string"}},"citations":{"type":"array","items":{"type":"string"}},
      "sections":{"type":"array","items":{"type":"object","additionalProperties":false,"required":["name","items"],"properties":{
        "name":{"type":"string"},"items":{"type":"array","items":{"type":"object","additionalProperties":false,
          "required":["prompt","parentPrompt","responseType","isRequired","affectsReadiness","registerSource","columns"],
          "properties":{"prompt":{"type":"string"},"parentPrompt":{"type":"string"},"responseType":{"type":"string","enum":["Text","TextArea","PassFail","Number","Date","Dropdown","Photo"]},
          "isRequired":{"type":"boolean"},"affectsReadiness":{"type":"boolean"},"registerSource":{"type":"string"},
          "columns":{"type":"array","items":{"type":"object","additionalProperties":false,
            "required":["heading","responseType","isRequired","affectsReadiness","registerSource"],
            "properties":{"heading":{"type":"string"},"responseType":{"type":"string","enum":["Text","TextArea","PassFail","Number","Date","Dropdown","Photo"]},
            "isRequired":{"type":"boolean"},"affectsReadiness":{"type":"boolean"},"registerSource":{"type":"string"}}}}}}}}}}}
    """;
}
