using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ChecklistReportsModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ChecklistReportPdfService _pdfService;

    public ChecklistReportsModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ChecklistReportPdfService pdfService)
    {
        _db = db;
        _currentUser = currentUser;
        _pdfService = pdfService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchField { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchValue { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? WorkflowStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReadinessStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? VehicleType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? OperationalAreaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? SelectedReportDate { get; set; }

    public bool IsSeniorOverview { get; private set; }
    public bool HasManagerScope { get; private set; } = true;
    public string ScopeLabel { get; private set; } = "Assigned area";
    public string? StatusMessage { get; private set; }
    public DateTime FromUtc { get; private set; }
    public DateTime ToUtcExclusive { get; private set; }
    public List<SelectListItem> AreaOptions { get; private set; } = [];
    public List<SelectListItem> WorkflowOptions { get; private set; } = [];
    public List<SelectListItem> ReadinessOptions { get; private set; } = [];
    public List<SelectListItem> VehicleTypeOptions { get; private set; } = [];
    public List<SelectListItem> SearchFieldOptions { get; private set; } = [];
    public List<SelectListItem> SearchValueOptions { get; private set; } = [];
    public List<ChecklistReportRow> ReportRows { get; private set; } = [];
    public List<ChecklistReportRow> SelectedReportRows { get; private set; } = [];
    public List<ChecklistReportAreaGroup> SelectedReportAreaGroups { get; private set; } = [];
    public List<ChecklistReportDateGroup> ReportDateGroups { get; private set; } = [];
    public ChecklistReportSummary Summary { get; private set; } = new();
    public string? SelectedReportDateLabel => SelectedReportDate?.Date.ToString("yyyy-MM-dd");

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CurrentUserService.CanSendTasks(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        IsSeniorOverview = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        SetDateRange();
        LoadStaticOptions();

        var scope = await LoadScopeAsync(currentUser);
        await LoadVehicleTypeOptionsAsync(currentUser.CompanyId, scope.AreaIds);

        if (!HasManagerScope)
        {
            return Page();
        }

        await LoadReportsAsync(currentUser.CompanyId, scope.AreaIds);
        BuildSearchValueOptions();
        ApplySearchFilter();
        BuildReportGroups();
        BuildSelectedReportRows();
        BuildSummary();

        return Page();
    }

    public async Task<IActionResult> OnGetPdfAsync(int id)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CurrentUserService.CanSendTasks(currentUser.AppRole?.Name))
        {
            return NotFound();
        }

        var report = await LoadFullReportAsync(currentUser.CompanyId, id);
        if (report is null)
        {
            return NotFound();
        }

        if (!await CanAccessReportAsync(currentUser, report))
        {
            return NotFound();
        }

        var pdfBytes = _pdfService.BuildDailyReadinessPdf(report);
        var vehicleLabel = SafeFilePart(report.VehicleRegistrationNumber);
        var dateLabel = (report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc).ToLocalTime().ToString("yyyyMMdd-HHmm");
        return File(pdfBytes, "application/pdf", $"checklist-{vehicleLabel}-{report.Id}-{dateLabel}.pdf");
    }

    private void SetDateRange()
    {
        var today = DateTime.Today;
        var from = FromDate?.Date ?? today.AddDays(-30);
        var to = ToDate?.Date ?? today;

        if (to < from)
        {
            (from, to) = (to, from);
        }

        FromDate = from;
        ToDate = to;
        FromUtc = from;
        ToUtcExclusive = to.AddDays(1);
    }

    private void LoadStaticOptions()
    {
        WorkflowOptions =
        [
            new SelectListItem("All evidence statuses", string.Empty),
            new SelectListItem("Saved / submitted evidence", "Saved"),
            new SelectListItem("Draft evidence only", "Draft")
        ];

        ReadinessOptions =
        [
            new SelectListItem("All readiness statuses", string.Empty),
            new SelectListItem("Operational", "Operational"),
            new SelectListItem("Ready", "Ready"),
            new SelectListItem("Attention", "Attention"),
            new SelectListItem("Not ready", "Not ready"),
            new SelectListItem("Pending", "Pending")
        ];

        SearchFieldOptions =
        [
            new SelectListItem("Filter by...", string.Empty),
            new SelectListItem("Callsign", "callsign"),
            new SelectListItem("Registration", "registration"),
            new SelectListItem("Area / base", "area"),
            new SelectListItem("Submitted by", "person"),
            new SelectListItem("Checklist template", "template")
        ];
    }

    private async Task<ReportScope> LoadScopeAsync(AppUser currentUser)
    {
        if (IsSeniorOverview)
        {
            AreaOptions.Add(new SelectListItem("All areas / bases", string.Empty));

            var areas = await _db.OperationalAreas
                .AsNoTracking()
                .Where(area => area.CompanyId == currentUser.CompanyId && area.Status == "Active")
                .OrderBy(area => area.AreaType)
                .ThenBy(area => area.Name)
                .Select(area => new AreaScopeItem(area.Id, area.Name, area.AreaType))
                .ToListAsync();

            AreaOptions.AddRange(areas.Select(area =>
                new SelectListItem($"{area.Name} ({area.AreaType})", area.Id.ToString())));

            if (OperationalAreaId.HasValue && areas.All(area => area.Id != OperationalAreaId.Value))
            {
                OperationalAreaId = null;
            }

            ScopeLabel = OperationalAreaId.HasValue
                ? areas.First(area => area.Id == OperationalAreaId.Value).Name
                : "All areas / bases";

            var areaIds = OperationalAreaId.HasValue ? [OperationalAreaId.Value] : areas.Select(area => area.Id).ToList();
            return new ReportScope(areaIds);
        }

        var assignedAreas = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Include(assignment => assignment.OperationalArea)
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active" &&
                assignment.OperationalArea != null &&
                assignment.OperationalArea.Status == "Active")
            .OrderBy(assignment => assignment.OperationalArea!.AreaType)
            .ThenBy(assignment => assignment.OperationalArea!.Name)
            .Select(assignment => new AreaScopeItem(
                assignment.OperationalAreaId,
                assignment.OperationalArea!.Name,
                assignment.OperationalArea.AreaType))
            .ToListAsync();

        if (assignedAreas.Count == 0)
        {
            HasManagerScope = false;
            ScopeLabel = "No assigned area";
            StatusMessage = "This operational manager has no assigned base or region yet. Senior management can assign areas in Master Setup.";
            return new ReportScope([]);
        }

        OperationalAreaId = null;
        ScopeLabel = string.Join(", ", assignedAreas.Select(area => area.Name));
        return new ReportScope(assignedAreas.Select(area => area.Id).ToList());
    }

    private async Task LoadVehicleTypeOptionsAsync(int companyId, IReadOnlyList<int> areaIds)
    {
        var query = _db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.CompanyId == companyId && vehicle.Status != "Deleted");

        if (IsSeniorOverview && OperationalAreaId.HasValue)
        {
            query = query.Where(vehicle => vehicle.CurrentOperationalAreaId == OperationalAreaId.Value);
        }
        else if (!IsSeniorOverview)
        {
            query = query.Where(vehicle =>
                vehicle.CurrentOperationalAreaId.HasValue &&
                areaIds.Contains(vehicle.CurrentOperationalAreaId.Value));
        }

        var vehicleRows = await query
            .Select(vehicle => new
            {
                vehicle.VehicleFunction,
                vehicle.VehicleSubtype,
                vehicle.VehicleType
            })
            .ToListAsync();

        var vehicleTypes = vehicleRows
            .Select(vehicle =>
                NormalizeOptional(vehicle.VehicleSubtype) ??
                NormalizeOptional(vehicle.VehicleFunction) ??
                NormalizeOptional(vehicle.VehicleType))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        VehicleTypeOptions = [new SelectListItem("All vehicle types", string.Empty)];
        VehicleTypeOptions.AddRange(vehicleTypes.Select(type => new SelectListItem(type, type)));
    }

    private async Task LoadReportsAsync(int companyId, IReadOnlyList<int> areaIds)
    {
        var query = _db.DailyVehicleReadinessReports
            .AsNoTracking()
            .Include(report => report.Vehicle)
                .ThenInclude(vehicle => vehicle!.CurrentOperationalArea)
            .Include(report => report.PerformedByUser)
                .ThenInclude(user => user!.AppRole)
            .Include(report => report.ChecklistTemplate)
            .Include(report => report.EquipmentChecks)
            .Where(report =>
                report.CompanyId == companyId &&
                report.WorkflowStatus != "Deleted" &&
                (report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc) >= FromUtc &&
                (report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc) < ToUtcExclusive);

        if (IsSeniorOverview && OperationalAreaId.HasValue)
        {
            query = query.Where(report =>
                report.Vehicle != null &&
                report.Vehicle.CurrentOperationalAreaId == OperationalAreaId.Value);
        }
        else if (!IsSeniorOverview)
        {
            query = query.Where(report =>
                report.Vehicle != null &&
                report.Vehicle.CurrentOperationalAreaId.HasValue &&
                areaIds.Contains(report.Vehicle.CurrentOperationalAreaId.Value));
        }

        if (string.Equals(WorkflowStatus, "Saved", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(report => report.WorkflowStatus == "Saved" || report.WorkflowStatus == "Submitted");
        }
        else if (!string.IsNullOrWhiteSpace(WorkflowStatus))
        {
            query = query.Where(report => report.WorkflowStatus == WorkflowStatus);
        }

        if (!string.IsNullOrWhiteSpace(ReadinessStatus))
        {
            query = query.Where(report => report.ReadinessStatus == ReadinessStatus);
        }

        if (!string.IsNullOrWhiteSpace(VehicleType))
        {
            var selectedVehicleType = VehicleType.Trim();
            query = query.Where(report =>
                report.VehicleTypeAtCheck == selectedVehicleType ||
                (report.Vehicle != null &&
                    (report.Vehicle.VehicleType == selectedVehicleType ||
                     report.Vehicle.VehicleFunction == selectedVehicleType ||
                     report.Vehicle.VehicleSubtype == selectedVehicleType)));
        }

        var reports = await query
            .OrderByDescending(report => report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc)
            .Take(500)
            .ToListAsync();

        ReportRows = reports
            .Select(BuildReportRow)
            .ToList();
    }

    private void ApplySearchFilter()
    {
        if (!string.IsNullOrWhiteSpace(SearchField) && !string.IsNullOrWhiteSpace(SearchValue))
        {
            var value = SearchValue.Trim();
            ReportRows = ReportRows
                .Where(row => SearchField switch
                {
                    "callsign" => string.Equals(row.Callsign, value, StringComparison.OrdinalIgnoreCase),
                    "registration" => string.Equals(row.RegistrationNumber, value, StringComparison.OrdinalIgnoreCase),
                    "area" => string.Equals(row.AreaName, value, StringComparison.OrdinalIgnoreCase),
                    "person" => string.Equals(row.PerformedByName, value, StringComparison.OrdinalIgnoreCase),
                    "template" => string.Equals(row.TemplateName, value, StringComparison.OrdinalIgnoreCase),
                    _ => true
                })
                .ToList();
        }

        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            return;
        }

        var search = SearchTerm.Trim();
        ReportRows = ReportRows
            .Where(row => ContainsAny(
                search,
                row.AreaName,
                row.VehicleLabel,
                row.RegistrationNumber,
                row.Callsign,
                row.VehicleType,
                row.PerformedByName,
                row.PerformedByRole,
                row.WorkflowStatus,
                row.ReadinessStatus,
                row.TemplateName,
                row.TemplateVersion,
                row.RecordedAtUtc.ToLocalTime().ToString("yyyy-MM-dd"),
                row.RecordedAtUtc.ToLocalTime().ToString("HH:mm")))
            .ToList();
    }

    private void BuildSearchValueOptions()
    {
        SearchValueOptions = [new SelectListItem("Select value...", string.Empty)];

        if (string.IsNullOrWhiteSpace(SearchField))
        {
            return;
        }

        var values = SearchField switch
        {
            "callsign" => ReportRows.Select(row => row.Callsign),
            "registration" => ReportRows.Select(row => row.RegistrationNumber),
            "area" => ReportRows.Select(row => row.AreaName),
            "person" => ReportRows.Select(row => row.PerformedByName),
            "template" => ReportRows.Select(row => row.TemplateName),
            _ => []
        };

        SearchValueOptions.AddRange(values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .Select(value => new SelectListItem(value, value)));
    }

    private void BuildReportGroups()
    {
        ReportDateGroups = ReportRows
            .GroupBy(row => row.RecordedAtUtc.ToLocalTime().Date)
            .OrderByDescending(group => group.Key)
            .Select(dateGroup => new ChecklistReportDateGroup
            {
                Date = dateGroup.Key,
                DateLabel = dateGroup.Key.ToString("yyyy-MM-dd"),
                TotalReports = dateGroup.Count(),
                AreaCount = dateGroup
                    .Select(row => string.IsNullOrWhiteSpace(row.AreaName) ? "Unallocated" : row.AreaName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                CallsignCount = dateGroup
                    .Select(row => string.IsNullOrWhiteSpace(row.Callsign) ? "No callsign" : row.Callsign)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(),
                SubmittedChecks = dateGroup.Count(row => IsSavedOrSubmitted(row.WorkflowStatus)),
                DraftChecks = dateGroup.Count(row => IsDraft(row.WorkflowStatus)),
                CriticalIssues = dateGroup.Sum(row => row.CriticalIssues),
                WarningIssues = dateGroup.Sum(row => row.WarningIssues),
                EquipmentIssues = dateGroup.Sum(row => row.EquipmentIssueCount),
                AreaGroups = dateGroup
                    .GroupBy(row => string.IsNullOrWhiteSpace(row.AreaName) ? "Unallocated" : row.AreaName)
                    .OrderBy(group => group.Key)
                    .Select(areaGroup => new ChecklistReportAreaGroup
                    {
                        AreaName = areaGroup.Key,
                        TotalReports = areaGroup.Count(),
                        CallsignGroups = areaGroup
                            .GroupBy(row => string.IsNullOrWhiteSpace(row.Callsign) ? "No callsign" : row.Callsign)
                            .OrderBy(group => group.Key)
                            .Select(callsignGroup => new ChecklistReportCallsignGroup
                            {
                                Callsign = callsignGroup.Key,
                                TotalReports = callsignGroup.Count(),
                                Rows = callsignGroup
                                    .OrderByDescending(row => row.RecordedAtUtc)
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();
    }

    private void BuildSelectedReportRows()
    {
        if (!SelectedReportDate.HasValue)
        {
            SelectedReportRows = [];
            SelectedReportAreaGroups = [];
            return;
        }

        var selectedDate = SelectedReportDate.Value.Date;
        SelectedReportRows = ReportRows
            .Where(row => row.RecordedAtUtc.ToLocalTime().Date == selectedDate)
            .OrderBy(row => row.AreaName)
            .ThenBy(row => row.Callsign)
            .ThenByDescending(row => row.RecordedAtUtc)
            .ToList();

        SelectedReportAreaGroups = SelectedReportRows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.AreaName) ? "Unallocated" : row.AreaName)
            .OrderBy(group => group.Key)
            .Select(areaGroup => new ChecklistReportAreaGroup
            {
                AreaName = areaGroup.Key,
                TotalReports = areaGroup.Count(),
                CallsignGroups = areaGroup
                    .GroupBy(row => string.IsNullOrWhiteSpace(row.Callsign) ? "No callsign" : row.Callsign)
                    .OrderBy(group => group.Key)
                    .Select(callsignGroup => new ChecklistReportCallsignGroup
                    {
                        Callsign = callsignGroup.Key,
                        TotalReports = callsignGroup.Count(),
                        Rows = callsignGroup
                            .OrderByDescending(row => row.RecordedAtUtc)
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();
    }

    private void BuildSummary()
    {
        Summary = new ChecklistReportSummary
        {
            TotalChecks = ReportRows.Count,
            SubmittedChecks = ReportRows.Count(row => IsSavedOrSubmitted(row.WorkflowStatus)),
            DraftChecks = ReportRows.Count(row => IsDraft(row.WorkflowStatus)),
            CriticalIssues = ReportRows.Sum(row => row.CriticalIssues),
            WarningIssues = ReportRows.Sum(row => row.WarningIssues),
            EquipmentIssues = ReportRows.Sum(row => row.EquipmentIssueCount),
            SameAsPreviousVehicleUses = ReportRows.Count(row => row.VehicleSameAsPreviousShiftUsed),
            SameAsPreviousEquipmentUses = ReportRows.Count(row => row.EquipmentSameAsPreviousShiftUsed)
        };
    }

    private static ChecklistReportRow BuildReportRow(DailyVehicleReadinessReport report)
    {
        var recordedAt = report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc;
        var vehicle = report.Vehicle;
        var areaName = vehicle?.CurrentOperationalArea?.Name ?? "Unallocated";
        var registration = string.IsNullOrWhiteSpace(report.VehicleRegistrationNumber)
            ? vehicle?.RegistrationNumber ?? "Unknown registration"
            : report.VehicleRegistrationNumber;
        var callsign = string.IsNullOrWhiteSpace(report.CallsignAtCheck)
            ? vehicle?.Callsign ?? "No callsign"
            : report.CallsignAtCheck;
        var vehicleType = vehicle is null
            ? string.IsNullOrWhiteSpace(report.VehicleTypeAtCheck) ? "Not set" : report.VehicleTypeAtCheck
            : VehicleTaxonomyService.DisplayClassification(vehicle);

        return new ChecklistReportRow
        {
            Id = report.Id,
            AreaName = areaName,
            RegistrationNumber = registration,
            Callsign = callsign,
            VehicleLabel = $"{registration} / {callsign}",
            VehicleType = vehicleType,
            PerformedByName = report.PerformedByUser?.FullName ?? "Unknown",
            PerformedByRole = report.PerformedByUser?.AppRole?.Name ?? "Unknown",
            WorkflowStatus = report.WorkflowStatus,
            EvidenceStatus = GetEvidenceStatusLabel(report.WorkflowStatus),
            ReadinessStatus = report.ReadinessStatus,
            ReadinessStatusClass = GetReadinessStatusClass(report.ReadinessStatus),
            CriticalIssues = report.CriticalIssueCount,
            WarningIssues = report.WarningIssueCount,
            EquipmentRowCount = report.EquipmentChecks.Count,
            EquipmentIssueCount = report.EquipmentChecks.Count(check =>
                !check.IsOperational ||
                !string.Equals(check.ReadinessImpact, "None", StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrWhiteSpace(check.IssueNotes)),
            RecordedAtUtc = recordedAt,
            TemplateName = GetTemplateDisplayName(report),
            TemplateVersion = report.ChecklistTemplateVersion ?? report.ChecklistTemplate?.Version ?? "N/A",
            VehicleSameAsPreviousShiftUsed = report.VehicleSameAsPreviousShiftUsed || report.SameAsPreviousShiftUsed,
            EquipmentSameAsPreviousShiftUsed = report.EquipmentSameAsPreviousShiftUsed
        };
    }

    private static string GetTemplateDisplayName(DailyVehicleReadinessReport report)
    {
        if (report.ChecklistTemplate is not null)
        {
            return ChecklistDisplayService.TemplateName(report.ChecklistTemplate.Name);
        }

        return report.ChecklistTemplateId.HasValue
            ? "Historical snapshot - template unavailable"
            : "Historical snapshot - no template link";
    }

    private static string GetEvidenceStatusLabel(string? workflowStatus)
    {
        if (IsSavedOrSubmitted(workflowStatus))
        {
            return "Saved / submitted evidence";
        }

        if (IsDraft(workflowStatus))
        {
            return "Draft evidence";
        }

        return string.IsNullOrWhiteSpace(workflowStatus) ? "Not set" : workflowStatus;
    }

    private static string GetReadinessStatusClass(string? readinessStatus)
    {
        if (string.Equals(readinessStatus, "Operational", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(readinessStatus, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            return "status-ok";
        }

        if (string.Equals(readinessStatus, "Not ready", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(readinessStatus, "Critical", StringComparison.OrdinalIgnoreCase))
        {
            return "status-danger";
        }

        return "status-warning";
    }

    private async Task<DailyVehicleReadinessReport?> LoadFullReportAsync(int companyId, int reportId)
    {
        return await _db.DailyVehicleReadinessReports
            .AsNoTracking()
            .AsSplitQuery()
            .Include(report => report.Company)
            .Include(report => report.Vehicle)
                .ThenInclude(vehicle => vehicle!.CurrentOperationalArea)
            .Include(report => report.PerformedByUser)
                .ThenInclude(user => user!.AppRole)
            .Include(report => report.PerformedByUser)
                .ThenInclude(user => user!.AssignedOperationalArea)
            .Include(report => report.ChecklistTemplate)
            .Include(report => report.EquipmentChecks)
            .FirstOrDefaultAsync(report =>
                report.CompanyId == companyId &&
                report.Id == reportId &&
                report.WorkflowStatus != "Deleted");
    }

    private async Task<bool> CanAccessReportAsync(AppUser currentUser, DailyVehicleReadinessReport report)
    {
        if (CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return true;
        }

        if (!string.Equals(currentUser.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var vehicleAreaId = report.Vehicle?.CurrentOperationalAreaId;
        if (!vehicleAreaId.HasValue)
        {
            return false;
        }

        return await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .AnyAsync(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.OperationalAreaId == vehicleAreaId.Value &&
                assignment.Status == "Active");
    }

    private static bool ContainsAny(string search, params string?[] values)
    {
        return values.Any(value => !string.IsNullOrWhiteSpace(value) && value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSavedOrSubmitted(string? workflowStatus)
    {
        return string.Equals(workflowStatus, "Saved", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(workflowStatus, "Submitted", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDraft(string? workflowStatus)
    {
        return string.Equals(workflowStatus, "Draft", StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeFilePart(string value)
    {
        var clean = new string(value
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_')
            .ToArray());

        return string.IsNullOrWhiteSpace(clean) ? "vehicle" : clean;
    }

    public string BuildDateSelectionUrl(DateTime date)
    {
        var parameters = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parameters.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            }
        }

        Add(nameof(FromDate), FromDate?.ToString("yyyy-MM-dd"));
        Add(nameof(ToDate), ToDate?.ToString("yyyy-MM-dd"));
        Add(nameof(SearchField), SearchField);
        Add(nameof(SearchValue), SearchValue);
        Add(nameof(WorkflowStatus), WorkflowStatus);
        Add(nameof(ReadinessStatus), ReadinessStatus);
        Add(nameof(VehicleType), VehicleType);
        Add(nameof(OperationalAreaId), OperationalAreaId?.ToString());
        Add(nameof(SelectedReportDate), date.Date.ToString("yyyy-MM-dd"));

        return parameters.Count == 0 ? "/ChecklistReports" : $"/ChecklistReports?{string.Join("&", parameters)}";
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record AreaScopeItem(int Id, string Name, string AreaType);
    private sealed record ReportScope(IReadOnlyList<int> AreaIds);

    public sealed class ChecklistReportSummary
    {
        public int TotalChecks { get; set; }
        public int SubmittedChecks { get; set; }
        public int DraftChecks { get; set; }
        public int CriticalIssues { get; set; }
        public int WarningIssues { get; set; }
        public int EquipmentIssues { get; set; }
        public int SameAsPreviousVehicleUses { get; set; }
        public int SameAsPreviousEquipmentUses { get; set; }
    }

    public sealed class ChecklistReportDateGroup
    {
        public DateTime Date { get; set; }
        public string DateLabel { get; set; } = string.Empty;
        public int TotalReports { get; set; }
        public int AreaCount { get; set; }
        public int CallsignCount { get; set; }
        public int SubmittedChecks { get; set; }
        public int DraftChecks { get; set; }
        public int CriticalIssues { get; set; }
        public int WarningIssues { get; set; }
        public int EquipmentIssues { get; set; }
        public List<ChecklistReportAreaGroup> AreaGroups { get; set; } = [];
    }

    public sealed class ChecklistReportAreaGroup
    {
        public string AreaName { get; set; } = string.Empty;
        public int TotalReports { get; set; }
        public List<ChecklistReportCallsignGroup> CallsignGroups { get; set; } = [];
    }

    public sealed class ChecklistReportCallsignGroup
    {
        public string Callsign { get; set; } = string.Empty;
        public int TotalReports { get; set; }
        public List<ChecklistReportRow> Rows { get; set; } = [];
    }

    public sealed class ChecklistReportRow
    {
        public int Id { get; set; }
        public string AreaName { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string VehicleLabel { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
        public string PerformedByRole { get; set; } = string.Empty;
        public string WorkflowStatus { get; set; } = string.Empty;
        public string EvidenceStatus { get; set; } = string.Empty;
        public string ReadinessStatus { get; set; } = string.Empty;
        public string ReadinessStatusClass { get; set; } = string.Empty;
        public int CriticalIssues { get; set; }
        public int WarningIssues { get; set; }
        public int EquipmentRowCount { get; set; }
        public int EquipmentIssueCount { get; set; }
        public string IssueSummary => $"Critical {CriticalIssues} | Warning {WarningIssues} | Equipment {EquipmentIssueCount}/{EquipmentRowCount}";
        public DateTime RecordedAtUtc { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string TemplateVersion { get; set; } = string.Empty;
        public bool VehicleSameAsPreviousShiftUsed { get; set; }
        public bool EquipmentSameAsPreviousShiftUsed { get; set; }
    }
}
