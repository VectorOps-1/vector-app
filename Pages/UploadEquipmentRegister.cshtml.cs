using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class UploadEquipmentRegisterModel : PageModel
{
    private readonly SetupUploadService _setupUploads;

    public UploadEquipmentRegisterModel(SetupUploadService setupUploads)
    {
        _setupUploads = setupUploads;
    }

    [BindProperty]
    public IFormFile? EquipmentRegisterFile { get; set; }

    public string? StatusMessage { get; private set; }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _setupUploads.SaveRegisterUploadAsync(EquipmentRegisterFile, "Equipment Register");
        if (result.IsNotSignedIn)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (!result.IsSaved)
        {
            StatusMessage = result.ErrorMessage;
            return Page();
        }

        return RedirectToPage("/EquipmentRegisterPreview", new { sourceFileId = result.FileId });
    }
}
