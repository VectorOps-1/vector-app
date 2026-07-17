using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ImportBatchModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly ImportBatchService _imports;
    private readonly ImportRegisterWorkflowService _workflow;
    private readonly IImportFieldRegistry _fields;
    private readonly IImportTabularReader _reader;

    public ImportBatchModel(
        CurrentUserService currentUser,
        ImportBatchService imports,
        ImportRegisterWorkflowService workflow,
        IImportFieldRegistry fields,
        IImportTabularReader reader)
    {
        _currentUser = currentUser;
        _imports = imports;
        _workflow = workflow;
        _fields = fields;
        _reader = reader;
    }

    public ImportBatch? Batch { get; private set; }
    public ImportSourceProfile? SourceProfile { get; private set; }
    public ImportTargetDefinition? Target { get; private set; }
    public bool CanCommit { get; private set; }
    public IReadOnlyDictionary<int, IReadOnlyList<string>> SamplesByColumn { get; private set; } = new Dictionary<int, IReadOnlyList<string>>();
    public string? StatusMessage { get; private set; }

    [BindProperty] public string? Worksheet { get; set; }
    [BindProperty] public int HeaderRowNumber { get; set; } = 1;
    [BindProperty] public List<MappingInputModel> Mappings { get; set; } = [];
    [BindProperty] public RowCorrectionInputModel RowCorrection { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int importBatchId, string? confirmation, CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync();
        if (user.Result is not null) return user.Result;
        await LoadAsync(user.User!, importBatchId, cancellationToken);
        StatusMessage = confirmation switch
        {
            "source-selected" => "Worksheet and header row selected. Confirm every column mapping.",
            "mapping-saved" => "Column mappings saved. Validate the source rows next.",
            "validated" => "Rows validated. Resolve highlighted rows and duplicate decisions before commit.",
            "row-updated" => "Row correction saved and the batch revalidated.",
            "committed" => "Import committed. The records are now available in the existing register.",
            _ => null
        };
        return Page();
    }

    public async Task<IActionResult> OnPostSelectSourceAsync(int importBatchId, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(importBatchId, cancellationToken, async user =>
        {
            await _workflow.SelectSourceAsync(user, importBatchId, Worksheet, HeaderRowNumber, cancellationToken);
            return RedirectToPage(new { importBatchId, confirmation = "source-selected" });
        });
    }

    public async Task<IActionResult> OnPostSaveMappingsAsync(int importBatchId, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(importBatchId, cancellationToken, async user =>
        {
            await _workflow.SaveMappingsAsync(
                user,
                importBatchId,
                Mappings.Select(mapping => new ImportMappingInput(mapping.SourceColumnIndex, mapping.TargetFieldKey, mapping.IsIgnored)).ToList(),
                cancellationToken);
            return RedirectToPage(new { importBatchId, confirmation = "mapping-saved" });
        });
    }

    public async Task<IActionResult> OnPostValidateAsync(int importBatchId, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(importBatchId, cancellationToken, async user =>
        {
            await _workflow.ValidateAsync(user, importBatchId, cancellationToken);
            return RedirectToPage(new { importBatchId, confirmation = "validated" });
        });
    }

    public async Task<IActionResult> OnPostCorrectRowAsync(int importBatchId, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(importBatchId, cancellationToken, async user =>
        {
            var values = RowCorrection.Fields
                .Where(field => !string.IsNullOrWhiteSpace(field.Key))
                .ToDictionary(field => field.Key, field => field.Value, StringComparer.OrdinalIgnoreCase);
            await _workflow.CorrectRowAsync(
                user,
                importBatchId,
                new ImportRowCorrection(RowCorrection.SourceRowNumber, values, RowCorrection.IsIncluded, RowCorrection.Decision),
                cancellationToken);
            return RedirectToPage(new { importBatchId, confirmation = "row-updated" });
        });
    }

    public async Task<IActionResult> OnPostCommitAsync(int importBatchId, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(importBatchId, cancellationToken, async user =>
        {
            await _workflow.CommitAsync(user, importBatchId, cancellationToken);
            return RedirectToPage(new { importBatchId, confirmation = "committed" });
        });
    }

    public IReadOnlyDictionary<string, string?> ValuesFor(ImportRowResult row)
    {
        var json = row.CorrectedPayloadJson ?? row.OriginalPayloadJson;
        return JsonSerializer.Deserialize<Dictionary<string, string?>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? new Dictionary<string, string?>();
    }

    public IReadOnlyDictionary<string, string> ErrorsFor(ImportRowResult row)
    {
        return string.IsNullOrWhiteSpace(row.FieldErrorsJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(row.FieldErrorsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? new Dictionary<string, string>();
    }

    public IReadOnlyList<ImportDuplicateCandidate> DuplicatesFor(ImportRowResult row)
    {
        return string.IsNullOrWhiteSpace(row.DuplicateCandidatesJson)
            ? []
            : JsonSerializer.Deserialize<List<ImportDuplicateCandidate>>(row.DuplicateCandidatesJson, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
    }

    private async Task<IActionResult> ExecuteAsync(
        int importBatchId,
        CancellationToken cancellationToken,
        Func<AppUser, Task<IActionResult>> action)
    {
        var current = await RequireUserAsync();
        if (current.Result is not null) return current.Result;
        try
        {
            return await action(current.User!);
        }
        catch (UnauthorizedAccessException ex)
        {
            return RedirectToPage("/Home", new { permissionDenied = "true", reason = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadAsync(current.User!, importBatchId, cancellationToken);
            return Page();
        }
    }

    private async Task LoadAsync(AppUser user, int importBatchId, CancellationToken cancellationToken)
    {
        var access = await _imports.CanPrepareAsync(user, cancellationToken);
        if (!access.Allowed) throw new UnauthorizedAccessException(access.Message);
        Batch = await _workflow.LoadAsync(user, importBatchId, cancellationToken)
            ?? throw new InvalidOperationException("The import batch was not found.");
        SourceProfile = ImportSourceProfile.FromJson(Batch.SourceProfileJson);
        Target = _fields.FindTarget(Batch.TargetType);
        CanCommit = (await _imports.CanCommitAsync(user, cancellationToken)).Allowed;
        Worksheet = Batch.SelectedWorksheet ?? SourceProfile?.Worksheets.FirstOrDefault()?.Name;
        HeaderRowNumber = Batch.HeaderRowNumber ?? 1;
        if (Batch.HeaderRowNumber is not null && Batch.SourceAssetFile is not null)
        {
            var source = await _reader.ReadAsync(Batch.SourceAssetFile, Batch.SelectedWorksheet, Batch.HeaderRowNumber.Value, cancellationToken);
            SamplesByColumn = source.Columns.ToDictionary(column => column.Index, column => column.Samples);
        }
    }

    private async Task<(AppUser? User, IActionResult? Result)> RequireUserAsync()
    {
        var user = await _currentUser.GetCurrentUserAsync();
        return user is null ? (null, RedirectToPage("/Access")) : (user, null);
    }

    public sealed class MappingInputModel
    {
        public int SourceColumnIndex { get; set; }
        public string? TargetFieldKey { get; set; }
        public bool IsIgnored { get; set; }
    }

    public sealed class RowCorrectionInputModel
    {
        public int SourceRowNumber { get; set; }
        public bool IsIncluded { get; set; } = true;
        public string? Decision { get; set; }
        public List<FieldInputModel> Fields { get; set; } = [];
    }

    public sealed class FieldInputModel
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
