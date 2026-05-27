using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class DailyEquipmentChecklistModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public DailyEquipmentChecklistModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty] public string? Callsign { get; set; }
    [BindProperty] public string? Registration { get; set; }
    [BindProperty] public string? AssetId { get; set; }
    [BindProperty] public string? SerialNumber { get; set; }
    [BindProperty] public string? EquipmentName { get; set; }
    [BindProperty] public string? AssignedLocation { get; set; }
    [BindProperty] public string? BatteryState { get; set; }
    [BindProperty] public string? EquipmentStatus { get; set; }
    [BindProperty] public string? ChecklistNotes { get; set; }
    [BindProperty] public bool SameAsPreviousEquipmentCheck { get; set; }
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public bool AllowSameAsPreviousEquipmentCheck { get; private set; } = true;

    public string PreviousAssetId => "MON-001";
    public string PreviousSerialNumber => "LP15-TEST-001";
    public string PreviousEquipmentName => "Defibrillator Monitor";
    public string PreviousAssignedLocation => string.IsNullOrWhiteSpace(Callsign) ? "Linked vehicle" : Callsign;
    public string PreviousBatteryState => "Full";
    public string PreviousEquipmentStatus => "Operational";
    public string PreviousChecklistNotes => "Previous shift reported equipment present, operational, undamaged, and battery-ready.";

    public string LinkedVehicleLabel => string.IsNullOrWhiteSpace(Callsign) && string.IsNullOrWhiteSpace(Registration)
        ? "No vehicle selected"
        : $"{Callsign} {Registration}".Trim();

    public string VehicleChecklistUrl => $"/DailyVehicleChecklist?callsign={Uri.EscapeDataString(Callsign ?? string.Empty)}&registration={Uri.EscapeDataString(Registration ?? string.Empty)}";

    public async Task OnGetAsync(string? callsign, string? registration)
    {
        await LoadSameAsPreviousSettingAsync();
        Callsign = callsign;
        Registration = registration;
        AssignedLocation = string.IsNullOrWhiteSpace(callsign) ? null : callsign;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadSameAsPreviousSettingAsync();

        if (SameAsPreviousEquipmentCheck)
        {
            ApplyPreviousEquipmentValues();
        }

        if (string.IsNullOrWhiteSpace(AssetId) && string.IsNullOrWhiteSpace(SerialNumber) && string.IsNullOrWhiteSpace(EquipmentName))
        {
            StatusMessage = "Enter an asset ID, serial number, or equipment name before saving.";
            return Page();
        }

        StatusMessage = SameAsPreviousEquipmentCheck
            ? $"Equipment checklist marked same as previous shift against linked vehicle: {LinkedVehicleLabel}."
            : $"Equipment checklist ready to save against linked vehicle: {LinkedVehicleLabel}. Database storage, daily inspection session linkage, signed-in profile linkage, and audit logging will be connected in the production data phase.";
        ActionSaved = true;
        return Page();
    }

    private void ApplyPreviousEquipmentValues()
    {
        AssetId = PreviousAssetId;
        SerialNumber = PreviousSerialNumber;
        EquipmentName = PreviousEquipmentName;
        AssignedLocation = PreviousAssignedLocation;
        BatteryState = PreviousBatteryState;
        EquipmentStatus = PreviousEquipmentStatus;
        ChecklistNotes = PreviousChecklistNotes;
    }

    private async Task LoadSameAsPreviousSettingAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            AllowSameAsPreviousEquipmentCheck = true;
            return;
        }

        var setting = await _db.Companies
            .AsNoTracking()
            .Where(company => company.Id == currentUser.CompanyId)
            .Select(company => company.AllowSameAsPreviousEquipmentCheck)
            .FirstOrDefaultAsync();

        AllowSameAsPreviousEquipmentCheck = setting;
    }
}
