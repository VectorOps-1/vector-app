using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class DailyEquipmentChecklistModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;

    public DailyEquipmentChecklistModel(VectorDbContext db, CurrentUserService currentUser, LocationOptionService locationOptions)
    {
        _db = db;
        _currentUser = currentUser;
        _locationOptions = locationOptions;
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
    public string DraftStorageKey { get; private set; } = "daily-equipment-readiness:anonymous";
    public string FreshChecklistUrl => "/DailyEquipmentChecklist";
    public List<SelectListItem> LocationOptions { get; private set; } = new();

    public string PreviousAssetId => "MON-001";
    public string PreviousSerialNumber => "LP15-TEST-001";
    public string PreviousEquipmentName => "Defibrillator Monitor";
    public string PreviousAssignedLocation => LocationOptionService.BuildVehicleLocationValue(Callsign, Registration);
    public string PreviousBatteryState => "Full";
    public string PreviousEquipmentStatus => "Operational";
    public string PreviousChecklistNotes => "Previous shift reported equipment present, operational, undamaged, and battery-ready.";

    public string LinkedVehicleLabel => string.IsNullOrWhiteSpace(Callsign) && string.IsNullOrWhiteSpace(Registration)
        ? "No vehicle selected"
        : $"{Callsign} {Registration}".Trim();

    public string VehicleChecklistUrl => $"/DailyVehicleChecklist?callsign={Uri.EscapeDataString(Callsign ?? string.Empty)}&registration={Uri.EscapeDataString(Registration ?? string.Empty)}";

    public async Task<IActionResult> OnGetAsync(string? callsign, string? registration)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        Callsign = callsign;
        Registration = registration;
        AssignedLocation = string.IsNullOrWhiteSpace(callsign) && string.IsNullOrWhiteSpace(registration)
            ? null
            : PreviousAssignedLocation;
        ApplyUserDraftContext(currentUser);
        await LoadSameAsPreviousSettingAsync(currentUser.CompanyId);
        await LoadLocationOptionsAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        ApplyUserDraftContext(currentUser);
        await LoadSameAsPreviousSettingAsync(currentUser.CompanyId);
        await LoadLocationOptionsAsync(currentUser.CompanyId);

        if (SameAsPreviousEquipmentCheck)
        {
            ApplyPreviousEquipmentValues();
        }

        if (string.IsNullOrWhiteSpace(AssetId) && string.IsNullOrWhiteSpace(SerialNumber) && string.IsNullOrWhiteSpace(EquipmentName))
        {
            StatusMessage = "Enter an asset ID, serial number, or equipment name before saving.";
            return Page();
        }

        var check = await SaveEquipmentReadinessAsync(currentUser);
        StatusMessage = SameAsPreviousEquipmentCheck
            ? $"Equipment section marked same as previous shift and saved against linked vehicle: {LinkedVehicleLabel}."
            : $"Equipment section saved against linked vehicle: {LinkedVehicleLabel}.";
        ActionSaved = true;
        return Page();
    }

    private void ApplyUserDraftContext(AppUser currentUser)
    {
        var accessView = CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView);
        var vehicleKey = string.IsNullOrWhiteSpace(Registration) ? "no-registration" : Registration.Trim();
        DraftStorageKey = $"daily-equipment-readiness:user-{currentUser.Id}:access-{accessView}:vehicle-{vehicleKey}";
    }

    private async Task<DailyVehicleEquipmentCheck> SaveEquipmentReadinessAsync(AppUser currentUser)
    {
        var now = DateTime.UtcNow;
        var vehicle = await EnsureVehicleAsync(currentUser.CompanyId, now);
        var report = await EnsureReadinessReportAsync(currentUser, vehicle, now);
        var equipmentItem = await FindEquipmentItemAsync(currentUser.CompanyId);

        var check = new DailyVehicleEquipmentCheck
        {
            CompanyId = currentUser.CompanyId,
            DailyVehicleReadinessReportId = report.Id,
            EquipmentItemId = equipmentItem?.Id,
            EquipmentName = NormalizeOptional(EquipmentName) ?? equipmentItem?.Name ?? NormalizeOptional(AssetId) ?? NormalizeOptional(SerialNumber) ?? "Equipment item",
            SerialOrAssetId = NormalizeOptional(SerialNumber) ?? NormalizeOptional(AssetId) ?? equipmentItem?.SerialOrAssetId,
            BatteryStatus = NormalizeOptional(BatteryState),
            PresentStatus = string.Equals(EquipmentStatus, "Missing", StringComparison.OrdinalIgnoreCase) ? "Missing" : "Present",
            DamageStatus = NormalizeOptional(EquipmentStatus),
            ReadinessImpact = string.Equals(EquipmentStatus, "Faulty", StringComparison.OrdinalIgnoreCase)
                || string.Equals(EquipmentStatus, "Missing", StringComparison.OrdinalIgnoreCase)
                    ? "Critical"
                    : string.Equals(EquipmentStatus, "Operational with notes", StringComparison.OrdinalIgnoreCase) ? "Warning" : "None",
            SameAsPreviousShiftUsed = SameAsPreviousEquipmentCheck,
            SameAsPreviousAppliedAtUtc = SameAsPreviousEquipmentCheck ? now : null,
            Notes = NormalizeOptional(ChecklistNotes),
            SortOrder = await NextEquipmentSortOrderAsync(report.Id),
            CreatedAtUtc = now
        };

        report.WorkflowStatus = "Saved";
        report.LastSavedSection = "Equipment";
        report.LastSavedAtUtc = now;
        report.UpdatedAtUtc = now;
        report.EquipmentSameAsPreviousShiftUsed = SameAsPreviousEquipmentCheck;
        report.EquipmentSameAsPreviousAppliedAtUtc = SameAsPreviousEquipmentCheck ? now : null;
        report.SubmittedAtUtc ??= now;

        _db.DailyVehicleEquipmentChecks.Add(check);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Equipment readiness saved",
            EntityType = "DailyVehicleEquipmentCheck",
            EntityId = check.Id,
            Details = $"{currentUser.FullName} saved equipment readiness for {report.VehicleRegistrationNumber} from {CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView)} access.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
        return check;
    }

    private async Task<Vehicle> EnsureVehicleAsync(int companyId, DateTime now)
    {
        var registration = NormalizeOptional(Registration)
            ?? NormalizeOptional(Callsign)
            ?? $"UNREGISTERED-{now:yyyyMMddHHmmss}";

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            item.RegistrationNumber == registration);

        if (vehicle is not null)
        {
            return vehicle;
        }

        vehicle = new Vehicle
        {
            CompanyId = companyId,
            RegistrationNumber = registration,
            Callsign = NormalizeOptional(Callsign) ?? registration,
            VehicleType = "Vehicle",
            Status = "Active",
            CreatedAtUtc = now
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return vehicle;
    }

    private async Task<DailyVehicleReadinessReport> EnsureReadinessReportAsync(AppUser currentUser, Vehicle vehicle, DateTime now)
    {
        var recentCutoff = now.AddHours(-12);
        var report = await _db.DailyVehicleReadinessReports
            .Where(item =>
                item.CompanyId == currentUser.CompanyId &&
                item.VehicleId == vehicle.Id &&
                item.PerformedByUserId == currentUser.Id &&
                item.InspectionDateUtc >= recentCutoff)
            .OrderByDescending(item => item.InspectionDateUtc)
            .FirstOrDefaultAsync();

        if (report is not null)
        {
            return report;
        }

        report = new DailyVehicleReadinessReport
        {
            CompanyId = currentUser.CompanyId,
            VehicleId = vehicle.Id,
            PerformedByUserId = currentUser.Id,
            InspectionDateUtc = now,
            ShiftStartedAtUtc = now,
            ShiftEndsAtUtc = now.AddHours(12),
            LastSavedAtUtc = now,
            WorkflowStatus = "Saved",
            LastSavedSection = "Equipment",
            VehicleRegistrationNumber = vehicle.RegistrationNumber,
            CallsignAtCheck = vehicle.Callsign,
            VehicleTypeAtCheck = vehicle.VehicleType,
            VehicleNextServiceDateAtCheck = vehicle.NextServiceDate,
            ReadinessStatus = "Pending",
            CreatedAtUtc = now,
            SubmittedAtUtc = now
        };

        _db.DailyVehicleReadinessReports.Add(report);
        await _db.SaveChangesAsync();
        return report;
    }

    private async Task<EquipmentItem?> FindEquipmentItemAsync(int companyId)
    {
        var assetId = NormalizeOptional(AssetId);
        var serialNumber = NormalizeOptional(SerialNumber);
        var equipmentName = NormalizeOptional(EquipmentName);

        return await _db.EquipmentItems.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            ((assetId != null && item.SerialOrAssetId == assetId) ||
             (serialNumber != null && item.SerialOrAssetId == serialNumber) ||
             (equipmentName != null && item.Name == equipmentName)));
    }

    private async Task<int> NextEquipmentSortOrderAsync(int reportId)
    {
        var currentMax = await _db.DailyVehicleEquipmentChecks
            .Where(check => check.DailyVehicleReadinessReportId == reportId)
            .Select(check => (int?)check.SortOrder)
            .MaxAsync();

        return (currentMax ?? 0) + 1;
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

    private async Task LoadSameAsPreviousSettingAsync(int companyId)
    {
        var setting = await _db.Companies
            .AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => company.AllowSameAsPreviousEquipmentCheck)
            .FirstOrDefaultAsync();

        AllowSameAsPreviousEquipmentCheck = setting;
    }

    private async Task LoadLocationOptionsAsync(int companyId)
    {
        LocationOptions = await _locationOptions.GetAssetLocationOptionsAsync(companyId);
        var linkedVehicleLocation = PreviousAssignedLocation;
        if (!string.IsNullOrWhiteSpace(Callsign)
            && LocationOptions.All(option => option.Value != linkedVehicleLocation))
        {
            LocationOptions.Insert(0, new SelectListItem
            {
                Value = linkedVehicleLocation,
                Text = linkedVehicleLocation
            });
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
