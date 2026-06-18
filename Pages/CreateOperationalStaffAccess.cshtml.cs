using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class CreateOperationalStaffAccessModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/CreateManagerAccess", new { accessLevel = CurrentUserService.StaffAccess });
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/CreateManagerAccess", new { accessLevel = CurrentUserService.StaffAccess });
    }
}
