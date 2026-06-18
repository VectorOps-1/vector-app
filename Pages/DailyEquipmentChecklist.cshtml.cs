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
    private readonly ChecklistVarianceService _varianceService;
    private readonly ReadinessAlertService _readinessAlertService;

    public DailyEquipmentChecklistModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ChecklistVarianceService varianceService,
        ReadinessAlertService readinessAlertService)
    {
        _db = db;
        _currentUser = currentUser;
        _varianceService = varianceService;
        _readinessAlertService = readinessAlertService;
    }

    [BindProperty] public string? Callsign { get; set; }
    [BindProperty] public string? Registration { get; set; }
    [BindProperty] public bool SameAsPreviousEquipmentCheck { get; set; }
    [BindProperty] public int? PreviousEquipmentReportId { get; set; }
    [BindProperty] public string? EquipmentSectionNote { get; set; }
    [BindProperty] public List<EquipmentCheckInput> EquipmentChecks { get; set; } = new();

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public bool AllowSameAsPreviousEquipmentCheck { get; private set; } = true;
    public bool HasAssignedChecklist { get; private set; }
    public string AssignedChecklistName { get; private set; } = string.Empty;
    public string DraftStorageKey { get; private set; } = "daily-equipment-readiness:anonymous";
    public string FreshChecklistUrl => "/DailyEquipmentChecklist";

    public string LinkedVehicleLabel => string.IsNullOrWhiteSpace(Callsign) && string.IsNullOrWhiteSpace(Registration)
        ? "No vehicle selected"
        : $"{Callsign} {Registration}".Trim();
    public bool HasLinkedVehicleSelection => !string.IsNullOrWhiteSpace(Callsign) || !string.IsNullOrWhiteSpace(Registration);

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
        if (!HasLinkedVehicleSelection)
        {
            return RedirectToPage("/DailyVehicleChecklist");
        }

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

        if (!HasAssignedChecklist)
        {
            StatusMessage = "No assigned checklist available for this vehicle. Ask management to publish a checklist for this vehicle type, area, or callsign.";
            return Page();
        }

        if (await FindLinkedVehicleAsync(currentUser.CompanyId) is null)
        {
            StatusMessage = "This vehicle is not active in the vehicle register. Select a registered active vehicle before saving equipment checks.";
            return Page();
        }

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
            ((check.RequiresOperationalCheck && check.IsOperational == false) ||
             (check.RequiresBatteryCheck && string.Equals(check.BatteryState, "Low", StringComparison.OrdinalIgnoreCase))) &&
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
                row.PreviousNextServiceDate = postedRow.PreviousNextServiceDate;
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
        if (vehicle is null)
        {
            HasAssignedChecklist = false;
            AssignedChecklistName = string.Empty;
            return new List<EquipmentCheckInput>();
        }

        var checklistTemplate = await ResolvePublishedTemplateForVehicleAsync(companyId, vehicle);
        HasAssignedChecklist = checklistTemplate is not null;
        AssignedChecklistName = checklistTemplate?.Name ?? string.Empty;
        if (checklistTemplate is null)
        {
            return new List<EquipmentCheckInput>();
        }

        var previousVehicleChecks = await LoadPreviousVehicleEquipmentChecksAsync(companyId, vehicle.Id);
        var activeEquipment = await _db.EquipmentItems
            .AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.Status != "Deleted")
            .OrderBy(item => item.Name)
            .ThenBy(item => item.SerialOrAssetId)
            .ToListAsync();

        var isResponseVehicle = IsResponseVehicleType(vehicle.VehicleType);
        var selectableEquipment = isResponseVehicle
            ? activeEquipment.Where(IsMonitorDefibrillatorEquipmentItem).ToList()
            : activeEquipment;

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

        if (isResponseVehicle)
        {
            assignments = assignments
                .Where(IsMonitorDefibrillatorAssignment)
                .ToList();
        }

        return await BuildRowsFromTemplateAsync(
            checklistTemplate.Id,
            selectableEquipment,
            assignments,
            previousVehicleChecks,
            isResponseVehicle);
    }

    private async Task<List<EquipmentCheckInput>> BuildRowsFromTemplateAsync(
        int checklistTemplateId,
        IReadOnlyList<EquipmentItem> activeEquipment,
        IReadOnlyList<VehicleEquipmentAssignment> assignments,
        IReadOnlyList<DailyVehicleEquipmentCheck> previousVehicleChecks,
        bool restrictToMonitorDefibrillator)
    {
        var sections = await _db.ChecklistSections
            .AsNoTracking()
            .Include(section => section.Items)
                .ThenInclude(item => item.ColumnDefinitions)
            .Where(section => section.ChecklistTemplateId == checklistTemplateId)
            .OrderBy(section => section.DisplayOrder)
            .AsSplitQuery()
            .ToListAsync();

        var equipmentSection = sections.FirstOrDefault(section =>
            section.Name == "Carried Equipment" ||
            section.Items.Any(item => item.ItemKind == "EquipmentRow" || item.ItemKind == "EquipmentSubItem" || item.ResponseType == "EquipmentRow"));

        if (equipmentSection is null)
        {
            return new List<EquipmentCheckInput>();
        }

        var subItemsByParentId = equipmentSection.Items
            .Where(item => item.ParentChecklistItemId is not null)
            .GroupBy(item => item.ParentChecklistItemId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.DisplayOrder).ToList());

        var rows = new List<EquipmentCheckInput>();
        var parentRows = equipmentSection.Items
            .Where(item => item.ParentChecklistItemId is null && (item.ItemKind == "EquipmentRow" || item.ResponseType == "EquipmentRow"))
            .OrderBy(item => item.DisplayOrder)
            .ToList();

        if (restrictToMonitorDefibrillator)
        {
            parentRows = parentRows
                .Where(IsMonitorDefibrillatorTemplateItem)
                .ToList();
        }

        foreach (var parentRow in parentRows)
        {
            rows.Add(BuildRowFromTemplateItem(parentRow, null, activeEquipment, assignments, previousVehicleChecks, rows.Count + 1));

            if (!subItemsByParentId.TryGetValue(parentRow.Id, out var subItems))
            {
                continue;
            }

            foreach (var subItem in subItems)
            {
                rows.Add(BuildRowFromTemplateItem(subItem, parentRow.Prompt, activeEquipment, assignments, previousVehicleChecks, rows.Count + 1));
            }
        }

        return rows;
    }

    private EquipmentCheckInput BuildRowFromTemplateItem(
        ChecklistItem templateItem,
        string? parentItemName,
        IReadOnlyList<EquipmentItem> activeEquipment,
        IReadOnlyList<VehicleEquipmentAssignment> assignments,
        IReadOnlyList<DailyVehicleEquipmentCheck> previousVehicleChecks,
        int sortOrder)
    {
        var name = NormalizeOptional(templateItem.Prompt) ?? "Equipment item";
        var type = NormalizeOptional(templateItem.EquipmentType) ?? name;
        var model = NormalizeOptional(templateItem.Model);
        var matchingAssignment = assignments.FirstOrDefault(assignment =>
            ValuesMatch(assignment.ExpectedEquipmentName, name) ||
            ValuesMatch(assignment.ExpectedEquipmentType, type) ||
            ValuesMatch(assignment.ExpectedModel, model));
        var equipmentItem = matchingAssignment?.EquipmentItem ?? activeEquipment.FirstOrDefault(item =>
            ValuesMatch(item.Name, name) ||
            ValuesMatch(item.EquipmentType, type) ||
            ValuesMatch(item.Model, model));
        var requiresSerialOrAssetCheck = HasApplicableColumn(templateItem, "S/N", "Serial", "Asset");
        var requiresNextServiceCheck = HasApplicableColumn(templateItem, "Next Service", "Service");
        var requiresBatteryColumn = HasApplicableColumn(templateItem, "Battery");
        var requiresOperationalCheck = HasApplicableColumn(templateItem, "Operational");
        var requiresIssueNotesCheck = HasApplicableColumn(templateItem, "Issue", "Error", "Note");
        var requiresBatteryCheck = requiresBatteryColumn &&
            (equipmentItem?.BatteryRequired ?? true);

        var row = new EquipmentCheckInput
        {
            ChecklistItemId = templateItem.Id,
            ParentItemName = NormalizeOptional(parentItemName),
            VehicleEquipmentAssignmentId = matchingAssignment?.Id,
            EquipmentItemId = equipmentItem?.Id,
            Name = name,
            EquipmentType = type,
            Model = model ?? equipmentItem?.Model,
            SerialOrAssetId = equipmentItem?.SerialOrAssetId,
            NextServiceDate = requiresNextServiceCheck
                ? ResolveNextServiceDate(
                    equipmentItem,
                    previousVehicleChecks,
                    matchingAssignment?.Id,
                    equipmentItem?.Id,
                    equipmentItem?.SerialOrAssetId,
                    name,
                    templateItem.Id)
                : null,
            RequiresSerialOrAssetCheck = requiresSerialOrAssetCheck,
            RequiresNextServiceCheck = requiresNextServiceCheck,
            RequiresBatteryCheck = requiresBatteryCheck,
            RequiresOperationalCheck = requiresOperationalCheck,
            RequiresIssueNotesCheck = requiresIssueNotesCheck,
            BatteryState = requiresBatteryCheck ? "Full" : "Not applicable",
            IsOperational = requiresOperationalCheck ? true : null,
            PreviousBatteryState = requiresBatteryCheck ? "Full" : "Not applicable",
            PreviousIsOperational = requiresOperationalCheck ? true : null,
            SortOrder = sortOrder
        };

        row.SerialOptions = BuildSerialOptions(activeEquipment, previousVehicleChecks, name, type, row.Model, row.SerialOrAssetId);
        return row;
    }

    private async Task<List<DailyVehicleEquipmentCheck>> LoadPreviousVehicleEquipmentChecksAsync(int companyId, int vehicleId)
    {
        return await _db.DailyVehicleEquipmentChecks
            .AsNoTracking()
            .Include(check => check.DailyVehicleReadinessReport)
            .Where(check =>
                check.CompanyId == companyId &&
                check.NextServiceDateAtCheck != null &&
                check.DailyVehicleReadinessReport != null &&
                check.DailyVehicleReadinessReport.VehicleId == vehicleId &&
                check.DailyVehicleReadinessReport.WorkflowStatus != "Deleted")
            .OrderByDescending(check => check.DailyVehicleReadinessReport!.InspectionDateUtc)
            .ThenByDescending(check => check.CreatedAtUtc)
            .ToListAsync();
    }

    private EquipmentCheckInput BuildRowFromAssignment(
        VehicleEquipmentAssignment assignment,
        IReadOnlyList<EquipmentItem> activeEquipment,
        IReadOnlyList<DailyVehicleEquipmentCheck> previousVehicleChecks,
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
            NextServiceDate = ResolveNextServiceDate(
                equipmentItem,
                previousVehicleChecks,
                assignment.Id,
                equipmentItem?.Id,
                equipmentItem?.SerialOrAssetId,
                name),
            RequiresBatteryCheck = assignment.RequiresBatteryCheck || equipmentItem?.BatteryRequired == true,
            BatteryState = assignment.RequiresBatteryCheck || equipmentItem?.BatteryRequired == true ? "Full" : "Not applicable",
            IsOperational = true,
            PreviousBatteryState = "Full",
            PreviousIsOperational = true,
            SortOrder = sortOrder
        };

        row.SerialOptions = BuildSerialOptions(activeEquipment, previousVehicleChecks, name, type, model, row.SerialOrAssetId);
        return row;
    }

    private EquipmentCheckInput BuildRowFromEquipmentItem(
        EquipmentItem item,
        IReadOnlyList<EquipmentItem> activeEquipment,
        IReadOnlyList<DailyVehicleEquipmentCheck> previousVehicleChecks,
        int sortOrder)
    {
        var row = new EquipmentCheckInput
        {
            EquipmentItemId = item.Id,
            Name = item.Name,
            EquipmentType = item.EquipmentType,
            Model = item.Model,
            SerialOrAssetId = item.SerialOrAssetId,
            NextServiceDate = ResolveNextServiceDate(
                item,
                previousVehicleChecks,
                vehicleEquipmentAssignmentId: null,
                equipmentItemId: item.Id,
                serialOrAssetId: item.SerialOrAssetId,
                equipmentName: item.Name),
            RequiresBatteryCheck = item.BatteryRequired,
            BatteryState = item.BatteryRequired ? "Full" : "Not applicable",
            IsOperational = true,
            PreviousBatteryState = "Full",
            PreviousIsOperational = true,
            SortOrder = sortOrder
        };

        row.SerialOptions = BuildSerialOptions(activeEquipment, previousVehicleChecks, item.Name, item.EquipmentType, item.Model, row.SerialOrAssetId);
        return row;
    }

    private List<EquipmentSerialOption> BuildSerialOptions(
        IReadOnlyList<EquipmentItem> equipmentItems,
        IReadOnlyList<DailyVehicleEquipmentCheck> previousVehicleChecks,
        string? equipmentName,
        string? equipmentType,
        string? model,
        string? selectedSerial)
    {
        var selectableEquipment = equipmentItems
            .Where(item => !string.IsNullOrWhiteSpace(item.SerialOrAssetId))
            .OrderBy(item => item.Name)
            .ThenBy(item => item.SerialOrAssetId)
            .ToList();

        var expectedMatches = selectableEquipment
            .Where(item =>
                ValuesMatch(item.Name, equipmentName) ||
                ValuesMatch(item.EquipmentType, equipmentType) ||
                ValuesMatch(item.Model, model))
            .ToList();

        var options = new List<EquipmentSerialOption>();
        var addedSerials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in expectedMatches)
        {
            AddSerialOption(item, prefix: string.Empty);
        }

        foreach (var item in selectableEquipment)
        {
            AddSerialOption(item, prefix: expectedMatches.Count > 0 ? "Other registered: " : string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(selectedSerial) &&
            options.All(option => !string.Equals(option.Value, selectedSerial, StringComparison.OrdinalIgnoreCase)))
        {
            options.Insert(0, new EquipmentSerialOption(
                selectedSerial,
                selectedSerial,
                ResolveHistoricalNextServiceDate(previousVehicleChecks, null, null, selectedSerial, equipmentName),
                null));
        }

        return options;

        void AddSerialOption(EquipmentItem item, string prefix)
        {
            var serial = NormalizeOptional(item.SerialOrAssetId);
            if (serial is null || !addedSerials.Add(serial))
            {
                return;
            }

            options.Add(new EquipmentSerialOption(
                serial,
                $"{prefix}{serial} - {item.Name}",
                ResolveNextServiceDate(
                    item,
                    previousVehicleChecks,
                    vehicleEquipmentAssignmentId: null,
                    equipmentItemId: item.Id,
                    serialOrAssetId: item.SerialOrAssetId,
                    equipmentName: item.Name),
                item.Id));
        }
    }

    private static DateTime? ResolveNextServiceDate(
        EquipmentItem? equipmentItem,
        IReadOnlyList<DailyVehicleEquipmentCheck> previousVehicleChecks,
        int? vehicleEquipmentAssignmentId,
        int? equipmentItemId,
        string? serialOrAssetId,
        string? equipmentName,
        int? checklistItemId = null)
    {
        return equipmentItem?.NextServiceDate ??
            ResolveHistoricalNextServiceDate(
                previousVehicleChecks,
                vehicleEquipmentAssignmentId,
                equipmentItemId ?? equipmentItem?.Id,
                serialOrAssetId ?? equipmentItem?.SerialOrAssetId,
                equipmentName ?? equipmentItem?.Name,
                checklistItemId);
    }

    private static DateTime? ResolveHistoricalNextServiceDate(
        IReadOnlyList<DailyVehicleEquipmentCheck> previousVehicleChecks,
        int? vehicleEquipmentAssignmentId,
        int? equipmentItemId,
        string? serialOrAssetId,
        string? equipmentName,
        int? checklistItemId = null)
    {
        var normalizedSerial = NormalizeOptional(serialOrAssetId);
        var normalizedName = NormalizeOptional(equipmentName);

        return previousVehicleChecks.FirstOrDefault(check =>
            (checklistItemId is not null && check.ChecklistItemId == checklistItemId) ||
            (vehicleEquipmentAssignmentId is not null && check.VehicleEquipmentAssignmentId == vehicleEquipmentAssignmentId) ||
            (equipmentItemId is not null && check.EquipmentItemId == equipmentItemId) ||
            (normalizedSerial is not null && string.Equals(check.SerialOrAssetId, normalizedSerial, StringComparison.OrdinalIgnoreCase)) ||
            (normalizedName is not null && string.Equals(check.EquipmentName, normalizedName, StringComparison.OrdinalIgnoreCase)))
            ?.NextServiceDateAtCheck;
    }

    private async Task<int> SaveEquipmentReadinessAsync(AppUser currentUser, IReadOnlyList<EquipmentCheckInput> populatedChecks)
    {
        var now = DateTime.UtcNow;
        var vehicle = await FindLinkedVehicleAsync(currentUser.CompanyId)
            ?? throw new InvalidOperationException("Equipment checks require an active registered vehicle.");
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
                ChecklistItemId = row.ChecklistItemId,
                EquipmentName = NormalizeOptional(row.Name) ?? equipmentItem?.Name ?? "Equipment item",
                EquipmentType = NormalizeOptional(row.EquipmentType) ?? equipmentItem?.EquipmentType,
                Model = NormalizeOptional(row.Model) ?? equipmentItem?.Model,
                SerialOrAssetId = row.RequiresSerialOrAssetCheck
                    ? NormalizeOptional(row.SerialOrAssetId) ?? equipmentItem?.SerialOrAssetId
                    : null,
                NextServiceDateAtCheck = row.RequiresNextServiceCheck
                    ? row.NextServiceDate ?? equipmentItem?.NextServiceDate
                    : null,
                BatteryStatus = row.RequiresBatteryCheck
                    ? NormalizeOptional(row.BatteryState)
                    : "Not applicable",
                IsOperational = row.RequiresOperationalCheck ? row.IsOperational ?? true : true,
                IssueNotes = NormalizeOptional(row.IssueNotes),
                PresentStatus = "Present",
                ReadinessImpact =
                    (row.RequiresOperationalCheck && row.IsOperational == false) ||
                    (row.RequiresBatteryCheck && string.Equals(row.BatteryState, "Low", StringComparison.OrdinalIgnoreCase))
                        ? "Warning"
                        : "None",
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
        report.GeneralNotes = NormalizeOptional(EquipmentSectionNote);
        report.WarningIssueCount += savedChecks.Count(check => check.ReadinessImpact == "Warning");
        report.SubmittedAtUtc ??= now;

        await _db.SaveChangesAsync();

        var varianceCount = await _varianceService.CreateEquipmentVarianceAlertsAsync(report.Id, currentUser.Id);
        var readinessAlertCount = await _readinessAlertService.CreateAlertsForReportAsync(report.Id, currentUser.Id);

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Equipment readiness saved",
            EntityType = "DailyVehicleReadinessReport",
            EntityId = report.Id,
            Details = $"{currentUser.FullName} saved {savedChecks.Count} equipment readiness rows for {report.VehicleRegistrationNumber} from {CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView)} access.{(varianceCount > 0 ? $" {varianceCount} manager review alert(s) created for changed equipment values." : string.Empty)}{(readinessAlertCount > 0 ? $" {readinessAlertCount} readiness alert(s) created from scoring rules." : string.Empty)}{(string.IsNullOrWhiteSpace(EquipmentSectionNote) ? string.Empty : " General equipment note added.")}",
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
                report.PerformedByUserId != currentUserId &&
                report.WorkflowStatus != "Deleted")
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
                    (row.ChecklistItemId is not null && check.ChecklistItemId == row.ChecklistItemId) ||
                    (row.VehicleEquipmentAssignmentId is not null && check.VehicleEquipmentAssignmentId == row.VehicleEquipmentAssignmentId) ||
                    (row.EquipmentItemId is not null && check.EquipmentItemId == row.EquipmentItemId) ||
                    (!string.IsNullOrWhiteSpace(row.SerialOrAssetId) && check.SerialOrAssetId == row.SerialOrAssetId) ||
                    (!string.IsNullOrWhiteSpace(row.Name) && check.EquipmentName == row.Name));

            if (previousCheck is null)
            {
                continue;
            }

            row.PreviousSerialOrAssetId = previousCheck.SerialOrAssetId;
            row.PreviousNextServiceDate = previousCheck.NextServiceDateAtCheck;
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
                vehicle.Status != "Deleted" &&
                ((registration != null && vehicle.RegistrationNumber == registration) ||
                 (callsign != null && vehicle.Callsign == callsign)));
    }

    private async Task<DailyVehicleReadinessReport> EnsureReadinessReportAsync(AppUser currentUser, Vehicle vehicle, DateTime now)
    {
        var checklistTemplate = await ResolvePublishedTemplateForVehicleAsync(currentUser.CompanyId, vehicle)
            ?? throw new InvalidOperationException("Equipment checks require an active published checklist template.");
        var recentCutoff = now.AddHours(-12);
        var report = await _db.DailyVehicleReadinessReports
            .Where(item =>
                item.CompanyId == currentUser.CompanyId &&
                item.VehicleId == vehicle.Id &&
                item.PerformedByUserId == currentUser.Id &&
                item.WorkflowStatus != "Deleted" &&
                item.ChecklistTemplateId == checklistTemplate.Id &&
                item.InspectionDateUtc >= recentCutoff)
            .OrderByDescending(item => item.InspectionDateUtc)
            .FirstOrDefaultAsync();

        if (report is not null)
        {
            report.ChecklistTemplateVersion = checklistTemplate.Version;
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
            ChecklistTemplateId = checklistTemplate.Id,
            ChecklistTemplateVersion = checklistTemplate.Version,
            VehicleNextServiceDateAtCheck = vehicle.NextServiceDate,
            ReadinessStatus = "Pending",
            CreatedAtUtc = now,
            SubmittedAtUtc = now
        };

        _db.DailyVehicleReadinessReports.Add(report);
        await _db.SaveChangesAsync();
        return report;
    }

    private async Task<ChecklistTemplate?> ResolvePublishedTemplateForVehicleAsync(int companyId, Vehicle vehicle)
    {
        var templates = await _db.ChecklistTemplates
            .AsNoTracking()
            .Include(item => item.PublishScopes)
            .Where(item =>
                item.CompanyId == companyId &&
                item.ChecklistType == "Vehicle" &&
                item.Status == "Published" &&
                item.IsPublished &&
                item.PublishScopes.Any(scope => scope.IsActive && scope.RetiredAtUtc == null))
            .ToListAsync();

        return SelectPublishedTemplateForVehicle(templates, vehicle);
    }

    private static ChecklistTemplate? SelectPublishedTemplateForVehicle(
        IReadOnlyList<ChecklistTemplate> templates,
        Vehicle vehicle)
    {
        if (templates.Count == 0)
        {
            return null;
        }

        var scopedTemplate = templates
            .Where(IsLivePublishedTemplate)
            .Select(template => new
            {
                Template = template,
                Rank = template.PublishScopes
                    .Where(scope => scope.IsActive && scope.RetiredAtUtc == null)
                    .Where(scope => ScopeAppliesToVehicle(scope, template, vehicle))
                    .Select(scope => ScopeRank(scope.ScopeType))
                    .DefaultIfEmpty(0)
                    .Max()
            })
            .Where(item => item.Rank > 0)
            .OrderByDescending(item => item.Rank)
            .ThenByDescending(item => item.Template.PublishedAtUtc)
            .Select(item => item.Template)
            .FirstOrDefault();

        return scopedTemplate;
    }

    private static bool IsLivePublishedTemplate(ChecklistTemplate template)
    {
        return template.IsPublished &&
            string.Equals(template.Status, "Published", StringComparison.OrdinalIgnoreCase) &&
            template.PublishScopes.Any(scope => scope.IsActive && scope.RetiredAtUtc is null);
    }

    private static bool ScopeAppliesToVehicle(ChecklistPublishScope scope, ChecklistTemplate template, Vehicle vehicle)
    {
        if (!VehicleMatchesTemplateTarget(vehicle, template.TargetVehicleType))
        {
            return false;
        }

        return scope.ScopeType switch
        {
            "Vehicle" => scope.VehicleId == vehicle.Id,
            "OperationalArea" => scope.OperationalAreaId == vehicle.CurrentOperationalAreaId,
            "VehicleCategory" => true,
            "AllAreas" => true,
            _ => false
        };
    }

    private static int ScopeRank(string scopeType) => scopeType switch
    {
        "Vehicle" => 4,
        "OperationalArea" => 3,
        "VehicleCategory" => 2,
        "AllAreas" => 1,
        _ => 0
    };

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

            row.NextServiceDate ??= row.PreviousNextServiceDate;
            if (!row.RequiresNextServiceCheck)
            {
                row.NextServiceDate = null;
            }

            row.BatteryState = row.RequiresBatteryCheck ? row.PreviousBatteryState ?? "Full" : "Not applicable";
            row.IsOperational = row.RequiresOperationalCheck ? row.PreviousIsOperational : null;
            row.IssueNotes = row.RequiresIssueNotesCheck ? row.PreviousIssueNotes : null;
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

    private static bool VehicleMatchesTemplateTarget(Vehicle vehicle, string targetVehicleType)
    {
        if (string.Equals(targetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var function = NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        var subtype = NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);

        return string.Equals(function, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(subtype, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vehicle.VehicleType, targetVehicleType, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsResponseVehicleType(string? vehicleType)
    {
        return IsPickupRvType(vehicleType);
    }

    private static bool IsPickupRvType(string? vehicleType)
    {
        return !string.IsNullOrWhiteSpace(vehicleType) &&
            (vehicleType.Contains("Pickup", StringComparison.OrdinalIgnoreCase) ||
             vehicleType.Contains("RV", StringComparison.OrdinalIgnoreCase) ||
             vehicleType.Contains("Response", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasApplicableColumn(ChecklistItem item, params string[] terms)
    {
        return item.ColumnDefinitions.Any(column =>
            !IsNotApplicableColumn(column) &&
            terms.Any(term =>
                column.Heading.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                column.FieldKey.Contains(term.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase) ||
                column.FieldKey.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsNotApplicableColumn(ChecklistColumnDefinition column)
    {
        return string.Equals(column.ResponseType, "N/A", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(column.RegisterSource, "Not applicable for this row", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMonitorDefibrillatorAssignment(VehicleEquipmentAssignment assignment)
    {
        return IsMonitorDefibrillatorText(assignment.ExpectedEquipmentName) ||
            IsMonitorDefibrillatorText(assignment.ExpectedEquipmentType) ||
            IsMonitorDefibrillatorText(assignment.ExpectedModel) ||
            IsMonitorDefibrillatorEquipmentItem(assignment.EquipmentItem);
    }

    private static bool IsMonitorDefibrillatorTemplateItem(ChecklistItem templateItem)
    {
        return IsMonitorDefibrillatorText(templateItem.Prompt) ||
            IsMonitorDefibrillatorText(templateItem.EquipmentType) ||
            IsMonitorDefibrillatorText(templateItem.Model);
    }

    private static bool IsMonitorDefibrillatorEquipmentItem(EquipmentItem? equipmentItem)
    {
        return equipmentItem is not null &&
            (IsMonitorDefibrillatorText(equipmentItem.Name) ||
             IsMonitorDefibrillatorText(equipmentItem.EquipmentType) ||
             IsMonitorDefibrillatorText(equipmentItem.Model));
    }

    private static bool IsMonitorDefibrillatorText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal)
            .Trim();

        return (normalized.Contains("Monitor", StringComparison.OrdinalIgnoreCase) &&
                normalized.Contains("Defibrillator", StringComparison.OrdinalIgnoreCase)) ||
            normalized.Contains("LIFEPAK", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("LP15", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Zoll", StringComparison.OrdinalIgnoreCase);
    }
}

public class EquipmentCheckInput
{
    public int? ChecklistItemId { get; set; }
    public int? VehicleEquipmentAssignmentId { get; set; }
    public int? EquipmentItemId { get; set; }
    public string? ParentItemName { get; set; }
    public string? Name { get; set; }
    public string? EquipmentType { get; set; }
    public string? Model { get; set; }
    public string? SerialOrAssetId { get; set; }
    public DateTime? NextServiceDate { get; set; }
    public bool RequiresSerialOrAssetCheck { get; set; } = true;
    public bool RequiresNextServiceCheck { get; set; } = true;
    public bool RequiresBatteryCheck { get; set; }
    public bool RequiresOperationalCheck { get; set; } = true;
    public bool RequiresIssueNotesCheck { get; set; } = true;
    public string? BatteryState { get; set; }
    public bool? IsOperational { get; set; } = true;
    public string? IssueNotes { get; set; }
    public string? PreviousSerialOrAssetId { get; set; }
    public DateTime? PreviousNextServiceDate { get; set; }
    public string? PreviousBatteryState { get; set; }
    public bool? PreviousIsOperational { get; set; } = true;
    public string? PreviousIssueNotes { get; set; }
    public int? PreviousEquipmentCheckId { get; set; }
    public int SortOrder { get; set; }
    public List<EquipmentSerialOption> SerialOptions { get; set; } = new();

    public string RowKey => ChecklistItemId is not null
        ? $"checklist:{ChecklistItemId}"
        : VehicleEquipmentAssignmentId is not null
            ? $"assignment:{VehicleEquipmentAssignmentId}"
            : EquipmentItemId is not null
                ? $"equipment:{EquipmentItemId}"
                : $"manual:{Name}";
}

public record EquipmentSerialOption(string Value, string Text, DateTime? NextServiceDate, int? EquipmentItemId);
