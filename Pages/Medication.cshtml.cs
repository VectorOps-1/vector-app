using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class MedicationModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;

    public MedicationModel(CurrentUserService currentUser, LocationOptionService locationOptions)
    {
        _currentUser = currentUser;
        _locationOptions = locationOptions;
    }

    public List<SelectListItem> LocationOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        LocationOptions = await _locationOptions.GetAssetLocationOptionsAsync(currentUser.CompanyId);
        return Page();
    }
}
