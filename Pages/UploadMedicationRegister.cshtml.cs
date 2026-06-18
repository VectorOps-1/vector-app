using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class UploadMedicationRegisterModel : PageModel
{
    private readonly SetupUploadService _setupUploads;

    public UploadMedicationRegisterModel(SetupUploadService setupUploads)
    {
        _setupUploads = setupUploads;
    }

    [BindProperty]
    public IFormFile? MedicationRegisterFile { get; set; }

    public string? StatusMessage { get; private set; }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _setupUploads.SaveRegisterUploadAsync(MedicationRegisterFile, "Medication Register");
        if (result.IsNotSignedIn)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (!result.IsSaved)
        {
            StatusMessage = result.ErrorMessage;
            return Page();
        }

        return RedirectToPage("/MedicationRegisterPreview", new { sourceFileId = result.FileId });
    }
}
