using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ImportBatchModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly ImportBatchService _imports;

    public ImportBatchModel(CurrentUserService currentUser, ImportBatchService imports)
    {
        _currentUser = currentUser;
        _imports = imports;
    }

    public ImportBatch? Batch { get; private set; }
    public ImportSourceProfile? SourceProfile { get; private set; }

    public async Task<IActionResult> OnGetAsync(int importBatchId, CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetCurrentUserAsync();
        if (user is null)
        {
            return RedirectToPage("/Access");
        }

        var access = await _imports.CanPrepareAsync(user, cancellationToken);
        if (!access.Allowed)
        {
            return RedirectToPage("/Home", new { permissionDenied = "true", reason = access.Message });
        }

        Batch = await _imports.GetForCurrentTenantAsync(user, importBatchId, cancellationToken);
        if (Batch is null)
        {
            return NotFound();
        }

        SourceProfile = ImportSourceProfile.FromJson(Batch.SourceProfileJson);
        return Page();
    }
}
