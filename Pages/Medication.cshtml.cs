using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class MedicationModel : PageModel
{
    private readonly CurrentUserService _currentUser;

    public MedicationModel(CurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        return Page();
    }
}
