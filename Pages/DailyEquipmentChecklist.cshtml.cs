using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class DailyEquipmentChecklistModel : PageModel
{
    [BindProperty] public string? Callsign { get; set; }
    [BindProperty] public string? Registration { get; set; }

    public IActionResult OnGet(string? callsign, string? registration)
    {
        return RedirectToPage("/DailyVehicleChecklist", BuildRouteValues(callsign, registration, "daily"));
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/DailyVehicleChecklist", BuildRouteValues(Callsign, Registration, "daily"));
    }

    private static object BuildRouteValues(string? callsign, string? registration, string frequency)
    {
        return new
        {
            frequency,
            callsign = Normalize(callsign),
            registration = Normalize(registration)
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
