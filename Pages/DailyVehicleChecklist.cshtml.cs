using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class DailyVehicleChecklistModel : PageModel
{
    [BindProperty] public string? Callsign { get; set; }
    [BindProperty] public string? Registration { get; set; }
    [BindProperty] public string? FleetNumber { get; set; }
    [BindProperty] public string? VehicleType { get; set; }
    [BindProperty] public int? Kilometres { get; set; }
    [BindProperty] public string? FuelLevel { get; set; }
    [BindProperty] public DateTime? NextServiceDate { get; set; }
    [BindProperty] public string? VehicleStatus { get; set; }
    [BindProperty] public string? DamageType { get; set; }
    [BindProperty] public string? DamageSeverity { get; set; }
    [BindProperty] public string? DamageNotes { get; set; }
    [BindProperty] public string? ChecklistNotes { get; set; }
    public string? StatusMessage { get; private set; }

    public IReadOnlyList<VehicleRegisterOption> VehicleRegisterOptions { get; } =
    [
        new("AMB-101", "Medic 1", "EMS-001", "Ambulance", 68420, "2026-06-30", "Operational"),
        new("AMB-102", "Medic 2", "EMS-002", "Ambulance", 72210, "2026-07-14", "Operational with notes"),
        new("RSP-201", "Response 1", "EMS-011", "Rapid Response", 41890, "2026-08-05", "Operational")
    ];

    public string EquipmentChecklistUrl => $"/DailyEquipmentChecklist?callsign={Uri.EscapeDataString(Callsign ?? string.Empty)}&registration={Uri.EscapeDataString(Registration ?? string.Empty)}";

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Callsign) && string.IsNullOrWhiteSpace(Registration))
        {
            StatusMessage = "Enter a callsign or registration before saving.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(VehicleStatus))
        {
            StatusMessage = "Select the vehicle condition before saving.";
            return Page();
        }

        StatusMessage = "Vehicle inspection ready to save. Continue to equipment checklist for the same vehicle. Database storage, schematic damage marks, signed-in profile linkage, and audit logging will be connected in the production data phase.";
        return Page();
    }

    public sealed record VehicleRegisterOption(
        string Registration,
        string Callsign,
        string FleetNumber,
        string VehicleType,
        int Kilometres,
        string NextServiceDate,
        string VehicleStatus);
}
