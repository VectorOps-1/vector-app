using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
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
    [BindProperty] public bool SameAsPreviousEquipmentCheck { get; set; }
    [BindProperty] public int? PreviousEquipmentReportId { get; set; }
    [BindProperty] public List<EquipmentCheckInput> EquipmentChecks { get; set; } = new();

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public bool AllowSameAsPreviousEquipmentCheck { get; private set; } = true;
    public string DraftStorageKey { get; private set; } = "daily-equipment-readiness:anonymous";
    public string FreshChecklistUrl => "/DailyEquipmentChecklist";

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
        ApplyUserDraftContext(currentUser);
        await LoadSameAsPreviousSettingAsync(currentUser.CompanyId);
        await LoadEquipmentChecklistRowsAsync(currentUser.CompanyId, currentUser.Id, preservePostedValues: false);
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
        await LoadEquipmentChecklistRowsAsync(currentUser.CompanyId, currentUser.Id, preservePostedValues: true);

        if (SameAsPreviousEquipmentCheck && PreviousEquipmentReportId is null)
        {
            StatusMessage = "No previous equipment checklist from another profile is available for this vehicle.";
            return Page();
        }

        if (SameAsPreviousEquipmentCheck)
        {
            ApplyPreviousEquipmentValues();
        }

        var populatedChecks = EquipmentChecks
            .Where(check => !string.IsNullOrWhiteSpace(check.Name) || !string.IsNullOrWhiteSpace(check.SerialOrAssetId))
            .ToList();

        if (populatedChecks.Count == 0)
        {
            StatusMessage = "No vehicle equipment rows are configured yet. Add equipment in Master Setup or the Equipment Register before saving this section.";
            return Page();
        }

        var rowsNeedingIssueNotes = populatedChecks.Count(check =>
            (!check.IsOperational || string.Equals(check.BatteryState, "Low", StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(check.IssueNotes));
        if (rowsNeedingIssueNotes > 0)
        {
            StatusMessage = "Add issue notes for every equipment item marked not operational or with a low battery.";
            return Page();
        }

        var savedCount = await SaveEquipmentReadinessAsync(currentUser, populatedChecks);
        StatusMessage = SameAsPreviousEquipmentCheck
            ? $"{savedCount} equipment rows marked same as previous shift and saved against linked vehicle: {LinkedVehicleLabel}."
            : $"{savedCount} equipment rows saved against linked vehicle: {LinkedVehicleLabel}.";
        ActionSaved = true;
        return Page();
    }

    private void ApplyUserDraftContext(AppUser currentUser)
    {
        var accessView = CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView);
        var vehicleKey = string.IsNullOrWhiteSpace(Registration) ? "no-registration" : Registration.Trim();
        DraftStorageKey = $"daily-equipment-readiness:user-{currentUser.Id}:access-{accessView}:vehicle-{vehicleKey}";
    }

    private async Task LoadEquipmentChecklistRowsAsync(int companyId, int currentUserId, bool preservePostedValues)
    {
        var postedRows = preservePostedValues
            ? EquipmentChecks.ToDictionary(row => row.RowKey, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, EquipmentCheckInput>(StringComparer.OrdinalIgnoreCase);

        var configuredRows = await BuildConfiguredEquipmentRowsAsync(companyId);

        EquipmentChecks = configuredRows.Select(row =>
        {
            if (postedRows.TryGetValue(row.RowKey, out var postedRow))
            {
                row.SerialOrAssetId = NormalizeOptional(postedRow.SerialOrAssetId) ?? row.SerialOrAssetId;
                row.EquipmentItemId = postedRow.EquipmentItemId ?? row.EquipmentItemId;
                row.NextServiceDate = postedRow.NextServiceDate ?? row.NextServiceDate;
                row.BatteryState = NormalizeOptional(postedRow.BatteryState) ?? row.BatteryState;
                row.IsOperational = postedRow.IsOperational;
                row.IssueNotes = NormalizeOptional(postedRow.IssueNotes);
                row.PreviousEquipmentCheckId = postedRow.PreviousEquipmentCheckId;
            }

            return row;
        }).ToList();

        for (var index = 0; index < EquipmentChecks.Count; index++)
        {
            EquipmentChecks[index].SortOrder = index + 1;
        }

        await ApplyPreviousEquipmentCheckValuesAsync(companyId, currentUserId);
    }

    private async Task<List<EquipmentCheckInput>> BuildConfiguredEquipmentRowsAsync(int companyId)
    {
        var vehicle = await FindLinkedVehicleAsync(companyId);
        var activeEquipment = await _db.EquipmentItems
            .AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.Status != "Deleted")
            .OrderBy(item => item.Name)
            .ThenBy(item => item.SerialOrAssetId)
            .ToListAsync();

        if (vehicle is null)
        {
            return activeEquipment
                .Select((item, index) => BuildRowFromEquipmentItem(item, activeEquipment, index + 1))
                .ToList();
        }

        var assignments = await _db.VehicleEquipmentAssignments
            .AsNoTracking()
            .Include(assignment => assignment.EquipmentItem)
            .Where(assignment =>
                assignment.CompanyId == companyId &&
                assignment.Status != "Deleted" &&
                (assignment.VehicleId == vehicle.Id ||
                 (assignment.VehicleId == null &&
                  assignment.VehicleType == vehicle.VehicleType &&
                  (assignment.QualificationLevel == null || assignment.QualificationLevel == vehicle.QualificationLevel))))
            .OrderBy(assignment => assignment.SortOrder)
            .ThenBy(assignment => assignment.ExpectedEquipmentName)
            .ToListAsync();

        if (assignments.Count > 0)
        {
            return assignments
                .Select((assignment, index) => BuildRowFromAssignment(assignment, activeEquipment, index + 1))
                .ToList();
        }

        return activeEquipment
            .Select((item, index) => BuildRowFromEquipmentItem(item, activeEquipment, index + 1))
            .ToList();
    }

    private EquipmentCheckInput BuildRowFromAssignment(
        VehicleEquipmentAssignment assignment,
        IReadOnlyList<EquipmentItem> activeEquipment,
        int sortOrder)
    {
        var equipmentItem = assignment.EquipmentItem;
        var name = NormalizeOptional(assignment.ExpectedEquipmentName)
            ?? equipmentItem?.Name
            ?? "Equipment item";
        var type = NormalizeOptional(assignment.ExpectedEquipmentType) ?? equipmentItem?.EquipmentType;
        var model = NormalizeOptional(assignment.ExpectedModel) ?? equipmentItem?.Model;

        var row = new EquipmentCheckInput
        {
            VehicleEquipmentAssignmentId = assignment.Id,
            EquipmentItemId = equipmentItem?.Id,
            Name = name,
            EquipmentType = type,
            Model = model,
            SerialOrAssetId = equipmentItem?.SerialOrAssetId,
            NextServiceDate = equipmentItem?.NextServiceDate,
            RequiresBatteryCheck = assignment.RequiresBatteryCheck || equipmentItem?.BatteryRequired == true,
            BatteryState = assignment.RequiresBatteryCheck || equipmentItem?.BatteryRequired == true ? "Full" : "Not applicable",
            IsOperational = true,
            PreviousBatteryState = "Full",
            PreviousIsOperational = true,
            SortOrder = sortOrder
        };

        row.SerialOptions = BuildSerialOptions(activeEquipment, name, type, model, row.SerialOrAssetId);
        return row;
    }

    private EquipmentCheckInput BuildRowFromEquipmentItem(
        EquipmentItem item,
        IReadOnlyList<EquipmentItem> activeEquipment,
        int sortOrder)
    {
        var row = new EquipmentCheckInput
        {
            EquipmentItemId = item.Id,
            Name = item.Name,
            EquipmentType = item.EquipmentType,
            Model = item.Model,
            SerialOrAssetId = item.SerialOrAssetId,
            NextServiceDate = item.NextServiceDate,
            RequiresBatteryCheck = item.BatteryRequired,
            BatteryState = item.BatteryRequired ? "Full" : "Not applicable",
            IsOperational = true,
            PreviousBatteryState = "Full",
            PreviousIsOperational = true,
            SortOrder = sortOrder
        };

        row.SerialOptions = BuildSerialOptions(activeEquipment, item.Name, item.EquipmentType, item.Model, row.SerialOrAssetId);
        return row;
    }

    private List<EquipmentSerialOption> BuildSerialOptions(
        IReadOnlyList<EquipmentItem> equipmentItems,
        string? equipmentName,
        string? equipmentType,
        string? model,
        string? selectedSerial)
    {
        var matches = equipmentItems
            .Where(item =>
                ValuesMatch(item.Name, equipmentName) ||
                ValuesMatch(item.EquipmentType, equipmentType) ||
                ValuesMatch(item.Model, model))
            .OrderBy(item => item.Name)
            .ThenBy(item => item.SerialOrAssetId)
            .ToList();

        if (matches.Count == 0)
        {
            matches = equipmentItems
                .OrderBy(item => item.Name)
                .ThenBy(item => item.SerialOrAssetId)
                .ToList();
        }

        var options = matches
            .Where(item => !string.IsNullOrWhiteSpace(item.SerialOrAssetId))
            .Select(item => new EquipmentSerialOption(
                item.SerialOrAssetId!,
                $"{item.SerialOrAssetId} - {item.Name}",
                item.NextServiceDate,
                item.Id))
            .ToList();

        if (!string.IsNullOrWhiteSpace(selectedSerial) &&
            options.All(option => !string.Equals(option.Value, selectedSerial, StringComparison.OrdinalIgnoreCase)))
        {
            options.Insert(0, new EquipmentSerialOption(selectedSerial, selectedSerial, null, null));
        }

        return options;
    }

    private async Task<int> SaveEquipmentReadinessAsync(AppUser currentUser, IReadOnlyList<EquipmentCheckInput> populatedChecks)
    {
        var now = DateTime.UtcNow;
        var vehicle = await EnsureVehicleAsync(currentUser.CompanyId, now);
        var report = await EnsureReadinessReportAsync(currentUser, vehicle, now);
        var nextSortOrder = await NextEquipmentSortOrderAsync(report.Id);
        var savedChecks = new List<DailyVehicleEquipmentCheck>();

        foreach (var row in populatedChecks.OrderBy(check => check.SortOrder))
        {
            var equipmentItem = await FindEquipmentItemAsync(currentUser.CompanyId, row);
            var check = new DailyVehicleEquipmentCheck
            {
                CompanyId = currentUser.CompanyId,
                DailyVehicleReadinessReportId = report.Id,
                VehicleEquipmentAssignmentId = row.VehicleEquipmentAssignmentId,
                EquipmentItemId = equipmentItem?.Id ?? row.EquipmentItemId,
                EquipmentName = NormalizeOptional(row.Name) ?? equipmentItem?.Name ?? "Equipment item",
                EquipmentType = NormalizeOptional(row.EquipmentType) ?? equipmentItem?.EquipmentType,
                Model = NormalizeOptional(row.Model) ?? equipmentItem?.Model,
                SerialOrAssetId = NormalizeOptional(row.SerialOrAssetId) ?? equipmentItem?.SerialOrAssetId,
                NextServiceDateAtCheck = row.NextServiceDate ?? equipmentItem?.NextServiceDate,
                BatteryStatus = NormalizeOptional(row.BatteryState),
                IsOperational = row.IsOperational,
                IssueNotes = NormalizeOptional(row.IssueNotes),
                PresentStatus = "Present",
                ReadinessImpact = !row.IsOperational || string.Equals(row.BatteryState, "Low", StringComparison.OrdinalIgnoreCase) ? "Warning" : "None",
                SameAsPreviousShiftUsed = SameAsPreviousEquipmentCheck,
                CopiedFromDailyVehicleEquipmentCheckId = SameAsPreviousEquipmentCheck ? row.PreviousEquipmentCheckId : null,
                SameAsPreviousAppliedAtUtc = SameAsPreviousEquipmentCheck ? now : null,
                SortOrder = nextSortOrder++,
                CreatedAtUtc = now
            };

            savedChecks.Add(check);
            _db.DailyVehicleEquipmentChecks.Add(check);
        }

        report.WorkflowStatus = "Saved";
        report.LastSavedSection = "Equipment";
        report.LastSavedAtUtc = now;
        report.UpdatedAtUtc = now;
        report.EquipmentSameAsPreviousShiftUsed = SameAsPreviousEquipmentCheck;
        report.EquipmentSameAsPreviousSourceReportId = SameAsPreviousEquipmentCheck && PreviousEquipmentReportId != report.Id
            ? PreviousEquipmentReportId
            : null;
        report.EquipmentSameAsPreviousAppliedAtUtc = SameAsPreviousEquipmentCheck ? now : null;
        report.EquipmentSameAsPreviousCopiedSummary = SameAsPreviousEquipmentCheck
            ? $"{savedChecks.Count} equipment rows copied from previous shift values; {savedChecks.Count(check => check.CopiedFromDailyVehicleEquipmentCheckId is not null)} rows linked to previous equipment checks."
            : null;
        report.WarningIssueCount += savedChecks.Count(check => check.ReadinessImpact == "Warning");
        report.SubmittedAtUtc ??= now;

        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Equipment readiness saved",
            EntityType = "DailyVehicleReadinessReport",
            EntityId = report.Id,
            Details = $"{currentUser.FullName} saved {savedChecks.Count} equipment readiness rows for {report.VehicleRegistrationNumber} from {CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView)} access.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
        return savedChecks.Count;
    }

    private async Task ApplyPreviousEquipmentCheckValuesAsync(int companyId, int currentUserId)
    {
        var vehicle = await FindLinkedVehicleAsync(companyId);
        if (vehicle is null)
        {
            return;
        }

        var previousReport = await _db.DailyVehicleReadinessReports
            .AsNoTracking()
            .Include(report => report.EquipmentChecks)
            .Where(report =>
                report.CompanyId == companyId &&
                report.VehicleId == vehicle.Id &&
                report.PerformedByUserId != currentUserId)
            .OrderByDescending(report => report.InspectionDateUtc)
            .FirstOrDefaultAsync();

        if (previousReport is null)
        {
            return;
        }

        PreviousEquipmentReportId = previousReport.Id;

        foreach (var row in EquipmentChecks)
        {
            var previousCheck = previousReport.EquipmentChecks
                .OrderByDescending(check => check.CreatedAtUtc)
                .FirstOrDefault(check =>
                    (row.VehicleEquipmentAssignmentId is not null && check.VehicleEquipmentAssignmentId == row.VehicleEquipmentAssignmentId) ||
                    (row.EquipmentItemId is not null && check.EquipmentItemId == row.EquipmentItemId) ||
                    (!string.IsNullOrWhiteSpace(row.SerialOrAssetId) && check.SerialOrAssetId == row.SerialOrAssetId) ||
                    (!string.IsNullOrWhiteSpace(row.Name) && check.EquipmentName == row.Name));

            if (previousCheck is null)
            {
                continue;
            }

            row.PreviousSerialOrAssetId = previousCheck.SerialOrAssetId;
            row.PreviousBatteryState = NormalizeOptional(previousCheck.BatteryStatus)
                ?? (row.RequiresBatteryCheck ? "Full" : "Not applicable");
            row.PreviousIsOperational = previousCheck.IsOperational;
            row.PreviousIssueNotes = previousCheck.IssueNotes;
            row.PreviousEquipmentCheckId = previousCheck.Id;
        }
    }

    private async Task<Vehicle?> FindLinkedVehicleAsync(int companyId)
    {
        var registration = NormalizeOptional(Registration);
        var callsign = NormalizeOptional(Callsign);

        if (registration is null && callsign is null)
        {
            return null;
        }

        return await _db.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(vehicle =>
                vehicle.CompanyId == companyId &&
                ((registration != null && vehicle.RegistrationNumber == registration) ||
                 (callsign != null && vehicle.Callsign == callsign)));
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

    private async Task<EquipmentItem?> FindEquipmentItemAsync(int companyId, EquipmentCheckInput row)
    {
        if (row.EquipmentItemId is not null)
        {
            var byId = await _db.EquipmentItems.FirstOrDefaultAsync(item =>
                item.CompanyId == companyId &&
                item.Id == row.EquipmentItemId);

            if (byId is not null)
            {
                return byId;
            }
        }

        var serialOrAssetId = NormalizeOptional(row.SerialOrAssetId);
        var equipmentName = NormalizeOptional(row.Name);

        return await _db.EquipmentItems.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            ((serialOrAssetId != null && item.SerialOrAssetId == serialOrAssetId) ||
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
        foreach (var row in EquipmentChecks)
        {
            if (!string.IsNullOrWhiteSpace(row.PreviousSerialOrAssetId))
            {
                row.SerialOrAssetId = row.PreviousSerialOrAssetId;
            }

            row.BatteryState = row.RequiresBatteryCheck ? row.PreviousBatteryState ?? "Full" : "Not applicable";
            row.IsOperational = row.PreviousIsOperational;
            row.IssueNotes = row.PreviousIssueNotes;
        }
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

    private static bool ValuesMatch(string? first, string? second)
    {
        return !string.IsNullOrWhiteSpace(first) &&
            !string.IsNullOrWhiteSpace(second) &&
            string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public class EquipmentCheckInput
{
    public int? VehicleEquipmentAssignmentId { get; set; }
    public int? EquipmentItemId { get; set; }
    public string? Name { get; set; }
    public string? EquipmentType { get; set; }
    public string? Model { get; set; }
    public string? SerialOrAssetId { get; set; }
    public DateTime? NextServiceDate { get; set; }
    public bool RequiresBatteryCheck { get; set; }
    public string? BatteryState { get; set; }
    public bool IsOperational { get; set; } = true;
    public string? IssueNotes { get; set; }
    public string? PreviousSerialOrAssetId { get; set; }
    public string? PreviousBatteryState { get; set; }
    public bool PreviousIsOperational { get; set; } = true;
    public string? PreviousIssueNotes { get; set; }
    public int? PreviousEquipmentCheckId { get; set; }
    public int SortOrder { get; set; }
    public List<EquipmentSerialOption> SerialOptions { get; set; } = new();

    public string RowKey => VehicleEquipmentAssignmentId is not null
        ? $"assignment:{VehicleEquipmentAssignmentId}"
        : EquipmentItemId is not null
            ? $"equipment:{EquipmentItemId}"
            : $"manual:{Name}";
}

public record EquipmentSerialOption(string Value, string Text, DateTime? NextServiceDate, int? EquipmentItemId);
