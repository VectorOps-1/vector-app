using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class MyProfileModel : PageModel
{
    private readonly CurrentUserService _currentUser;

    public MyProfileModel(CurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        return RedirectToPage("/StaffRecordsSearch", new { staffUserId = currentUser.Id });
    }
}
