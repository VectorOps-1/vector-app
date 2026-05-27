using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class DailyVehicleChecklistModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string Frequency { get; set; } = "daily";
    [BindProperty] public string? Callsign { get; set; }
    [BindProperty] public string? Registration { get; set; }
    [BindProperty] public string? VehicleType { get; set; }
    [BindProperty] public int? Kilometres { get; set; }
    [BindProperty] public string? FuelLevel { get; set; }
    [BindProperty] public DateTime? NextServiceDate { get; set; }
    [BindProperty] public string? VehicleStatus { get; set; }
    [BindProperty] public string? LightsStatus { get; set; }
    [BindProperty] public string? SirenStatus { get; set; }
    [BindProperty] public string? WarningLightsStatus { get; set; }
    [BindProperty] public string? TyresStatus { get; set; }
    [BindProperty] public string? OpsRadioStatus { get; set; }
    [BindProperty] public string? DamageType { get; set; }
    [BindProperty] public string? DamageSeverity { get; set; }
    [BindProperty] public string? DamageNotes { get; set; }
    [BindProperty] public string? ChecklistNotes { get; set; }
    [BindProperty] public bool SameAsPreviousShift { get; set; }
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public bool AllowSameAsPreviousShift { get; private set; } = true;
    public string FrequencyLabel => NormalizeFrequency(Frequency) == "monthly" ? "Monthly Checklist" : "Daily Checklist";
    public string InspectionTitle => NormalizeFrequency(Frequency) == "monthly" ? "Monthly Vehicle Inspection" : "Daily Vehicle Inspection";

    public IReadOnlyList<VehicleRegisterOption> VehicleRegisterOptions { get; } =
    [
        new(
            "AMB-101",
            "Medic 1",
            "Ambulance",
            SchematicName("operational-ambulance"),
            "operational-ambulance",
            "2026-06-30",
            68420,
            "3/4",
            "Operational",
            "Pass",
            "Pass",
            "Pass",
            "Pass",
            "Pass",
            "Scratch",
            "Minor",
            "Light scratch on left rear locker door. No change reported on previous shift.",
            "Previous shift reported no operational defects. Vehicle remained ready for duty."),
        new(
            "AMB-102",
            "Medic 2",
            "Ambulance",
            SchematicName("ift-ambulance"),
            "ift-ambulance",
            "2026-07-14",
            72210,
            "1/2",
            "Operational with notes",
            "Pass",
            "Pass",
            "Issue",
            "Pass",
            "Pass",
            "Dent",
            "Moderate",
            "Existing dent on right front bumper. Manager already notified.",
            "Previous shift completed with vehicle operational with notes."),
        new(
            "ICU-301",
            "ICU 1",
            "ICU Ambulance",
            SchematicName("icu-ambulance"),
            "icu-ambulance",
            "2026-07-30",
            55380,
            "Full",
            "Operational",
            "Pass",
            "Pass",
            "Pass",
            "Pass",
            "Pass",
            "",
            "",
            "No exterior damage recorded on previous shift.",
            "Previous shift reported ICU vehicle ready for duty."),
        new(
            "RSP-201",
            "Response 1",
            "Response Pickup",
            SchematicName("response-pickup"),
            "response-pickup",
            "2026-08-05",
            41890,
            "Full",
            "Operational",
            "Pass",
            "Pass",
            "Pass",
            "Pass",
            "Pass",
            "",
            "",
            "No exterior damage recorded on previous shift.",
            "Previous shift reported no defects."),
        new(
            "RSP-202",
            "Response 2",
            "Response Sedan",
            SchematicName("response-sedan"),
            "response-sedan",
            "2026-08-22",
            36710,
            "3/4",
            "Operational",
            "Pass",
            "Pass",
            "Pass",
            "Pass",
            "Pass",
            "Scratch",
            "Minor",
            "Small scratch on rear bumper.",
            "Previous shift reported sedan response vehicle ready.")
    ];

    public string EquipmentChecklistUrl => $"/DailyEquipmentChecklist?callsign={Uri.EscapeDataString(Callsign ?? string.Empty)}&registration={Uri.EscapeDataString(Registration ?? string.Empty)}";

    public void OnGet()
    {
        Frequency = NormalizeFrequency(Frequency);
    }

    public IActionResult OnPost()
    {
        Frequency = NormalizeFrequency(Frequency);

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
        ActionSaved = true;
        return Page();
    }

    private static string NormalizeFrequency(string? frequency)
    {
        return string.Equals(frequency, "monthly", StringComparison.OrdinalIgnoreCase) ? "monthly" : "daily";
    }

    private static string SchematicName(string key)
    {
        return VehicleSchematicLibrary.Require(key).DisplayName;
    }

    public sealed record VehicleRegisterOption(
        string Registration,
        string Callsign,
        string VehicleType,
        string SchematicName,
        string SchematicKey,
        string NextServiceDate,
        int PreviousKilometres,
        string PreviousFuelLevel,
        string PreviousVehicleStatus,
        string PreviousLightsStatus,
        string PreviousSirenStatus,
        string PreviousWarningLightsStatus,
        string PreviousTyresStatus,
        string PreviousOpsRadioStatus,
        string PreviousDamageType,
        string PreviousDamageSeverity,
        string PreviousDamageNotes,
        string PreviousChecklistNotes);
}
