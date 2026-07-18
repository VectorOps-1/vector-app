using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public static class ChecklistImportLayouts
{
    public const string ExplicitColumns = "ExplicitColumns";
    public const string Matrix = "Matrix";
    public const string OneSheetPerSection = "OneSheetPerSection";
    public const string SectionedSheet = "SectionedSheet";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ExplicitColumns, Matrix, OneSheetPerSection, SectionedSheet
    };
}

public sealed record ChecklistImportDraft(string Name, string Layout, IReadOnlyList<ChecklistImportSection> Sections);
public sealed record ChecklistImportSection(string Name, IReadOnlyList<ChecklistImportItem> Items);
public sealed record ChecklistImportItem(
    string Prompt,
    string? ParentPrompt,
    string ResponseType,
    bool IsRequired,
    bool AffectsReadiness,
    string? RegisterSource,
    IReadOnlyList<ChecklistImportColumn> Columns);
public sealed record ChecklistImportColumn(string Heading, string ResponseType, bool IsRequired, bool AffectsReadiness, string? RegisterSource);

public sealed class ChecklistImportConversionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly VectorDbContext _db;
    private readonly ImportBatchService _batches;
    private readonly IImportTabularReader _reader;
    private readonly IUserActionPermissionService _permissions;

    public ChecklistImportConversionService(
        VectorDbContext db,
        ImportBatchService batches,
        IImportTabularReader reader,
        IUserActionPermissionService permissions)
    {
        _db = db;
        _batches = batches;
        _reader = reader;
        _permissions = permissions;
    }

    public async Task<ChecklistImportDraft> PrepareAsync(
        AppUser user,
        int batchId,
        string checklistName,
        string layout,
        string? worksheet,
        int headerRowNumber,
        CancellationToken cancellationToken = default)
    {
        var batch = await RequireBatchAsync(user, batchId, cancellationToken);
        if (!string.Equals(batch.TargetType, ImportTargetTypes.Checklist, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This import batch is not a checklist source.");
        if (!ChecklistImportLayouts.All.Contains(layout))
            throw new InvalidOperationException("Choose a supported checklist layout.");
        checklistName = checklistName?.Trim() ?? string.Empty;
        if (checklistName.Length is < 2 or > 160)
            throw new InvalidOperationException("Enter a checklist name between 2 and 160 characters.");
        if (headerRowNumber < 1) throw new InvalidOperationException("Header row must be 1 or greater.");

        var profile = ImportSourceProfile.FromJson(batch.SourceProfileJson)
            ?? throw new InvalidOperationException("The checklist source profile is unavailable.");
        var sections = layout switch
        {
            ChecklistImportLayouts.ExplicitColumns =>
                ParseExplicit(await ReadSelectedAsync(batch, worksheet, headerRowNumber, cancellationToken)),
            ChecklistImportLayouts.Matrix =>
                [ParseMatrix(await ReadSelectedAsync(batch, worksheet, headerRowNumber, cancellationToken))],
            ChecklistImportLayouts.OneSheetPerSection =>
                await ParseAllSheetsAsync(batch, profile, headerRowNumber, cancellationToken),
            ChecklistImportLayouts.SectionedSheet =>
                ParseSectioned(await ReadSelectedAsync(batch, worksheet, headerRowNumber, cancellationToken)),
            _ => []
        };
        sections = sections.Where(section => section.Items.Count > 0).ToList();
        if (sections.Count == 0 || sections.All(section => section.Items.Count == 0))
            throw new InvalidOperationException("No checklist items were found. Review the layout, worksheet, and header row.");
        if (sections.Any(section => string.IsNullOrWhiteSpace(section.Name) || section.Name.Length > 160))
            throw new InvalidOperationException("Every section needs a name of 160 characters or fewer.");
        if (sections.SelectMany(section => section.Items).Any(item => string.IsNullOrWhiteSpace(item.Prompt) || item.Prompt.Length > 240))
            throw new InvalidOperationException("Every checklist item needs a label of 240 characters or fewer.");

        var draft = new ChecklistImportDraft(checklistName, layout, sections);
        _db.ImportRowResults.RemoveRange(batch.RowResults);
        batch.RowResults.Clear();
        batch.RowResults.Add(new ImportRowResult
        {
            CompanyId = user.CompanyId,
            SourceRowNumber = 0,
            OriginalPayloadJson = JsonSerializer.Serialize(draft, JsonOptions),
            CorrectedPayloadJson = JsonSerializer.Serialize(draft, JsonOptions),
            ValidationStatus = ImportRowStatuses.Valid,
            RowDecision = ImportRowDecisions.Create,
            IsIncluded = true
        });
        batch.ProposedRecordName = checklistName;
        batch.SelectedWorksheet = layout == ChecklistImportLayouts.OneSheetPerSection ? "All worksheets" : worksheet;
        batch.HeaderRowNumber = headerRowNumber;
        batch.LayoutMode = layout;
        batch.SourceRowCount = sections.Sum(section => section.Items.Count);
        batch.IncludedRowCount = batch.SourceRowCount;
        batch.ValidRowCount = batch.SourceRowCount;
        batch.InvalidRowCount = 0;
        batch.Status = ImportBatchStatuses.ReadyToCommit;
        batch.ValidatedByUserId = user.Id;
        batch.ValidatedAtUtc = DateTime.UtcNow;
        batch.UpdatedAtUtc = DateTime.UtcNow;
        batch.ConcurrencyToken = Guid.NewGuid().ToString("D");
        await _db.SaveChangesAsync(cancellationToken);
        return draft;
    }

    public ChecklistImportDraft? ReadDraft(ImportBatch batch)
    {
        var row = batch.RowResults.OrderBy(item => item.SourceRowNumber).FirstOrDefault();
        if (row is null) return null;
        try { return JsonSerializer.Deserialize<ChecklistImportDraft>(row.CorrectedPayloadJson ?? row.OriginalPayloadJson, JsonOptions); }
        catch (JsonException) { return null; }
    }

    public async Task<ChecklistTemplate> CommitDraftAsync(AppUser user, int batchId, CancellationToken cancellationToken = default)
    {
        var access = await _batches.CanCommitAsync(user, cancellationToken);
        if (!access.Allowed) throw new UnauthorizedAccessException(access.Message);
        if (!await _permissions.HasPermissionAsync(user, UserActionPermissions.ChecklistsBuild, cancellationToken))
            throw new UnauthorizedAccessException("You do not have permission to build checklist drafts.");
        var existing = await _db.ChecklistTemplates
            .Include(template => template.Sections).ThenInclude(section => section.Items).ThenInclude(item => item.ColumnDefinitions)
            .SingleOrDefaultAsync(template => template.CompanyId == user.CompanyId && template.SourceImportBatchId == batchId, cancellationToken);
        if (existing is not null) return existing;

        var batch = await RequireBatchAsync(user, batchId, cancellationToken);
        if (!string.Equals(batch.Status, ImportBatchStatuses.ReadyToCommit, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Prepare and confirm the checklist structure before creating the draft.");
        var draft = ReadDraft(batch) ?? throw new InvalidOperationException("The confirmed checklist structure is unavailable.");
        var company = await _db.Companies.AsNoTracking().SingleAsync(item => item.Id == user.CompanyId, cancellationToken);
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var template = new ChecklistTemplate
        {
            CompanyId = user.CompanyId,
            ClientName = CompanyBranding.GetDisplayCompanyName(company),
            Name = draft.Name,
            ChecklistType = "Vehicle",
            TargetVehicleType = "Unassigned",
            Version = "1.0",
            Status = "Draft",
            SourceType = "Imported",
            SourceImportBatchId = batch.Id,
            CreatedByUserId = user.Id,
            IsPublished = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var sourceSection in draft.Sections.Select((section, index) => (section, index)))
        {
            var section = new ChecklistSection { Name = sourceSection.section.Name, DisplayOrder = (sourceSection.index + 1) * 10 };
            template.Sections.Add(section);
            var roots = new Dictionary<string, ChecklistItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var sourceItem in sourceSection.section.Items.Select((item, index) => (item, index)))
            {
                var item = new ChecklistItem
                {
                    Prompt = sourceItem.item.Prompt,
                    ResponseType = sourceItem.item.ResponseType,
                    ItemKind = sourceItem.item.ParentPrompt is null ? "Field" : "SubItem",
                    FieldKey = FieldKey($"{sourceSection.section.Name}-{sourceItem.item.Prompt}"),
                    DefaultLocation = sourceItem.item.RegisterSource,
                    IsRequired = sourceItem.item.IsRequired,
                    IsReadinessCritical = sourceItem.item.AffectsReadiness,
                    AllowsSameAsPrevious = true,
                    DisplayOrder = sourceItem.index + 1
                };
                if (!string.IsNullOrWhiteSpace(sourceItem.item.ParentPrompt))
                {
                    if (!roots.TryGetValue(sourceItem.item.ParentPrompt, out var parent))
                        throw new InvalidOperationException($"Subitem '{item.Prompt}' refers to missing parent '{sourceItem.item.ParentPrompt}' in section '{section.Name}'.");
                    item.ParentChecklistItem = parent;
                }
                else roots[item.Prompt] = item;

                var columns = sourceItem.item.Columns.Count > 0
                    ? sourceItem.item.Columns
                    : [new ChecklistImportColumn("Response", sourceItem.item.ResponseType, sourceItem.item.IsRequired, sourceItem.item.AffectsReadiness, sourceItem.item.RegisterSource)];
                foreach (var sourceColumn in columns.Select((column, index) => (column, index)))
                {
                    item.ColumnDefinitions.Add(new ChecklistColumnDefinition
                    {
                        Heading = sourceColumn.column.Heading,
                        FieldKey = FieldKey($"{item.Prompt}-{sourceColumn.column.Heading}"),
                        ResponseType = sourceColumn.column.ResponseType,
                        RegisterSource = sourceColumn.column.RegisterSource,
                        IsRequired = sourceColumn.column.IsRequired,
                        IsEditable = true,
                        AllowsNotApplicable = !sourceColumn.column.IsRequired,
                        PullsFromRegister = !string.IsNullOrWhiteSpace(sourceColumn.column.RegisterSource),
                        AffectsReadiness = sourceColumn.column.AffectsReadiness,
                        SameAsPreviousEligible = true,
                        RequiresNoteWhenNotNormal = sourceColumn.column.AffectsReadiness,
                        ReadinessImpact = sourceColumn.column.AffectsReadiness ? "Configured" : "None",
                        SortOrder = sourceColumn.index + 1
                    });
                }
                section.Items.Add(item);
            }
        }

        _db.ChecklistTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
        var after = JsonSerializer.Serialize(new { template.Id, template.Name, template.Version, template.Status, Sections = template.Sections.Count }, JsonOptions);
        _db.ImportEntityChanges.Add(new ImportEntityChange
        {
            CompanyId = user.CompanyId,
            ImportBatchId = batch.Id,
            ImportRowResultId = batch.RowResults.First().Id,
            EntityType = nameof(ChecklistTemplate),
            EntityId = template.Id,
            Action = ImportRowDecisions.Create,
            AfterValuesJson = after,
            EntityStateToken = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(after))),
            IsRollbackEligible = true,
            RollbackStatus = "Eligible"
        });
        batch.Status = ImportBatchStatuses.Committed;
        batch.CommittedByUserId = user.Id;
        batch.CommittedAtUtc = now;
        batch.UpdatedAtUtc = now;
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = user.CompanyId, AppUserId = user.Id, Action = "Checklist draft imported",
            EntityType = nameof(ChecklistTemplate), EntityId = template.Id,
            Details = $"Imported checklist draft '{template.Name}' with {template.Sections.Count} sections. It was not published.", CreatedAtUtc = now
        });
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return template;
    }

    private async Task<ImportBatch> RequireBatchAsync(AppUser user, int batchId, CancellationToken cancellationToken)
    {
        var access = await _batches.CanPrepareAsync(user, cancellationToken);
        if (!access.Allowed) throw new UnauthorizedAccessException(access.Message);
        return await _db.ImportBatches
            .Include(batch => batch.SourceAssetFile)
            .Include(batch => batch.RowResults)
            .SingleOrDefaultAsync(batch => batch.Id == batchId && batch.CompanyId == user.CompanyId && batch.SourceAssetFile!.CompanyId == user.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("The checklist import batch was not found for this company.");
    }

    private Task<ImportTabularData> ReadSelectedAsync(ImportBatch batch, string? worksheet, int headerRow, CancellationToken token) =>
        _reader.ReadAsync(batch.SourceAssetFile!, worksheet, headerRow, token);

    private async Task<List<ChecklistImportSection>> ParseAllSheetsAsync(ImportBatch batch, ImportSourceProfile profile, int headerRow, CancellationToken token)
    {
        var sections = new List<ChecklistImportSection>();
        foreach (var worksheet in profile.Worksheets)
            sections.Add(ParseMatrix(await ReadSelectedAsync(batch, worksheet.Name, headerRow, token)));
        return sections;
    }

    private static List<ChecklistImportSection> ParseExplicit(ImportTabularData data)
    {
        var headings = HeadingMap(data);
        var sections = new Dictionary<string, List<ChecklistImportItem>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in data.Rows)
        {
            var sectionName = Cell(row, headings, "section") ?? "Imported section";
            var itemName = Cell(row, headings, "item");
            var subitem = Cell(row, headings, "subitem");
            var prompt = subitem ?? itemName;
            if (string.IsNullOrWhiteSpace(prompt)) continue;
            var parent = subitem is null ? null : Cell(row, headings, "parent") ?? itemName;
            var response = NormalizeResponseType(Cell(row, headings, "inputtype", "responsetype", "type"));
            var required = ParseBoolean(Cell(row, headings, "required"));
            var readiness = ParseBoolean(Cell(row, headings, "readiness", "affectsreadiness", "critical"));
            var register = Cell(row, headings, "registersource", "source");
            var column = Cell(row, headings, "column", "columnheading");
            var columns = string.IsNullOrWhiteSpace(column) ? Array.Empty<ChecklistImportColumn>() :
                [new ChecklistImportColumn(column, response, required, readiness, register)];
            if (!sections.TryGetValue(sectionName, out var items)) sections[sectionName] = items = [];
            items.Add(new ChecklistImportItem(prompt, parent, response, required, readiness, register, columns));
        }
        return sections.Select(section => new ChecklistImportSection(section.Key, section.Value)).ToList();
    }

    private static ChecklistImportSection ParseMatrix(ImportTabularData data)
    {
        if (data.Columns.Count == 0) return new ChecklistImportSection(data.Worksheet, []);
        var columns = data.Columns.Skip(1).Select(column => new ChecklistImportColumn(column.Heading, "Text", false, false, null)).ToList();
        var items = data.Rows.Select(row => row.Values.GetValueOrDefault(data.Columns[0].Index))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => new ChecklistImportItem(value!, null, "Text", false, false, null, columns)).ToList();
        return new ChecklistImportSection(data.Worksheet, items);
    }

    private static List<ChecklistImportSection> ParseSectioned(ImportTabularData data)
    {
        var headings = HeadingMap(data);
        if (headings.ContainsKey("section") && headings.ContainsKey("item")) return ParseExplicit(data);
        var sections = new List<ChecklistImportSection>();
        var currentName = "Imported section";
        var currentItems = new List<ChecklistImportItem>();
        foreach (var row in data.Rows)
        {
            var populated = row.Values.Where(cell => !string.IsNullOrWhiteSpace(cell.Value)).ToList();
            if (populated.Count == 1)
            {
                if (currentItems.Count > 0) sections.Add(new ChecklistImportSection(currentName, currentItems));
                currentName = populated[0].Value!;
                currentItems = [];
                continue;
            }
            var prompt = populated.FirstOrDefault().Value;
            if (!string.IsNullOrWhiteSpace(prompt)) currentItems.Add(new ChecklistImportItem(prompt, null, "Text", false, false, null, []));
        }
        if (currentItems.Count > 0) sections.Add(new ChecklistImportSection(currentName, currentItems));
        return sections;
    }

    private static Dictionary<string, int> HeadingMap(ImportTabularData data) => data.Columns
        .GroupBy(column => Normalize(column.Heading)).ToDictionary(group => group.Key, group => group.First().Index);
    private static string? Cell(ImportSourceRow row, IReadOnlyDictionary<string, int> headings, params string[] names)
    {
        foreach (var name in names)
            if (headings.TryGetValue(Normalize(name), out var index) && row.Values.TryGetValue(index, out var value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
        return null;
    }
    private static bool ParseBoolean(string? value) => value is not null && new[] { "yes", "true", "1", "required", "critical" }.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
    private static string NormalizeResponseType(string? value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "passfail" or "yesno" or "boolean" => "PassFail",
            "number" or "numeric" or "integer" => "Number",
            "date" => "Date",
            "dropdown" or "select" => "Dropdown",
            "photo" or "image" => "Photo",
            "note" or "textarea" or "longtext" => "TextArea",
            _ => "Text"
        };
    }
    private static string FieldKey(string value) => Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
    private static string Normalize(string? value) => Regex.Replace(value?.Trim().ToLowerInvariant() ?? string.Empty, "[^a-z0-9]+", string.Empty);
}
