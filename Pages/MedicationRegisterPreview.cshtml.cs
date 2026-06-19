using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class MedicationRegisterPreviewModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;
    private readonly SetupUploadService _setupUploads;

    public MedicationRegisterPreviewModel(
        CurrentUserService currentUser,
        LocationOptionService locationOptions,
        SetupUploadService setupUploads)
    {
        _currentUser = currentUser;
        _locationOptions = locationOptions;
        _setupUploads = setupUploads;
    }

    public string FileName { get; private set; } = "Uploaded medication register";
    public List<SelectListItem> LocationOptions { get; private set; } = new();

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

        return RedirectToPage("/MedicationRegister", new { view = "register", confirmation = "medication-source-uploaded" });
    }
}
