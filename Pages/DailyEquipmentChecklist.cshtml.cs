using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class DailyEquipmentChecklistModel : PageModel
{
    [BindProperty] public string? Callsign { get; set; }
    [BindProperty] public string? Registration { get; set; }
    [BindProperty] public string? AssetId { get; set; }
    [BindProperty] public string? SerialNumber { get; set; }
    [BindProperty] public string? EquipmentName { get; set; }
    [BindProperty] public string? AssignedLocation { get; set; }
    [BindProperty] public string? BatteryState { get; set; }
    [BindProperty] public string? EquipmentStatus { get; set; }
    [BindProperty] public string? ChecklistNotes { get; set; }
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    public string LinkedVehicleLabel => string.IsNullOrWhiteSpace(Callsign) && string.IsNullOrWhiteSpace(Registration)
        ? "No vehicle selected"
        : $"{Callsign} {Registration}".Trim();

    public string VehicleChecklistUrl => $"/DailyVehicleChecklist?callsign={Uri.EscapeDataString(Callsign ?? string.Empty)}&registration={Uri.EscapeDataString(Registration ?? string.Empty)}";

    public void OnGet(string? callsign, string? registration)
    {
        Callsign = callsign;
        Registration = registration;
        AssignedLocation = string.IsNullOrWhiteSpace(callsign) ? null : callsign;
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(AssetId) && string.IsNullOrWhiteSpace(SerialNumber) && string.IsNullOrWhiteSpace(EquipmentName))
        {
            StatusMessage = "Enter an asset ID, serial number, or equipment name before saving.";
            return Page();
        }

        StatusMessage = $"Equipment checklist ready to save against linked vehicle: {LinkedVehicleLabel}. Database storage, daily inspection session linkage, signed-in profile linkage, and audit logging will be connected in the production data phase.";
        ActionSaved = true;
        return Page();
    }
}
