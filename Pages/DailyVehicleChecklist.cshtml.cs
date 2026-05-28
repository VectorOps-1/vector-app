using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class DailyVehicleChecklistModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public DailyVehicleChecklistModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string Frequency { get; set; } = "daily";
    [BindProperty(SupportsGet = true)] public string? Callsign { get; set; }
    [BindProperty(SupportsGet = true)] public string? Registration { get; set; }
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
    [BindProperty] public int? PreviousVehicleReportId { get; set; }
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public bool AllowSameAsPreviousVehicleInspection { get; private set; } = true;
    public string DraftStorageKey { get; private set; } = "daily-vehicle-readiness:anonymous";
    public string FreshChecklistUrl => $"/DailyVehicleChecklist?frequency={Uri.EscapeDataString(Frequency)}";
    public string FrequencyLabel => NormalizeFrequency(Frequency) == "monthly" ? "Monthly Checklist" : "Daily Readiness";
    public string InspectionTitle => NormalizeFrequency(Frequency) == "monthly" ? "Monthly Vehicle Inspection" : "Daily Vehicle Readiness";

    public List<VehicleRegisterOption> VehicleRegisterOptions { get; private set; } = new();

    private static readonly IReadOnlyList<VehicleRegisterOption> DefaultVehicleRegisterOptions =
    [
        new(
            "AMB-101",
            "Medic 1",
            "Ambulance",
            SchematicName("operational-ambulance"),
            "operational-ambulance",
            "2026-06-30",
            null,
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            ""),
        new(
            "AMB-102",
            "Medic 2",
            "Ambulance",
            SchematicName("ift-ambulance"),
            "ift-ambulance",
            "2026-07-14",
            null,
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            ""),
        new(
            "ICU-301",
            "ICU 1",
            "ICU Ambulance",
            SchematicName("icu-ambulance"),
            "icu-ambulance",
            "2026-07-30",
            null,
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            ""),
        new(
            "RSP-201",
            "Response 1",
            "Response Pickup",
            SchematicName("response-pickup"),
            "response-pickup",
            "2026-08-05",
            null,
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            ""),
        new(
            "RSP-202",
            "Response 2",
            "Response Sedan",
            SchematicName("response-sedan"),
            "response-sedan",
            "2026-08-22",
            null,
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "")
    ];

    public string EquipmentChecklistUrl => $"/DailyEquipmentChecklist?callsign={Uri.EscapeDataString(Callsign ?? string.Empty)}&registration={Uri.EscapeDataString(Registration ?? string.Empty)}";

    public async Task<IActionResult> OnGetAsync()
    {
        Frequency = NormalizeFrequency(Frequency);
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        ApplyUserDraftContext(currentUser);
        await LoadSameAsPreviousSettingAsync(currentUser.CompanyId);
        await LoadVehicleRegisterOptionsAsync(currentUser.CompanyId, currentUser.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Frequency = NormalizeFrequency(Frequency);
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        ApplyUserDraftContext(currentUser);
        await LoadSameAsPreviousSettingAsync(currentUser.CompanyId);
        await LoadVehicleRegisterOptionsAsync(currentUser.CompanyId, currentUser.Id);

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

        if (SameAsPreviousShift && PreviousVehicleReportId is null)
        {
            StatusMessage = "No previous checklist from another profile is available for this vehicle.";
            return Page();
        }

        var report = await SaveVehicleReadinessAsync(currentUser);
        StatusMessage = $"{InspectionTitle} saved for {report.VehicleRegistrationNumber}. A fresh check is ready.";
        ActionSaved = true;
        return Page();
    }

    private void ApplyUserDraftContext(AppUser currentUser)
    {
        var accessView = CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView);
        DraftStorageKey = $"daily-vehicle-readiness:user-{currentUser.Id}:access-{accessView}:frequency-{Frequency}";
    }

    private async Task LoadSameAsPreviousSettingAsync(int companyId)
    {
        var setting = await _db.Companies
            .AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => company.AllowSameAsPreviousVehicleInspection)
            .FirstOrDefaultAsync();

        AllowSameAsPreviousVehicleInspection = setting;
    }

    private async Task LoadVehicleRegisterOptionsAsync(int companyId, int currentUserId)
    {
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.CompanyId == companyId)
            .ToListAsync();
        var vehiclesByRegistration = vehicles
            .GroupBy(vehicle => vehicle.RegistrationNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var vehicleIds = vehicles.Select(vehicle => vehicle.Id).ToList();
        var latestReports = vehicleIds.Count == 0
            ? new List<DailyVehicleReadinessReport>()
            : await _db.DailyVehicleReadinessReports
                .AsNoTracking()
                .Include(report => report.PerformedByUser)
                .Where(report =>
                    report.CompanyId == companyId &&
                    vehicleIds.Contains(report.VehicleId) &&
                    report.PerformedByUserId != currentUserId)
                .OrderByDescending(report => report.InspectionDateUtc)
                .ToListAsync();
        var latestReportByVehicleId = latestReports
            .GroupBy(report => report.VehicleId)
            .ToDictionary(group => group.Key, group => group.First());
        var publishedTemplates = await _db.ChecklistTemplates
            .AsNoTracking()
            .Where(template =>
                template.CompanyId == companyId &&
                template.ChecklistType == "Vehicle" &&
                template.IsPublished)
            .ToListAsync();

        VehicleRegisterOptions = DefaultVehicleRegisterOptions
            .Select(option =>
            {
                var vehicleType = vehiclesByRegistration.TryGetValue(option.Registration, out var registeredVehicle)
                    ? registeredVehicle.VehicleType
                    : option.VehicleType;
                var publishedTemplate = SelectPublishedTemplateForVehicleType(publishedTemplates, vehicleType);

                if (!vehiclesByRegistration.TryGetValue(option.Registration, out var vehicle) ||
                    !latestReportByVehicleId.TryGetValue(vehicle.Id, out var report))
                {
                    return option with
                    {
                        PublishedChecklistTemplateId = publishedTemplate?.Id,
                        PublishedChecklistTemplateName = publishedTemplate?.Name ?? ""
                    };
                }

                return option with
                {
                    Callsign = vehicle.Callsign,
                    VehicleType = vehicle.VehicleType,
                    SchematicKey = VehicleSchematicLibrary.Find(vehicle.SchematicType ?? string.Empty)?.Key ?? option.SchematicKey,
                    SchematicName = VehicleSchematicLibrary.Find(vehicle.SchematicType ?? string.Empty)?.DisplayName ?? option.SchematicName,
                    NextServiceDate = (vehicle.NextServiceDate ?? option.NextServiceDateAsDate)?.ToString("yyyy-MM-dd") ?? option.NextServiceDate,
                    PreviousSourceReportId = report.Id,
                    PreviousSourcePerformer = report.PerformedByUser?.FullName ?? "another profile",
                    PreviousKilometres = ExtractNoteValue(report.OperationalNotes, "Kilometres") ?? "",
                    PreviousFuelLevel = ExtractNoteValue(report.OperationalNotes, "Fuel") ?? "",
                    PreviousVehicleStatus = report.ReadinessStatus,
                    PreviousLightsStatus = report.LightsStatus ?? "",
                    PreviousSirenStatus = report.SirensStatus ?? "",
                    PreviousWarningLightsStatus = report.WarningLightsStatus ?? "",
                    PreviousTyresStatus = report.TyresStatus ?? "",
                    PreviousOpsRadioStatus = report.RadioConnectivityStatus ?? "",
                    PreviousDamageType = ExtractNoteValue(report.SchematicNotes, "Damage type") ?? "",
                    PreviousDamageSeverity = ExtractNoteValue(report.SchematicNotes, "Severity") ?? "",
                    PreviousDamageNotes = report.DamageNotes ?? "",
                    PreviousChecklistNotes = report.GeneralNotes ?? "",
                    PublishedChecklistTemplateId = publishedTemplate?.Id,
                    PublishedChecklistTemplateName = publishedTemplate?.Name ?? ""
                };
            })
            .ToList();
    }

    private async Task<DailyVehicleReadinessReport> SaveVehicleReadinessAsync(AppUser currentUser)
    {
        var now = DateTime.UtcNow;
        var vehicle = await EnsureVehicleAsync(currentUser.CompanyId, now);
        var selectedVehicle = VehicleRegisterOptions.FirstOrDefault(option =>
            string.Equals(option.Registration, Registration, StringComparison.OrdinalIgnoreCase));

        var report = new DailyVehicleReadinessReport
        {
            CompanyId = currentUser.CompanyId,
            VehicleId = vehicle.Id,
            PerformedByUserId = currentUser.Id,
            InspectionDateUtc = now,
            ShiftStartedAtUtc = now,
            ShiftEndsAtUtc = now.AddHours(12),
            LastSavedAtUtc = now,
            WorkflowStatus = "Saved",
            LastSavedSection = "Vehicle",
            VehicleRegistrationNumber = vehicle.RegistrationNumber,
            CallsignAtCheck = NormalizeOptional(Callsign) ?? vehicle.Callsign,
            VehicleTypeAtCheck = NormalizeOptional(VehicleType) ?? vehicle.VehicleType,
            SchematicTypeAtCheck = selectedVehicle?.SchematicKey ?? vehicle.SchematicType,
            VehicleNextServiceDateAtCheck = NextServiceDate ?? vehicle.NextServiceDate,
            SameAsPreviousShiftUsed = SameAsPreviousShift,
            VehicleSameAsPreviousShiftUsed = SameAsPreviousShift,
            VehicleSameAsPreviousSourceReportId = SameAsPreviousShift ? PreviousVehicleReportId : null,
            VehicleSameAsPreviousAppliedAtUtc = SameAsPreviousShift ? now : null,
            VehicleSameAsPreviousCopiedSummary = SameAsPreviousShift && PreviousVehicleReportId.HasValue
                ? $"Vehicle inspection values copied from readiness report #{PreviousVehicleReportId.Value} completed by another profile."
                : null,
            LightsStatus = NormalizeOptional(LightsStatus),
            SirensStatus = NormalizeOptional(SirenStatus),
            WarningLightsStatus = NormalizeOptional(WarningLightsStatus),
            TyresStatus = NormalizeOptional(TyresStatus),
            RadioConnectivityStatus = NormalizeOptional(OpsRadioStatus),
            OperationalNotes = BuildOperationalNotes(),
            DamageNotes = NormalizeOptional(DamageNotes),
            SchematicNotes = BuildSchematicNotes(),
            GeneralNotes = NormalizeOptional(ChecklistNotes),
            ReadinessStatus = NormalizeOptional(VehicleStatus) ?? "Pending",
            CriticalIssueCount = CountCriticalIssues(),
            WarningIssueCount = CountWarningIssues(),
            CreatedAtUtc = now,
            SubmittedAtUtc = now
        };

        _db.DailyVehicleReadinessReports.Add(report);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Vehicle readiness saved",
            EntityType = "DailyVehicleReadinessReport",
            EntityId = report.Id,
            Details = $"{currentUser.FullName} saved {InspectionTitle} for {report.VehicleRegistrationNumber} from {CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView)} access.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
        return report;
    }

    private async Task<Vehicle> EnsureVehicleAsync(int companyId, DateTime now)
    {
        var selectedVehicle = VehicleRegisterOptions.FirstOrDefault(option =>
            string.Equals(option.Registration, Registration, StringComparison.OrdinalIgnoreCase));
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
            Callsign = NormalizeOptional(Callsign) ?? selectedVehicle?.Callsign ?? registration,
            VehicleType = NormalizeOptional(VehicleType) ?? selectedVehicle?.VehicleType ?? "Vehicle",
            SchematicType = selectedVehicle?.SchematicKey,
            NextServiceDate = NextServiceDate,
            Status = "Active",
            CreatedAtUtc = now
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return vehicle;
    }

    private string? BuildOperationalNotes()
    {
        var values = new List<string>();
        if (Kilometres.HasValue)
        {
            values.Add($"Kilometres: {Kilometres.Value}");
        }
        if (!string.IsNullOrWhiteSpace(FuelLevel))
        {
            values.Add($"Fuel: {FuelLevel.Trim()}");
        }

        return values.Count == 0 ? null : string.Join("; ", values);
    }

    private string? BuildSchematicNotes()
    {
        var values = new List<string>();
        if (!string.IsNullOrWhiteSpace(DamageType))
        {
            values.Add($"Damage type: {DamageType.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(DamageSeverity))
        {
            values.Add($"Severity: {DamageSeverity.Trim()}");
        }

        return values.Count == 0 ? null : string.Join("; ", values);
    }

    private int CountCriticalIssues()
    {
        return new[] { VehicleStatus, LightsStatus, SirenStatus, WarningLightsStatus, TyresStatus, OpsRadioStatus }
            .Count(value => string.Equals(value, "Fail", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Out of service", StringComparison.OrdinalIgnoreCase));
    }

    private int CountWarningIssues()
    {
        return new[] { VehicleStatus, LightsStatus, SirenStatus, WarningLightsStatus, TyresStatus, OpsRadioStatus }
            .Count(value => string.Equals(value, "Issue", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Operational with notes", StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? ExtractNoteValue(string? notes, string label)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        foreach (var part in notes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var prefix = $"{label}:";
            if (part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return part[prefix.Length..].Trim();
            }
        }

        return null;
    }

    private static ChecklistTemplate? SelectPublishedTemplateForVehicleType(
        IReadOnlyList<ChecklistTemplate> templates,
        string? vehicleType)
    {
        if (templates.Count == 0)
        {
            return null;
        }

        var normalizedVehicleType = NormalizeOptional(vehicleType) ?? "All Vehicles";
        return templates.FirstOrDefault(template => string.Equals(template.TargetVehicleType, normalizedVehicleType, StringComparison.OrdinalIgnoreCase))
            ?? templates.FirstOrDefault(template => VehicleTypeMatchesTemplateTarget(normalizedVehicleType, template.TargetVehicleType))
            ?? templates.FirstOrDefault(template => string.Equals(template.TargetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase));
    }

    private static bool VehicleTypeMatchesTemplateTarget(string vehicleType, string targetVehicleType)
    {
        if (string.Equals(targetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (vehicleType.Contains("ICU", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(targetVehicleType, "ICU Ambulance", StringComparison.OrdinalIgnoreCase);
        }

        if (vehicleType.Contains("Ambulance", StringComparison.OrdinalIgnoreCase))
        {
            return targetVehicleType.Contains("Ambulance", StringComparison.OrdinalIgnoreCase);
        }

        if (vehicleType.Contains("Response", StringComparison.OrdinalIgnoreCase))
        {
            return targetVehicleType.Contains("Response", StringComparison.OrdinalIgnoreCase);
        }

        if (vehicleType.Contains("Rescue", StringComparison.OrdinalIgnoreCase))
        {
            return targetVehicleType.Contains("Rescue", StringComparison.OrdinalIgnoreCase);
        }

        return false;
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
        int? PreviousSourceReportId,
        string PreviousSourcePerformer,
        string PreviousKilometres,
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
        string PreviousChecklistNotes,
        int? PublishedChecklistTemplateId = null,
        string PublishedChecklistTemplateName = "")
    {
        public DateTime? NextServiceDateAsDate =>
            DateTime.TryParse(NextServiceDate, out var date) ? date : null;
    }
}
