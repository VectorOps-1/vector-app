using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class OperationsReportsModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ExpiryPressureService _expiryPressure;

    public OperationsReportsModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ExpiryPressureService expiryPressure)
    {
        _db = db;
        _currentUser = currentUser;
        _expiryPressure = expiryPressure;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Metric { get; set; }

    public bool IsSeniorOverview { get; private set; }
    public string ScopeLabel { get; private set; } = "Company-wide";
    public string? StatusMessage { get; private set; }
    public string? SelectedMetricTitle { get; private set; }
    public string? SelectedMetricDescription { get; private set; }
    public DateTime FromUtc { get; private set; }
    public DateTime ToUtcExclusive { get; private set; }
    public ReportSummary Summary { get; private set; } = new();
    public bool HasNoReportActivity => Summary.TotalActivityCount == 0;
    public IReadOnlyList<MetricDetailRow> MetricRows { get; private set; } = [];
    public IReadOnlyList<ReadinessReportRow> ReadinessRows { get; private set; } = [];
    public IReadOnlyList<IssueReportRow> IssueRows { get; private set; } = [];
    public IReadOnlyList<TaskReportRow> TaskRows { get; private set; } = [];
    public IReadOnlyList<MovementReportRow> MovementRows { get; private set; } = [];
    public IReadOnlyList<SetupUploadRow> UploadRows { get; private set; } = [];
    public IReadOnlyList<ExpiryPressureItem> ExpiryRows { get; private set; } = [];
    public IReadOnlyList<AuditTrailRow> AuditRows { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        IsSeniorOverview = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        SetDateRange();

        var scope = await LoadReportScopeAsync(currentUser);
        await LoadReadinessRowsAsync(currentUser.CompanyId, scope.VehicleIds);
        await LoadIssueRowsAsync(currentUser, scope);
        await LoadTaskRowsAsync(currentUser, scope);
        await LoadMovementRowsAsync(currentUser.CompanyId, scope.AreaIds);
        await LoadUploadRowsAsync(currentUser);
        await LoadExpiryRowsAsync(currentUser);
        await LoadAuditRowsAsync(currentUser);

        ApplySearchFilter();
        BuildSummary();
        BuildMetricRows();

        return Page();
    }

    private void SetDateRange()
    {
        var today = DateTime.Today;
        var from = FromDate?.Date ?? today.AddDays(-7);
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

    private async Task<ReportScope> LoadReportScopeAsync(AppUser currentUser)
    {
        if (IsSeniorOverview)
        {
            ScopeLabel = "All bases / regions";
            var allVehicles = await _db.Vehicles
                .AsNoTracking()
                .Where(vehicle => vehicle.CompanyId == currentUser.CompanyId && vehicle.Status != "Deleted")
                .Select(vehicle => vehicle.Id)
                .ToListAsync();

            return new ReportScope([], allVehicles, [], [], []);
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
            .Select(assignment => new AreaScopeItem(
                assignment.OperationalAreaId,
                assignment.OperationalArea!.Name))
            .ToListAsync();

        if (assignedAreas.Count == 0)
        {
            ScopeLabel = "No assigned area";
            StatusMessage = "This operational manager has no assigned base or region yet. Senior management can assign manager areas in Master Setup.";
            return new ReportScope([], [], [], [], []);
        }

        var areaIds = assignedAreas.Select(area => area.Id).ToList();
        var scopedVehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle =>
                vehicle.CompanyId == currentUser.CompanyId &&
                vehicle.CurrentOperationalAreaId.HasValue &&
                areaIds.Contains(vehicle.CurrentOperationalAreaId.Value) &&
                vehicle.Status != "Deleted")
            .Select(vehicle => new
            {
                vehicle.Id,
                vehicle.RegistrationNumber,
                vehicle.Callsign
            })
            .ToListAsync();
        var vehicleIds = scopedVehicles.Select(vehicle => vehicle.Id).ToList();
        var vehicleLabels = scopedVehicles
            .SelectMany(vehicle => new[] { vehicle.RegistrationNumber, vehicle.Callsign })
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var staffIds = await _db.AppUsers
            .AsNoTracking()
            .Where(user =>
                user.CompanyId == currentUser.CompanyId &&
                user.Status != "Deleted" &&
                (user.Id == currentUser.Id ||
                    (user.AssignedOperationalAreaId.HasValue && areaIds.Contains(user.AssignedOperationalAreaId.Value))))
            .Select(user => user.Id)
            .ToListAsync();

        ScopeLabel = string.Join(", ", assignedAreas.Select(area => area.Name));
        return new ReportScope(areaIds, vehicleIds, assignedAreas.Select(area => area.Name).ToList(), vehicleLabels, staffIds);
    }

    private async Task LoadReadinessRowsAsync(int companyId, IReadOnlyList<int> scopedVehicleIds)
    {
        var query = _db.DailyVehicleReadinessReports
            .AsNoTracking()
            .Include(report => report.Vehicle)
                .ThenInclude(vehicle => vehicle!.CurrentOperationalArea)
            .Include(report => report.PerformedByUser)
            .Include(report => report.EquipmentChecks)
            .Where(report =>
                report.CompanyId == companyId &&
                (report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc) >= FromUtc &&
                (report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc) < ToUtcExclusive);

        if (!IsSeniorOverview)
        {
            query = query.Where(report => scopedVehicleIds.Contains(report.VehicleId));
        }

        ReadinessRows = await query
            .OrderByDescending(report => report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc)
            .Take(40)
            .Select(report => new ReadinessReportRow
            {
                Id = report.Id,
                VehicleLabel = report.Vehicle == null
                    ? $"{report.VehicleRegistrationNumber} / {report.CallsignAtCheck}"
                    : $"{report.Vehicle.RegistrationNumber} / {report.Vehicle.Callsign}",
                AreaName = report.Vehicle != null && report.Vehicle.CurrentOperationalArea != null
                    ? report.Vehicle.CurrentOperationalArea.Name
                    : "Unallocated",
                PerformedByName = report.PerformedByUser == null ? "Unknown" : report.PerformedByUser.FullName,
                WorkflowStatus = report.WorkflowStatus,
                ReadinessStatus = report.ReadinessStatus,
                CriticalIssues = report.CriticalIssueCount,
                WarningIssues = report.WarningIssueCount,
                EquipmentIssueCount = report.EquipmentChecks.Count(check => !check.IsOperational || check.IssueNotes != null),
                DetailUrl = $"/ChecklistReportDetail?id={report.Id}",
                RecordedAtUtc = report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.CreatedAtUtc
            })
            .ToListAsync();
    }

    private async Task LoadIssueRowsAsync(AppUser currentUser, ReportScope scope)
    {
        var query = _db.IssueReports
            .AsNoTracking()
            .Include(issue => issue.ReportedByUser)
            .Include(issue => issue.AssignedToUser)
            .Where(issue =>
                issue.CompanyId == currentUser.CompanyId &&
                issue.CreatedAtUtc >= FromUtc &&
                issue.CreatedAtUtc < ToUtcExclusive);

        var issues = await query
            .OrderByDescending(issue => issue.CreatedAtUtc)
            .ToListAsync();

        if (!IsSeniorOverview)
        {
            issues = issues
                .Where(issue =>
                    issue.AssignedToUserId == currentUser.Id ||
                    scope.AreaNames.Any(areaName => MatchesText(issue.Location, areaName)) ||
                    scope.VehicleLabels.Any(vehicleLabel => MatchesText(issue.Location, vehicleLabel) || MatchesText(issue.RelatedItem, vehicleLabel)))
                .ToList();
        }

        IssueRows = issues
            .Take(40)
            .Select(issue => new IssueReportRow
            {
                Id = issue.Id,
                Module = issue.Module,
                IssueType = issue.IssueType,
                Severity = issue.Severity,
                RelatedItem = issue.RelatedItem,
                Location = issue.Location,
                Status = issue.Status,
                ActionTaken = issue.ActionTaken,
                ReportedByName = issue.ReportedByUser == null ? "Unknown" : issue.ReportedByUser.FullName,
                AssignedToName = issue.AssignedToUser == null ? "Unknown" : issue.AssignedToUser.FullName,
                ResolvedAtUtc = issue.ResolvedAtUtc,
                CreatedAtUtc = issue.CreatedAtUtc
            })
            .ToList();
    }

    private async Task LoadTaskRowsAsync(AppUser currentUser, ReportScope scope)
    {
        var query = _db.TaskItems
            .AsNoTracking()
            .Include(task => task.AssignedToUser)
            .Include(task => task.AssignedByUser)
            .Where(task =>
                task.CompanyId == currentUser.CompanyId &&
                task.CreatedAtUtc >= FromUtc &&
                task.CreatedAtUtc < ToUtcExclusive);

        if (!IsSeniorOverview)
        {
            IReadOnlyList<int> scopedStaffIds = scope.StaffIds.Count == 0
                ? new List<int> { currentUser.Id }
                : scope.StaffIds;
            query = query.Where(task =>
                task.AssignedByUserId == currentUser.Id ||
                task.AssignedToUserId == currentUser.Id ||
                scopedStaffIds.Contains(task.AssignedByUserId) ||
                scopedStaffIds.Contains(task.AssignedToUserId));
        }

        TaskRows = await query
            .OrderByDescending(task => task.CreatedAtUtc)
            .Take(40)
            .Select(task => new TaskReportRow
            {
                Id = task.Id,
                ActionType = task.ActionType,
                RelatedItemReference = task.RelatedItemReference,
                Status = task.Status,
                AssignedToName = task.AssignedToUser == null ? "Unknown" : task.AssignedToUser.FullName,
                AssignedByName = task.AssignedByUser == null ? "Unknown" : task.AssignedByUser.FullName,
                CreatedAtUtc = task.CreatedAtUtc,
                CompletedAtUtc = task.CompletedAtUtc
            })
            .ToListAsync();
    }

    private async Task LoadMovementRowsAsync(int companyId, IReadOnlyList<int> scopedAreaIds)
    {
        var query = _db.AssetMovements
            .AsNoTracking()
            .Include(movement => movement.MovedByUser)
            .Include(movement => movement.FromOperationalArea)
            .Include(movement => movement.ToOperationalArea)
            .Where(movement =>
                movement.CompanyId == companyId &&
                movement.CreatedAtUtc >= FromUtc &&
                movement.CreatedAtUtc < ToUtcExclusive);

        if (!IsSeniorOverview)
        {
            query = query.Where(movement =>
                scopedAreaIds.Contains(movement.ToOperationalAreaId) ||
                (movement.FromOperationalAreaId.HasValue && scopedAreaIds.Contains(movement.FromOperationalAreaId.Value)));
        }

        MovementRows = await query
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .Take(40)
            .Select(movement => new MovementReportRow
            {
                Id = movement.Id,
                AssetType = movement.AssetType,
                AssetLabel = movement.AssetLabel,
                FromLocation = movement.FromOperationalArea == null
                    ? movement.FromLocationText
                    : movement.FromOperationalArea.Name,
                ToLocation = movement.ToOperationalArea == null
                    ? movement.ToLocationText
                    : movement.ToOperationalArea.Name,
                QuantityMoved = movement.QuantityMoved,
                LinkedTaskId = movement.TaskItemId,
                MovedByName = movement.MovedByUser == null ? "Unknown" : movement.MovedByUser.FullName,
                CreatedAtUtc = movement.CreatedAtUtc
            })
            .ToListAsync();
    }

    private async Task LoadUploadRowsAsync(AppUser currentUser)
    {
        if (!IsSeniorOverview)
        {
            UploadRows = [];
            return;
        }

        UploadRows = await _db.AssetFiles
            .AsNoTracking()
            .Include(file => file.UploadedByUser)
            .Where(file =>
                file.CompanyId == currentUser.CompanyId &&
                file.UploadedAtUtc >= FromUtc &&
                file.UploadedAtUtc < ToUtcExclusive &&
                (file.LinkedEntityType == SetupUploadService.ChecklistUploadEntityType ||
                    file.LinkedEntityType == SetupUploadService.RegisterUploadEntityType))
            .OrderByDescending(file => file.UploadedAtUtc)
            .Take(30)
            .Select(file => new SetupUploadRow
            {
                Id = file.Id,
                FileName = file.OriginalFileName,
                Category = file.Category,
                LinkedEntityType = file.LinkedEntityType,
                SizeBytes = file.SizeBytes,
                UploadedByName = file.UploadedByUser == null ? "Unknown" : file.UploadedByUser.FullName,
                UploadedAtUtc = file.UploadedAtUtc
            })
            .ToListAsync();
    }

    private async Task LoadExpiryRowsAsync(AppUser currentUser)
    {
        ExpiryRows = await _expiryPressure.LoadForUserAsync(currentUser, ToDate?.Date ?? DateTime.Today);
    }

    private async Task LoadAuditRowsAsync(AppUser currentUser)
    {
        if (!IsSeniorOverview)
        {
            AuditRows = [];
            return;
        }

        AuditRows = await _db.AuditLogs
            .AsNoTracking()
            .Include(log => log.AppUser)
            .Where(log =>
                log.CompanyId == currentUser.CompanyId &&
                log.CreatedAtUtc >= FromUtc &&
                log.CreatedAtUtc < ToUtcExclusive)
            .OrderByDescending(log => log.CreatedAtUtc)
            .Take(40)
            .Select(log => new AuditTrailRow
            {
                Id = log.Id,
                Action = log.Action,
                EntityType = log.EntityType,
                Details = log.Details,
                UserName = log.AppUser == null ? "System / unknown" : log.AppUser.FullName,
                CreatedAtUtc = log.CreatedAtUtc
            })
            .ToListAsync();
    }

    private void ApplySearchFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            return;
        }

        var search = SearchTerm.Trim();
        ReadinessRows = ReadinessRows.Where(row => ContainsAny(search, row.VehicleLabel, row.AreaName, row.PerformedByName, row.ReadinessStatus, row.WorkflowStatus)).ToList();
        IssueRows = IssueRows.Where(row => ContainsAny(search, row.Module, row.IssueType, row.RelatedItem, row.Location, row.Status, row.ReportedByName, row.AssignedToName)).ToList();
        TaskRows = TaskRows.Where(row => ContainsAny(search, row.ActionType, row.Status, row.AssignedToName, row.AssignedByName)).ToList();
        MovementRows = MovementRows.Where(row => ContainsAny(search, row.AssetType, row.AssetLabel, row.FromLocation, row.ToLocation, row.MovedByName)).ToList();
        UploadRows = UploadRows.Where(row => ContainsAny(search, row.FileName, row.Category, row.LinkedEntityType, row.UploadedByName)).ToList();
        ExpiryRows = ExpiryRows.Where(row => ContainsAny(search, row.AssetType, row.AssetLabel, row.Source, row.Location, row.OwnerName, row.AlertBand, row.Status, row.ActionState)).ToList();
        AuditRows = AuditRows.Where(row => ContainsAny(search, row.Action, row.EntityType, row.Details, row.UserName)).ToList();
    }

    private void BuildSummary()
    {
        Summary = new ReportSummary
        {
            SubmittedReadinessChecks = ReadinessRows.Count(row => IsSavedOrSubmitted(row.WorkflowStatus)),
            DraftReadinessChecks = ReadinessRows.Count(row => IsDraft(row.WorkflowStatus)),
            OpenIssues = IssueRows.Count(row => string.Equals(row.Status, "Open", StringComparison.OrdinalIgnoreCase)),
            OpenTasks = TaskRows.Count(row => string.Equals(row.Status, "Open", StringComparison.OrdinalIgnoreCase)),
            CompletedTasks = TaskRows.Count(row => row.CompletedAtUtc.HasValue || string.Equals(row.Status, "Completed", StringComparison.OrdinalIgnoreCase)),
            AssetMovements = MovementRows.Count,
            SetupUploads = UploadRows.Count,
            ServiceOrExpiryPressure = ExpiryRows.Count,
            AuditEvents = AuditRows.Count
        };
    }

    private void BuildMetricRows()
    {
        var metric = NormalizeMetric(Metric);
        if (metric is null)
        {
            MetricRows = [];
            return;
        }

        (string? title, string? description, IReadOnlyList<MetricDetailRow> rows) metricResult = metric switch
        {
            "submitted-checks" => (
                "Saved / Submitted Checks",
                "Checklist evidence that has been saved or submitted in this report window.",
                ReadinessRows
                    .Where(row => IsSavedOrSubmitted(row.WorkflowStatus))
                    .Select(row => new MetricDetailRow
                    {
                        Source = "Daily vehicle and equipment check",
                        Item = row.VehicleLabel,
                        Callsign = ExtractCallsign(row.VehicleLabel),
                        Area = row.AreaName,
                        SubmittedBy = row.PerformedByName,
                        RecordedAtUtc = row.RecordedAtUtc,
                        Status = row.ReadinessStatus,
                        ActionState = row.CriticalIssues > 0 || row.WarningIssues > 0 || row.EquipmentIssueCount > 0
                            ? $"Issues captured: Critical {row.CriticalIssues}, Warning {row.WarningIssues}, Equipment {row.EquipmentIssueCount}"
                            : "No action required from captured data",
                        DetailUrl = row.DetailUrl
                    })
                    .ToList()),

            "drafts" => (
                "Saved Drafts",
                "Incomplete checks that were saved but not submitted.",
                ReadinessRows
                    .Where(row => IsDraft(row.WorkflowStatus))
                    .Select(row => new MetricDetailRow
                    {
                        Source = "Daily check draft",
                        Item = row.VehicleLabel,
                        Callsign = ExtractCallsign(row.VehicleLabel),
                        Area = row.AreaName,
                        SubmittedBy = row.PerformedByName,
                        RecordedAtUtc = row.RecordedAtUtc,
                        Status = row.WorkflowStatus,
                        ActionState = "Awaiting completion / submission",
                        DetailUrl = row.DetailUrl
                    })
                    .ToList()),

            "open-issues" => (
                "Open Issues",
                "Issues currently open in the selected scope.",
                IssueRows
                    .Where(row => string.Equals(row.Status, "Open", StringComparison.OrdinalIgnoreCase))
                    .Select(row => new MetricDetailRow
                    {
                        Source = $"{row.Module} issue report",
                        Item = FirstNonEmpty(row.RelatedItem, row.IssueType, "Issue report"),
                        Callsign = ExtractCallsign(row.RelatedItem, row.Location),
                        Area = FirstNonEmpty(row.Location, "Location not set"),
                        SubmittedBy = row.ReportedByName,
                        AssignedTo = row.AssignedToName,
                        RecordedAtUtc = row.CreatedAtUtc,
                        Status = FirstNonEmpty(row.Severity, row.Status),
                        ActionState = string.IsNullOrWhiteSpace(row.ActionTaken) ? "No action recorded yet" : row.ActionTaken!,
                        DetailUrl = $"/IssueReportAction?issueId={row.Id}&pool=true"
                    })
                    .ToList()),

            "open-tasks" => (
                "Open Tasks",
                "Tasks that are still open in the selected scope.",
                TaskRows
                    .Where(row => string.Equals(row.Status, "Open", StringComparison.OrdinalIgnoreCase))
                    .Select(row => new MetricDetailRow
                    {
                        Source = "Task communication",
                        Item = FirstNonEmpty(row.RelatedItemReference, row.ActionType),
                        Callsign = ExtractCallsign(row.RelatedItemReference),
                        SubmittedBy = row.AssignedByName,
                        AssignedTo = row.AssignedToName,
                        RecordedAtUtc = row.CreatedAtUtc,
                        Status = row.Status,
                        ActionState = "Awaiting feedback or completion",
                        DetailUrl = "/TaskInbox"
                    })
                    .ToList()),

            "completed-tasks" => (
                "Completed Tasks",
                "Tasks completed in the selected report window.",
                TaskRows
                    .Where(row => row.CompletedAtUtc.HasValue || string.Equals(row.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    .Select(row => new MetricDetailRow
                    {
                        Source = "Task communication",
                        Item = FirstNonEmpty(row.RelatedItemReference, row.ActionType),
                        Callsign = ExtractCallsign(row.RelatedItemReference),
                        SubmittedBy = row.AssignedByName,
                        AssignedTo = row.AssignedToName,
                        RecordedAtUtc = row.CompletedAtUtc ?? row.CreatedAtUtc,
                        Status = row.Status,
                        ActionState = $"Completed{(row.CompletedAtUtc.HasValue ? $" at {row.CompletedAtUtc.Value.ToLocalTime():yyyy-MM-dd HH:mm}" : string.Empty)}",
                        DetailUrl = "/TaskInbox"
                    })
                    .ToList()),

            "asset-movements" => (
                "Asset Movements",
                "Vehicle, equipment, stock, and medication movements captured in this scope.",
                MovementRows
                    .Select(row => new MetricDetailRow
                    {
                        Source = $"{row.AssetType} movement",
                        Item = row.AssetLabel,
                        Callsign = ExtractCallsign(row.AssetLabel, row.FromLocation, row.ToLocation),
                        Area = $"{FirstNonEmpty(row.FromLocation, "Unallocated")} -> {FirstNonEmpty(row.ToLocation, "Unknown")}",
                        SubmittedBy = row.MovedByName,
                        RecordedAtUtc = row.CreatedAtUtc,
                        Status = row.QuantityMoved.HasValue ? $"Quantity {row.QuantityMoved}" : "Movement logged",
                        ActionState = row.LinkedTaskId.HasValue ? $"Movement completed from task #{row.LinkedTaskId.Value}" : "Movement completed directly",
                        DetailUrl = "/MoveAsset"
                    })
                    .ToList()),

            "service-expiry" => (
                "Service / Expiry Pressure",
                "Register service dates, licence dates, stock expiry, medication expiry, and staff compliance dates that need attention soon.",
                ExpiryRows
                    .Select(row => new MetricDetailRow
                    {
                        Source = row.AssetType,
                        Item = row.AssetLabel,
                        Callsign = ExtractCallsign(row.AssetLabel, row.Location),
                        Area = FirstNonEmpty(row.Location, "No location"),
                        SubmittedBy = FirstNonEmpty(row.OwnerName, "Register"),
                        RecordedAtUtc = row.DueAtUtc,
                        Status = row.AlertBand,
                        ActionState = $"{row.ActionState} Register status: {row.Status}.",
                        DetailUrl = row.DetailUrl
                    })
                    .ToList()),

            "setup-uploads" => (
                "Setup Uploads",
                "Register and checklist source uploads saved during setup.",
                UploadRows
                    .Select(row => new MetricDetailRow
                    {
                        Source = row.LinkedEntityType,
                        Item = row.FileName,
                        Area = row.Category,
                        SubmittedBy = row.UploadedByName,
                        RecordedAtUtc = row.UploadedAtUtc,
                        Status = $"{Math.Max(1, row.SizeBytes / 1024)} KB",
                        ActionState = "Uploaded and stored as setup evidence",
                        DetailUrl = "/MasterSetup"
                    })
                    .ToList()),

            _ => (null, null, Array.Empty<MetricDetailRow>())
        };

        SelectedMetricTitle = metricResult.title;
        SelectedMetricDescription = metricResult.description;
        MetricRows = metricResult.rows;
    }

    public string MetricUrl(string metric)
    {
        var parameters = BuildCurrentQueryParameters();
        parameters["Metric"] = metric;
        return BuildUrl(parameters);
    }

    public string OverviewUrl()
    {
        return BuildUrl(BuildCurrentQueryParameters());
    }

    public bool IsMetricSelected(string metric)
    {
        return string.Equals(NormalizeMetric(Metric), metric, StringComparison.OrdinalIgnoreCase);
    }

    private Dictionary<string, string> BuildCurrentQueryParameters()
    {
        var parameters = new Dictionary<string, string>
        {
            ["FromDate"] = FromDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            ["ToDate"] = ToDate?.ToString("yyyy-MM-dd") ?? string.Empty
        };

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            parameters["SearchTerm"] = SearchTerm.Trim();
        }

        return parameters;
    }

    private static string BuildUrl(Dictionary<string, string> parameters)
    {
        var query = string.Join("&", parameters
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
            .Select(parameter => $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));

        return string.IsNullOrWhiteSpace(query) ? "/OperationsReports" : $"/OperationsReports?{query}";
    }

    private static string? NormalizeMetric(string? metric)
    {
        if (string.IsNullOrWhiteSpace(metric))
        {
            return null;
        }

        return metric.Trim().ToLowerInvariant() switch
        {
            "submitted-checks" or "saved-submitted-checks" => "submitted-checks",
            "drafts" or "saved-drafts" => "drafts",
            "open-issues" => "open-issues",
            "open-tasks" => "open-tasks",
            "completed-tasks" => "completed-tasks",
            "asset-movements" => "asset-movements",
            "service-expiry" or "service-expiry-pressure" => "service-expiry",
            "setup-uploads" => "setup-uploads",
            _ => null
        };
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "N/A";
    }

    private static string ExtractCallsign(params string?[] values)
    {
        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var text = value!.Trim();
            var parts = text
                .Split(['/', '|', '-', ',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(part => part.Any(char.IsDigit) && part.Any(char.IsLetter))
                .ToList();

            if (parts.Count > 0)
            {
                return parts[^1];
            }
        }

        return "N/A";
    }

    private static bool ContainsAny(string search, params string?[] values)
    {
        return values.Any(value => !string.IsNullOrWhiteSpace(value) && value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesText(string? value, string? search)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            !string.IsNullOrWhiteSpace(search) &&
            value.Contains(search, StringComparison.OrdinalIgnoreCase);
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

    private sealed record AreaScopeItem(int Id, string Name);
    private sealed record ReportScope(
        IReadOnlyList<int> AreaIds,
        IReadOnlyList<int> VehicleIds,
        IReadOnlyList<string> AreaNames,
        IReadOnlyList<string> VehicleLabels,
        IReadOnlyList<int> StaffIds);

    public sealed class ReportSummary
    {
        public int SubmittedReadinessChecks { get; set; }
        public int DraftReadinessChecks { get; set; }
        public int OpenIssues { get; set; }
        public int OpenTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int AssetMovements { get; set; }
        public int SetupUploads { get; set; }
        public int ServiceOrExpiryPressure { get; set; }
        public int AuditEvents { get; set; }
        public int TotalActivityCount =>
            SubmittedReadinessChecks +
            DraftReadinessChecks +
            OpenIssues +
            OpenTasks +
            CompletedTasks +
            AssetMovements +
            SetupUploads +
            ServiceOrExpiryPressure;
    }

    public sealed class ReadinessReportRow
    {
        public int Id { get; set; }
        public string VehicleLabel { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
        public string WorkflowStatus { get; set; } = string.Empty;
        public string ReadinessStatus { get; set; } = string.Empty;
        public int CriticalIssues { get; set; }
        public int WarningIssues { get; set; }
        public int EquipmentIssueCount { get; set; }
        public string DetailUrl { get; set; } = string.Empty;
        public DateTime RecordedAtUtc { get; set; }
    }

    public sealed class IssueReportRow
    {
        public int Id { get; set; }
        public string Module { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string? Severity { get; set; }
        public string? RelatedItem { get; set; }
        public string? Location { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ActionTaken { get; set; }
        public string ReportedByName { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public DateTime? ResolvedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class TaskReportRow
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? RelatedItemReference { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public string AssignedByName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public sealed class MovementReportRow
    {
        public int Id { get; set; }
        public string AssetType { get; set; } = string.Empty;
        public string AssetLabel { get; set; } = string.Empty;
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public int? QuantityMoved { get; set; }
        public int? LinkedTaskId { get; set; }
        public string MovedByName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class SetupUploadRow
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string LinkedEntityType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime UploadedAtUtc { get; set; }
    }

    public sealed class ExpiryPressureRow
    {
        public string AssetType { get; set; } = string.Empty;
        public string AssetLabel { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateTime? DueAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public sealed class AuditTrailRow
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class MetricDetailRow
    {
        public string Source { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        public string Callsign { get; set; } = "N/A";
        public string Area { get; set; } = "N/A";
        public string SubmittedBy { get; set; } = "N/A";
        public string AssignedTo { get; set; } = "N/A";
        public DateTime? RecordedAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ActionState { get; set; } = string.Empty;
        public string DetailUrl { get; set; } = string.Empty;
    }
}
