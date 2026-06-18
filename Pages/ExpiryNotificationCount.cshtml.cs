using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ExpiryNotificationCountModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly ExpiryPressureService _expiryPressure;

    public ExpiryNotificationCountModel(CurrentUserService currentUser, ExpiryPressureService expiryPressure)
    {
        _currentUser = currentUser;
        _expiryPressure = expiryPressure;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return new JsonResult(new { count = 0, url = "/ExpiryNotifications" });
        }

        var rows = await _expiryPressure.LoadForUserAsync(currentUser);
        return new JsonResult(new
        {
            count = rows.Count,
            url = "/ExpiryNotifications",
            label = "Open expiry and service notifications"
        });
    }
}
