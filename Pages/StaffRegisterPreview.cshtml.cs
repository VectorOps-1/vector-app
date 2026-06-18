using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class StaffRegisterPreviewModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly SetupUploadService _setupUploads;

    public StaffRegisterPreviewModel(CurrentUserService currentUser, SetupUploadService setupUploads)
    {
        _currentUser = currentUser;
        _setupUploads = setupUploads;
    }

    public string FileName { get; private set; } = "Uploaded staff register";

    public async Task<IActionResult> OnGetAsync(int? sourceFileId, string? fileName)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (sourceFileId.HasValue)
        {
            FileName = await _setupUploads.GetUploadedFileNameAsync(sourceFileId.Value, SetupUploadService.RegisterUploadEntityType)
                ?? FileName;
        }
        else if (!string.IsNullOrWhiteSpace(fileName))
        {
            FileName = fileName;
        }

        return Page();
    }
}
