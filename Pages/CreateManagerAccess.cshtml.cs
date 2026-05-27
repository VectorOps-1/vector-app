using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class CreateManagerAccessModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;

    public CreateManagerAccessModel(CurrentUserService currentUser, LocationOptionService locationOptions)
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
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        LocationOptions = await _locationOptions.GetOperationalAreaOptionsAsync(currentUser.CompanyId);
        return Page();
    }
}
