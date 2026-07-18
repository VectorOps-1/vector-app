using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ImportHistoryModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ImportBatchService _imports;
    private readonly ImportGovernanceService _governance;

    public ImportHistoryModel(VectorDbContext db, CurrentUserService currentUser, ImportBatchService imports, ImportGovernanceService governance)
    {
        _db = db;
        _currentUser = currentUser;
        _imports = imports;
        _governance = governance;
    }

    public IReadOnlyList<ImportHistoryRow> Rows { get; private set; } = [];
    public bool CanRollback { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? confirmation, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetCurrentUserAsync();
        if (user is null) return RedirectToPage("/Access");
        await LoadAsync(user, cancellationToken);
        StatusMessage = confirmation switch
        {
            "rolled-back" => "Eligible imported records were rolled back.",
            "partially-rolled-back" => "Eligible records were rolled back. Records changed or used after import were preserved.",
            _ => null
        };
        return Page();
    }

    public async Task<IActionResult> OnPostRollbackAsync(int importBatchId, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetCurrentUserAsync();
        if (user is null) return RedirectToPage("/Access");
        try
        {
            var result = await _governance.RollbackAsync(user, importBatchId, cancellationToken);
            return RedirectToPage(new { confirmation = result.Blocked == 0 ? "rolled-back" : "partially-rolled-back" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return RedirectToPage("/Home", new { permissionDenied = "true", reason = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadAsync(user, cancellationToken);
            return Page();
        }
    }

    private async Task LoadAsync(AppUser user, CancellationToken cancellationToken)
    {
        var prepare = await _imports.CanPrepareAsync(user, cancellationToken);
        if (!prepare.Allowed) throw new UnauthorizedAccessException(prepare.Message);
        CanRollback = (await _imports.CanCommitAsync(user, cancellationToken)).Allowed;
        Rows = await _db.ImportBatches.AsNoTracking()
            .Where(batch => batch.CompanyId == user.CompanyId
                && batch.SourceAssetFile != null && batch.SourceAssetFile.CompanyId == user.CompanyId)
            .OrderByDescending(batch => batch.CreatedAtUtc)
            .Select(batch => new ImportHistoryRow(
                batch.Id, batch.TargetType, batch.ProposedRecordName, batch.OriginalFileName, batch.Status,
                batch.CreatedByUser != null ? batch.CreatedByUser.FullName : "Unknown",
                batch.CreatedAtUtc, batch.CommittedAtUtc, batch.RolledBackAtUtc,
                batch.IncludedRowCount, batch.ValidRowCount, batch.InvalidRowCount,
                batch.EntityChanges.Count(change => change.RollbackStatus == "RolledBack"),
                batch.EntityChanges.Count(change => change.RollbackStatus == "Blocked")))
            .ToListAsync(cancellationToken);
    }

    public sealed record ImportHistoryRow(
        int Id, string TargetType, string? ProposedRecordName, string OriginalFileName, string Status,
        string CreatedBy, DateTime CreatedAtUtc, DateTime? CommittedAtUtc, DateTime? RolledBackAtUtc,
        int IncludedRows, int ValidRows, int InvalidRows, int RolledBackChanges, int BlockedChanges);
}
