using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EquipmentRegisterPreviewModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;

    public EquipmentRegisterPreviewModel(CurrentUserService currentUser, LocationOptionService locationOptions)
    {
        _currentUser = currentUser;
        _locationOptions = locationOptions;
    }

    public string FileName { get; private set; } = "Uploaded equipment register";
    public List<SelectListItem> LocationOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string? fileName)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        LocationOptions = await _locationOptions.GetAssetLocationOptionsAsync(currentUser.CompanyId);

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            FileName = fileName;
        }

        return Page();
    }
}
