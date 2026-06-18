using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ChecklistPreviewModel : PageModel
{
    private readonly SetupUploadService _setupUploads;
    private readonly CurrentUserService _currentUser;

    public ChecklistPreviewModel(SetupUploadService setupUploads, CurrentUserService currentUser)
    {
        _setupUploads = setupUploads;
        _currentUser = currentUser;
    }

    public string FileName { get; private set; } = "Uploaded checklist";

    public async Task<IActionResult> OnGetAsync(int? sourceFileId, string? fileName)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (sourceFileId.HasValue)
        {
            FileName = await _setupUploads.GetUploadedFileNameAsync(sourceFileId.Value, SetupUploadService.ChecklistUploadEntityType)
                ?? FileName;
        }
        else if (!string.IsNullOrWhiteSpace(fileName))
        {
            FileName = fileName;
        }

        return Page();
    }
}
