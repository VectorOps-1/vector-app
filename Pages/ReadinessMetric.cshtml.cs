using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ReadinessMetricModel : PageModel
{
    private const int ShiftHours = 12;

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ReadinessMetricModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? Metric { get; set; }
    [BindProperty(SupportsGet = true)] public int? OperationalAreaId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Confirmation { get; set; }

    public string MetricHeading { get; private set; } = "Readiness Metric";
    public string MetricSubtitle { get; private set; } = "Vehicles affecting the current shift readiness score.";
    public string PrimaryColumnHeading { get; private set; } = "Callsign";
    public string DetailColumnHeading { get; private set; } = "Discrepancy";
    public string OwnerColumnHeading { get; private set; } = "Assigned Manager";
    public string EmptyMessage { get; private set; } = "No Variables Collected";
    public string? StatusMessage { get; private set; }
    public List<ReadinessMetricRow> Rows { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CanViewReadinessMetrics(currentUser))
        {
            return RedirectToPage("/Home");
        }

        MetricHeading = GetMetricHeading(Metric);
        MetricSubtitle = GetMetricSubtitle(Metric);
        (PrimaryColumnHeading, DetailColumnHeading, OwnerColumnHeading) = GetMetricColumnHeadings(Metric);
        EmptyMessage = GetMetricEmptyMessage(Metric);
        StatusMessage = Confirmation switch
        {
            "readiness-row-deleted" => "Readiness list item deleted.",
            _ => null
        };

        if (string.Equals(Metric, "ready-vehicles", StringComparison.OrdinalIgnoreCase))
        {
            Rows = await LoadReadyVehicleVariablesAsync(currentUser);
        }
        else if (string.Equals(Metric, "checked-this-shift", StringComparison.OrdinalIgnoreCase))
        {
            Rows = await LoadCompletedReadinessCheckVariablesAsync(currentUser);
        }
        else if (string.Equals(Metric, "equipment-alerts", StringComparison.OrdinalIgnoreCase))
        {
            Rows = await LoadEquipmentAlertVariablesAsync(currentUser);
        }
        else if (string.Equals(Metric, "open-issues", StringComparison.OrdinalIgnoreCase))
        {
            Rows = await LoadOpenIssueVariablesAsync(currentUser);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        string? metric,
        int? operationalAreaId,
        int vehicleId,
        int? reportId,
        int? issueId,
        string? alertKey)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CanViewReadinessMetrics(currentUser))
        {
            return RedirectToPage("/Home");
        }

        MetricHeading = GetMetricHeading(metric);

        if (issueId.HasValue)
        {
            await DeleteIssueSourceAsync(currentUser, issueId.Value);
        }
        else if (reportId.HasValue && string.IsNullOrWhiteSpace(alertKey))
        {
            await DeleteChecklistSourceAsync(currentUser, reportId.Value);
        }

        if (!string.IsNullOrWhiteSpace(alertKey))
        {
            await DismissMetricAlertRowAsync(currentUser, metric, vehicleId, alertKey);
        }
        else if (vehicleId > 0)
        {
            await DismissVehicleMetricRowAsync(currentUser, metric, vehicleId);
        }

        return RedirectToPage("/ReadinessMetric", new
        {
            metric,
            operationalAreaId,
            confirmation = "readiness-row-deleted"
        });
    }

    private static string GetMetricHeading(string? metric)
    {
        return metric switch
        {
            "ready-vehicles" => "Ready Vehicles",
            "checked-this-shift" => "Daily Vehicle Checks Completed",
            "missing-checks" => "Missing Checks",
            "not-ready" => "Not Ready",
            "equipment-alerts" => "Equipment Alerts",
            "open-issues" => "Open Issues",
            _ => "Readiness Metric"
        };
    }

    private static string GetMetricSubtitle(string? metric)
    {
        return metric switch
        {
            "ready-vehicles" => "Vehicle readiness state for the current 12-hour shift.",
            "checked-this-shift" => "Units with a completed daily vehicle readiness check, including the equipment section, during this shift.",
            "missing-checks" => "Units that still need a completed daily vehicle readiness check this shift.",
            "not-ready" => "Units currently reducing readiness because of critical or unavailable status.",
            "equipment-alerts" => "Equipment values from completed checks that need manager attention.",
            "open-issues" => "Open issue reports affecting the current shift readiness view.",
            _ => "Variables collected for the selected readiness metric."
        };
    }

    private static (string Primary, string Detail, string Owner) GetMetricColumnHeadings(string? metric)
    {
        return metric switch
        {
            "checked-this-shift" => ("Callsign", "Completed check", "Completed by"),
            "equipment-alerts" => ("Callsign", "Type of alert", "Assigned Manager"),
            "open-issues" => ("Issue source", "Open issue", "Assigned to"),
            _ => ("Callsign", "Discrepancy", "Assigned Manager")
        };
    }

    private static string GetMetricEmptyMessage(string? metric)
    {
        return metric switch
        {
            "checked-this-shift" => "No completed daily vehicle readiness checks this shift.",
            "equipment-alerts" => "No equipment alerts collected.",
            "open-issues" => "No open issue reports collected.",
            _ => "No Variables Collected"
        };
    }

    private async Task<List<ReadinessMetricRow>> LoadReadyVehicleVariablesAsync(AppUser currentUser)
    {
        var isSeniorOverview = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        var allowedAreaIds = await LoadAllowedAreaIdsAsync(currentUser, isSeniorOverview);
        if (!isSeniorOverview && allowedAreaIds.Count == 0)
        {
            return [];
        }

        var shiftStartUtc = DateTime.UtcNow.AddHours(-ShiftHours);
        var vehicleQuery = _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle => vehicle.CompanyId == currentUser.CompanyId && vehicle.Status != "Deleted");

        if (OperationalAreaId.HasValue)
        {
            vehicleQuery = vehicleQuery.Where(vehicle => vehicle.CurrentOperationalAreaId == OperationalAreaId.Value);
        }
        else if (!isSeniorOverview)
        {
            vehicleQuery = vehicleQuery.Where(vehicle =>
                vehicle.CurrentOperationalAreaId.HasValue &&
                allowedAreaIds.Contains(vehicle.CurrentOperationalAreaId.Value));
        }

        var vehicles = await vehicleQuery
            .OrderBy(vehicle => vehicle.CurrentOperationalArea == null ? "Unallocated" : vehicle.CurrentOperationalArea.Name)
            .ThenBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync();

        var vehicleIds = vehicles.Select(vehicle => vehicle.Id).ToList();
        var recentReports = vehicleIds.Count == 0
            ? []
            : await _db.DailyVehicleReadinessReports
                .AsNoTracking()
                .Include(report => report.EquipmentChecks)
                .Where(report =>
                    report.CompanyId == currentUser.CompanyId &&
                    report.WorkflowStatus != "Deleted" &&
                    vehicleIds.Contains(report.VehicleId) &&
                    (report.SubmittedAtUtc ?? report.InspectionDateUtc) >= shiftStartUtc)
                .ToListAsync();

        var latestReports = recentReports
            .GroupBy(report => report.VehicleId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(report => report.SubmittedAtUtc ?? report.InspectionDateUtc)
                    .ThenByDescending(report => report.Id)
                    .First());

        var openIssues = await _db.IssueReports
            .AsNoTracking()
            .Where(issue => issue.CompanyId == currentUser.CompanyId && issue.Status == "Open")
            .OrderByDescending(issue => issue.CreatedAtUtc)
            .ToListAsync();

        if (OperationalAreaId.HasValue)
        {
            var selectedAreaName = vehicles
                .Select(vehicle => vehicle.CurrentOperationalArea?.Name)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty;
            var scopedVehicleLabels = vehicles.SelectMany(vehicle => new[] { vehicle.RegistrationNumber, vehicle.Callsign }).ToList();
            openIssues = openIssues
                .Where(issue => MatchesText(issue.Location, selectedAreaName) || scopedVehicleLabels.Any(label => MatchesIssue(issue, label)))
                .ToList();
        }

        var managersByArea = await LoadManagersByAreaAsync(currentUser.CompanyId);
        var dismissedVehicleIds = await LoadDismissedVehicleIdsAsync(currentUser.CompanyId, Metric, shiftStartUtc);

        return vehicles
            .Where(vehicle => !dismissedVehicleIds.Contains(vehicle.Id))
            .Select(vehicle => BuildVehicleMetricRow(
                vehicle,
                latestReports.GetValueOrDefault(vehicle.Id),
                openIssues,
                managersByArea))
            .Select(row => new ReadinessMetricRow
            {
                IsReady = row.IsReady,
                VehicleId = row.VehicleId,
                ReportId = row.ReportId,
                IssueId = row.IssueId,
                Callsign = row.Callsign,
                Discrepancy = row.Discrepancy,
                AssignedManager = row.AssignedManager,
                TargetUrl = row.TargetUrl
            })
            .ToList();
    }

    private async Task<List<ReadinessMetricRow>> LoadCompletedReadinessCheckVariablesAsync(AppUser currentUser)
    {
        var isSeniorOverview = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        var allowedAreaIds = await LoadAllowedAreaIdsAsync(currentUser, isSeniorOverview);
        if (!isSeniorOverview && allowedAreaIds.Count == 0)
        {
            return [];
        }

        var shiftStartUtc = DateTime.UtcNow.AddHours(-ShiftHours);
        var vehicleQuery = _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle => vehicle.CompanyId == currentUser.CompanyId && vehicle.Status != "Deleted");

        if (OperationalAreaId.HasValue)
        {
            vehicleQuery = vehicleQuery.Where(vehicle => vehicle.CurrentOperationalAreaId == OperationalAreaId.Value);
        }
        else if (!isSeniorOverview)
        {
            vehicleQuery = vehicleQuery.Where(vehicle =>
                vehicle.CurrentOperationalAreaId.HasValue &&
                allowedAreaIds.Contains(vehicle.CurrentOperationalAreaId.Value));
        }

        var vehicles = await vehicleQuery
            .OrderBy(vehicle => vehicle.CurrentOperationalArea == null ? "Unallocated" : vehicle.CurrentOperationalArea.Name)
            .ThenBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync();

        var vehicleIds = vehicles.Select(vehicle => vehicle.Id).ToList();
        var recentReports = vehicleIds.Count == 0
            ? []
            : await _db.DailyVehicleReadinessReports
                .AsNoTracking()
                .Include(report => report.PerformedByUser)
                .Include(report => report.EquipmentChecks)
                .Where(report =>
                    report.CompanyId == currentUser.CompanyId &&
                    report.WorkflowStatus != "Deleted" &&
                    vehicleIds.Contains(report.VehicleId) &&
                    (report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.InspectionDateUtc) >= shiftStartUtc)
                .ToListAsync();

        var latestCompletedReports = recentReports
            .Where(IsReadinessCheckComplete)
            .GroupBy(report => report.VehicleId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(report => report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.InspectionDateUtc)
                    .ThenByDescending(report => report.Id)
                    .First());

        var dismissedVehicleIds = await LoadDismissedVehicleIdsAsync(currentUser.CompanyId, Metric, shiftStartUtc);

        return vehicles
            .Where(vehicle => latestCompletedReports.ContainsKey(vehicle.Id))
            .Where(vehicle => !dismissedVehicleIds.Contains(vehicle.Id))
            .Select(vehicle =>
            {
                var report = latestCompletedReports[vehicle.Id];
                var completedAt = report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.InspectionDateUtc;
                var statusSummary = BuildCompletedCheckStatusSummary(report);

                return new ReadinessMetricRow
                {
                    IsReady = statusSummary.IsReady,
                    VehicleId = vehicle.Id,
                    ReportId = report.Id,
                    Callsign = string.IsNullOrWhiteSpace(vehicle.Callsign) ? vehicle.RegistrationNumber : $"{vehicle.Callsign} / {vehicle.RegistrationNumber}",
                    Discrepancy = $"{completedAt.ToLocalTime():yyyy-MM-dd HH:mm} - {statusSummary.Summary}",
                    AssignedManager = report.PerformedByUser?.FullName ?? "No staff member recorded",
                    TargetUrl = $"/ReadinessMetricDetail?metric=checked-this-shift&vehicleId={vehicle.Id}&reportId={report.Id}"
                };
            })
            .ToList();
    }

    private async Task<List<ReadinessMetricRow>> LoadEquipmentAlertVariablesAsync(AppUser currentUser)
    {
        var isSeniorOverview = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        var allowedAreaIds = await LoadAllowedAreaIdsAsync(currentUser, isSeniorOverview);
        if (!isSeniorOverview && allowedAreaIds.Count == 0)
        {
            return [];
        }

        var shiftStartUtc = DateTime.UtcNow.AddHours(-ShiftHours);
        var vehicleQuery = _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle => vehicle.CompanyId == currentUser.CompanyId && vehicle.Status != "Deleted");

        if (OperationalAreaId.HasValue)
        {
            vehicleQuery = vehicleQuery.Where(vehicle => vehicle.CurrentOperationalAreaId == OperationalAreaId.Value);
        }
        else if (!isSeniorOverview)
        {
            vehicleQuery = vehicleQuery.Where(vehicle =>
                vehicle.CurrentOperationalAreaId.HasValue &&
                allowedAreaIds.Contains(vehicle.CurrentOperationalAreaId.Value));
        }

        var vehicles = await vehicleQuery
            .OrderBy(vehicle => vehicle.CurrentOperationalArea == null ? "Unallocated" : vehicle.CurrentOperationalArea.Name)
            .ThenBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync();

        var vehicleIds = vehicles.Select(vehicle => vehicle.Id).ToList();
        var recentReports = vehicleIds.Count == 0
            ? []
            : await _db.DailyVehicleReadinessReports
                .AsNoTracking()
                .Include(report => report.EquipmentChecks)
                .Where(report =>
                    report.CompanyId == currentUser.CompanyId &&
                    report.WorkflowStatus != "Deleted" &&
                    vehicleIds.Contains(report.VehicleId) &&
                    (report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.InspectionDateUtc) >= shiftStartUtc)
                .ToListAsync();

        var latestReports = recentReports
            .GroupBy(report => report.VehicleId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(report => report.SubmittedAtUtc ?? report.LastSavedAtUtc ?? report.InspectionDateUtc)
                    .ThenByDescending(report => report.Id)
                    .First());

        var openIssues = (await _db.IssueReports
            .AsNoTracking()
            .Where(issue => issue.CompanyId == currentUser.CompanyId && issue.Status == "Open")
            .OrderByDescending(issue => issue.CreatedAtUtc)
            .ToListAsync())
            .Where(IsEquipmentIssueReport)
            .ToList();

        var varianceAlerts = vehicleIds.Count == 0
            ? []
            : await _db.ChecklistVarianceAlerts
                .AsNoTracking()
                .Where(alert =>
                    alert.CompanyId == currentUser.CompanyId &&
                    alert.Status == "Open" &&
                    alert.VehicleId.HasValue &&
                    vehicleIds.Contains(alert.VehicleId.Value) &&
                    alert.CreatedAtUtc >= shiftStartUtc)
                .OrderByDescending(alert => alert.CreatedAtUtc)
                .ToListAsync();

        var equipmentItems = await _db.EquipmentItems
            .AsNoTracking()
            .Where(item => item.CompanyId == currentUser.CompanyId && item.Status != "Deleted")
            .ToListAsync();

        var equipmentBySerial = equipmentItems
            .Where(item => !string.IsNullOrWhiteSpace(item.SerialOrAssetId))
            .GroupBy(item => NormalizeAssetId(item.SerialOrAssetId))
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .ToDictionary(group => group.Key, group => group.First());

        var activeAssignments = await _db.VehicleEquipmentAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.Status != "Deleted" &&
                assignment.VehicleId.HasValue &&
                assignment.EquipmentItemId.HasValue &&
                vehicleIds.Contains(assignment.VehicleId.Value))
            .Select(assignment => new { VehicleId = assignment.VehicleId!.Value, EquipmentItemId = assignment.EquipmentItemId!.Value })
            .ToListAsync();

        var assignedEquipmentPairs = activeAssignments
            .Select(assignment => $"{assignment.VehicleId}:{assignment.EquipmentItemId}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var managersByArea = await LoadManagersByAreaAsync(currentUser.CompanyId);
        var dismissedAlertKeys = await LoadDismissedAlertKeysAsync(currentUser.CompanyId, Metric, shiftStartUtc);
        var rows = new List<ReadinessMetricRow>();

        foreach (var vehicle in vehicles)
        {
            var assignedManager = vehicle.CurrentOperationalAreaId.HasValue &&
                managersByArea.TryGetValue(vehicle.CurrentOperationalAreaId.Value, out var managerNames)
                    ? managerNames
                    : "No manager assigned";
            var callsign = string.IsNullOrWhiteSpace(vehicle.Callsign)
                ? vehicle.RegistrationNumber
                : $"{vehicle.Callsign} / {vehicle.RegistrationNumber}";

            latestReports.TryGetValue(vehicle.Id, out var report);

            if (IsUnavailable(vehicle.Status))
            {
                AddEquipmentAlertRow(
                    rows,
                    dismissedAlertKeys,
                    vehicle.Id,
                    report?.Id,
                    null,
                    $"vehicle-problem:{vehicle.Id}:{vehicle.Status}",
                    false,
                    callsign,
                    $"Vehicle problem: vehicle register status is {vehicle.Status}",
                    assignedManager,
                    BuildMetricDetailUrl(vehicle.Id, report?.Id, null));
            }

            if (report is not null && !report.EquipmentChecks.Any())
            {
                AddEquipmentAlertRow(
                    rows,
                    dismissedAlertKeys,
                    vehicle.Id,
                    report.Id,
                    null,
                    $"equipment-omitted:{vehicle.Id}:{report.Id}",
                    false,
                    callsign,
                    "Omitted checklist items: equipment section has no captured equipment rows",
                    assignedManager,
                    BuildMetricDetailUrl(vehicle.Id, report.Id, null));
            }

            if (report is not null)
            {
                foreach (var check in report.EquipmentChecks.OrderBy(check => check.SortOrder).ThenBy(check => check.EquipmentName))
                {
                    var problemSummary = BuildEquipmentCheckAlertSummary(check);
                    if (!string.IsNullOrWhiteSpace(problemSummary))
                    {
                        AddEquipmentAlertRow(
                            rows,
                            dismissedAlertKeys,
                            vehicle.Id,
                            report.Id,
                            null,
                            $"equipment-problem:{vehicle.Id}:{report.Id}:{check.Id}",
                            false,
                            callsign,
                            $"Problem marked: {check.EquipmentName} - {problemSummary}",
                            assignedManager,
                            BuildMetricDetailUrl(vehicle.Id, report.Id, null));
                    }

                    var serialAlert = BuildSerialRegisterAlert(vehicle, check, equipmentBySerial, assignedEquipmentPairs);
                    if (!string.IsNullOrWhiteSpace(serialAlert))
                    {
                        AddEquipmentAlertRow(
                            rows,
                            dismissedAlertKeys,
                            vehicle.Id,
                            report.Id,
                            null,
                            $"serial-register:{vehicle.Id}:{report.Id}:{check.Id}:{NormalizeAssetId(check.SerialOrAssetId)}",
                            false,
                            callsign,
                            serialAlert,
                            assignedManager,
                            BuildMetricDetailUrl(vehicle.Id, report.Id, null));
                    }
                }
            }

            foreach (var issue in openIssues.Where(issue => MatchesIssue(issue, vehicle.RegistrationNumber) || MatchesIssue(issue, vehicle.Callsign)))
            {
                AddEquipmentAlertRow(
                    rows,
                    dismissedAlertKeys,
                    vehicle.Id,
                    null,
                    issue.Id,
                    $"equipment-issue:{vehicle.Id}:{issue.Id}",
                    false,
                    callsign,
                    $"Reported issue about equipment: {BuildIssueAlertText(issue)}",
                    assignedManager,
                    BuildMetricDetailUrl(vehicle.Id, null, issue.Id));
            }

            foreach (var alert in varianceAlerts.Where(alert => alert.VehicleId == vehicle.Id))
            {
                AddEquipmentAlertRow(
                    rows,
                    dismissedAlertKeys,
                    vehicle.Id,
                    alert.DailyVehicleReadinessReportId,
                    null,
                    $"variance:{vehicle.Id}:{alert.Id}",
                    false,
                    callsign,
                    $"S/N or register variance: {BuildVarianceAlertText(alert)}",
                    assignedManager,
                    BuildMetricDetailUrl(vehicle.Id, alert.DailyVehicleReadinessReportId, null));
            }
        }

        return rows;
    }

    private async Task<List<ReadinessMetricRow>> LoadOpenIssueVariablesAsync(AppUser currentUser)
    {
        var isSeniorOverview = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        var allowedAreaIds = await LoadAllowedAreaIdsAsync(currentUser, isSeniorOverview);
        if (!isSeniorOverview && allowedAreaIds.Count == 0)
        {
            return [];
        }

        var vehicleQuery = _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle => vehicle.CompanyId == currentUser.CompanyId && vehicle.Status != "Deleted");

        if (OperationalAreaId.HasValue)
        {
            vehicleQuery = vehicleQuery.Where(vehicle => vehicle.CurrentOperationalAreaId == OperationalAreaId.Value);
        }
        else if (!isSeniorOverview)
        {
            vehicleQuery = vehicleQuery.Where(vehicle =>
                vehicle.CurrentOperationalAreaId.HasValue &&
                allowedAreaIds.Contains(vehicle.CurrentOperationalAreaId.Value));
        }

        var vehicles = await vehicleQuery
            .OrderBy(vehicle => vehicle.CurrentOperationalArea == null ? "Unallocated" : vehicle.CurrentOperationalArea.Name)
            .ThenBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync();

        var areaNames = vehicles
            .Select(vehicle => vehicle.CurrentOperationalArea?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var vehicleLabels = vehicles
            .SelectMany(vehicle => new[] { vehicle.RegistrationNumber, vehicle.Callsign })
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var issues = await _db.IssueReports
            .AsNoTracking()
            .Include(issue => issue.AssignedToUser)
            .Where(issue => issue.CompanyId == currentUser.CompanyId && issue.Status == "Open")
            .OrderByDescending(issue => issue.CreatedAtUtc)
            .ToListAsync();

        if (!isSeniorOverview || OperationalAreaId.HasValue)
        {
            issues = issues
                .Where(issue =>
                    issue.AssignedToUserId == currentUser.Id ||
                    areaNames.Any(areaName => MatchesText(issue.Location, areaName)) ||
                    vehicleLabels.Any(label => MatchesIssue(issue, label)))
                .ToList();
        }

        return issues
            .Select(issue =>
            {
                var matchedVehicle = vehicles.FirstOrDefault(vehicle =>
                    MatchesIssue(issue, vehicle.RegistrationNumber) ||
                    MatchesIssue(issue, vehicle.Callsign));

                return new ReadinessMetricRow
                {
                    IsReady = false,
                    VehicleId = matchedVehicle?.Id ?? 0,
                    IssueId = issue.Id,
                    Callsign = BuildIssueSourceLabel(issue, matchedVehicle),
                    Discrepancy = BuildOpenIssueSummary(issue),
                    AssignedManager = issue.AssignedToUser?.FullName ?? "No manager assigned",
                    TargetUrl = $"/IssueReportAction?issueId={issue.Id}&pool=true"
                };
            })
            .ToList();
    }

    private async Task DeleteIssueSourceAsync(AppUser currentUser, int issueId)
    {
        var issue = await _db.IssueReports
            .FirstOrDefaultAsync(report =>
                report.Id == issueId &&
                report.CompanyId == currentUser.CompanyId &&
                report.Status != "Deleted");

        if (issue is null)
        {
            return;
        }

        if (!await CanAccessIssueAsync(currentUser, issue))
        {
            return;
        }

        var now = DateTime.UtcNow;
        issue.Status = "Deleted";

        _db.IssueReportEvents.Add(new IssueReportEvent
        {
            IssueReportId = issue.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Deleted from readiness metric",
            Notes = "Issue source removed from the readiness metric list.",
            CreatedAtUtc = now
        });
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Readiness metric source deleted",
            EntityType = "IssueReport",
            EntityId = issue.Id,
            Details = $"Issue source deleted from {MetricHeading}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
    }

    private async Task DeleteChecklistSourceAsync(AppUser currentUser, int reportId)
    {
        var report = await _db.DailyVehicleReadinessReports
            .FirstOrDefaultAsync(item =>
                item.Id == reportId &&
                item.CompanyId == currentUser.CompanyId &&
                item.WorkflowStatus != "Deleted");

        if (report is null)
        {
            return;
        }

        if (!await CanAccessVehicleIdAsync(currentUser, report.VehicleId))
        {
            return;
        }

        var now = DateTime.UtcNow;
        report.WorkflowStatus = "Deleted";
        report.UpdatedAtUtc = now;

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Readiness metric source deleted",
            EntityType = "DailyVehicleReadinessReport",
            EntityId = report.Id,
            Details = $"Checklist source deleted from {MetricHeading}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
    }

    private async Task DismissVehicleMetricRowAsync(AppUser currentUser, string? metric, int vehicleId)
    {
        if (!await CanAccessVehicleIdAsync(currentUser, vehicleId))
        {
            return;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Readiness metric row deleted",
            EntityType = "ReadinessMetricRow",
            EntityId = vehicleId,
            Details = $"Metric: {metric ?? "unknown"}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    private async Task DismissMetricAlertRowAsync(AppUser currentUser, string? metric, int vehicleId, string alertKey)
    {
        if (vehicleId > 0)
        {
            if (!await CanAccessVehicleIdAsync(currentUser, vehicleId))
            {
                return;
            }
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Readiness metric alert deleted",
            EntityType = "ReadinessMetricAlert",
            EntityId = vehicleId > 0 ? vehicleId : null,
            Details = BuildMetricAlertDismissalDetails(metric, alertKey),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    private static bool CanViewReadinessMetrics(AppUser user)
    {
        return string.Equals(user.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase) ||
            CurrentUserService.IsSeniorAccessRole(user.AppRole?.Name);
    }

    private async Task<bool> CanAccessVehicleIdAsync(AppUser currentUser, int vehicleId)
    {
        if (vehicleId <= 0)
        {
            return false;
        }

        if (CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return await _db.Vehicles
                .AsNoTracking()
                .AnyAsync(vehicle => vehicle.Id == vehicleId && vehicle.CompanyId == currentUser.CompanyId);
        }

        var areaId = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.Id == vehicleId && vehicle.CompanyId == currentUser.CompanyId)
            .Select(vehicle => vehicle.CurrentOperationalAreaId)
            .FirstOrDefaultAsync();

        if (!areaId.HasValue)
        {
            return false;
        }

        return await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .AnyAsync(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.OperationalAreaId == areaId.Value &&
                assignment.Status == "Active");
    }

    private async Task<bool> CanAccessIssueAsync(AppUser currentUser, IssueReport issue)
    {
        if (CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return true;
        }

        if (issue.AssignedToUserId == currentUser.Id)
        {
            return true;
        }

        var assignedAreaIds = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active")
            .Select(assignment => assignment.OperationalAreaId)
            .ToListAsync();

        if (assignedAreaIds.Count == 0)
        {
            return false;
        }

        var areaNames = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == currentUser.CompanyId && assignedAreaIds.Contains(area.Id))
            .Select(area => area.Name)
            .ToListAsync();

        if (areaNames.Any(areaName => MatchesText(issue.Location, areaName)))
        {
            return true;
        }

        var scopedVehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle =>
                vehicle.CompanyId == currentUser.CompanyId &&
                vehicle.CurrentOperationalAreaId.HasValue &&
                assignedAreaIds.Contains(vehicle.CurrentOperationalAreaId.Value))
            .ToListAsync();

        var vehicleLabels = scopedVehicles
            .SelectMany(vehicle => new[] { vehicle.RegistrationNumber, vehicle.Callsign })
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .ToList();

        return vehicleLabels.Any(label => MatchesIssue(issue, label));
    }

    private async Task<HashSet<int>> LoadDismissedVehicleIdsAsync(int companyId, string? metric, DateTime shiftStartUtc)
    {
        var metricLabel = $"Metric: {metric ?? "unknown"}";

        var ids = await _db.AuditLogs
            .AsNoTracking()
            .Where(log =>
                log.CompanyId == companyId &&
                log.Action == "Readiness metric row deleted" &&
                log.EntityType == "ReadinessMetricRow" &&
                log.EntityId.HasValue &&
                log.CreatedAtUtc >= shiftStartUtc &&
                log.Details == metricLabel)
            .Select(log => log.EntityId!.Value)
            .ToListAsync();

        return ids.ToHashSet();
    }

    private async Task<HashSet<string>> LoadDismissedAlertKeysAsync(int companyId, string? metric, DateTime shiftStartUtc)
    {
        var prefix = $"Metric: {metric ?? "unknown"}; Key: ";

        var details = await _db.AuditLogs
            .AsNoTracking()
            .Where(log =>
                log.CompanyId == companyId &&
                log.Action == "Readiness metric alert deleted" &&
                log.EntityType == "ReadinessMetricAlert" &&
                log.CreatedAtUtc >= shiftStartUtc &&
                log.Details != null &&
                log.Details.StartsWith(prefix))
            .Select(log => log.Details!)
            .ToListAsync();

        return details
            .Select(detail => detail[prefix.Length..])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<int>> LoadAllowedAreaIdsAsync(AppUser currentUser, bool isSeniorOverview)
    {
        var query = _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == currentUser.CompanyId && area.Status == "Active");

        if (isSeniorOverview)
        {
            var seniorAreaIds = await query.Select(area => area.Id).ToListAsync();
            if (OperationalAreaId.HasValue && !seniorAreaIds.Contains(OperationalAreaId.Value))
            {
                OperationalAreaId = null;
            }

            return seniorAreaIds;
        }

        var managerAreaIds = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active")
            .Select(assignment => assignment.OperationalAreaId)
            .ToListAsync();

        if (OperationalAreaId.HasValue && !managerAreaIds.Contains(OperationalAreaId.Value))
        {
            OperationalAreaId = null;
        }

        return managerAreaIds;
    }

    private async Task<Dictionary<int, string>> LoadManagersByAreaAsync(int companyId)
    {
        var assignments = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Include(assignment => assignment.ManagerUser)
            .Where(assignment =>
                assignment.CompanyId == companyId &&
                assignment.Status == "Active" &&
                assignment.ManagerUser != null &&
                assignment.ManagerUser.Status == "Active")
            .OrderBy(assignment => assignment.ManagerUser!.FullName)
            .Select(assignment => new
            {
                assignment.OperationalAreaId,
                ManagerName = assignment.ManagerUser!.FullName
            })
            .ToListAsync();

        return assignments
            .GroupBy(assignment => assignment.OperationalAreaId)
            .ToDictionary(
                group => group.Key,
                group => string.Join(", ", group.Select(assignment => assignment.ManagerName).Distinct()));
    }

    private static VehicleMetricRow BuildVehicleMetricRow(
        Vehicle vehicle,
        DailyVehicleReadinessReport? report,
        List<IssueReport> openIssues,
        Dictionary<int, string> managersByArea)
    {
        var matchingIssue = openIssues.FirstOrDefault(issue =>
            MatchesIssue(issue, vehicle.RegistrationNumber) ||
            MatchesIssue(issue, vehicle.Callsign));

        var issueParts = new List<string>();
        var checkedThisShift = report is not null;
        var vehicleUnavailable = IsUnavailable(vehicle.Status);
        var reportUnavailable = report is not null &&
            (IsUnavailable(report.ReadinessStatus) || report.CriticalIssueCount > 0);
        var equipmentIssue = report is not null && HasEquipmentIssue(report);
        var warningCount = report?.WarningIssueCount ?? 0;
        var openIssueCount = openIssues.Count(issue =>
            MatchesIssue(issue, vehicle.RegistrationNumber) ||
            MatchesIssue(issue, vehicle.Callsign));

        if (!checkedThisShift)
        {
            issueParts.Add("Check missing this shift");
        }
        if (vehicleUnavailable)
        {
            issueParts.Add($"Vehicle status: {vehicle.Status}");
        }
        if (reportUnavailable)
        {
            issueParts.Add("Critical checklist issue captured");
        }
        if (equipmentIssue)
        {
            issueParts.Add("Equipment discrepancy captured");
        }
        if (warningCount > 0)
        {
            issueParts.Add($"{warningCount} checklist warning{(warningCount == 1 ? string.Empty : "s")}");
        }
        if (openIssueCount > 0)
        {
            issueParts.Add($"{openIssueCount} open issue{(openIssueCount == 1 ? string.Empty : "s")}");
        }

        var critical = !checkedThisShift || vehicleUnavailable || reportUnavailable;
        var warning = !critical && (warningCount > 0 || equipmentIssue || openIssueCount > 0 || !IsOperational(report?.ReadinessStatus));
        var areaId = vehicle.CurrentOperationalAreaId;
        var targetUrl = matchingIssue is not null
            ? $"/ReadinessMetricDetail?metric=ready-vehicles&vehicleId={vehicle.Id}&issueId={matchingIssue.Id}"
            : report is not null
                ? $"/ReadinessMetricDetail?metric=ready-vehicles&vehicleId={vehicle.Id}&reportId={report.Id}"
                : $"/ReadinessMetricDetail?metric=ready-vehicles&vehicleId={vehicle.Id}";

        return new VehicleMetricRow
        {
            IsReady = !critical && !warning,
            VehicleId = vehicle.Id,
            ReportId = report?.Id,
            IssueId = matchingIssue?.Id,
            Callsign = string.IsNullOrWhiteSpace(vehicle.Callsign) ? vehicle.RegistrationNumber : vehicle.Callsign,
            Discrepancy = issueParts.Count == 0 ? "No discrepancy collected" : string.Join("; ", issueParts),
            AssignedManager = areaId.HasValue && managersByArea.TryGetValue(areaId.Value, out var managerNames)
                ? managerNames
                : "No manager assigned",
            TargetUrl = targetUrl
        };
    }

    private static bool HasEquipmentIssue(DailyVehicleReadinessReport report)
    {
        if (!report.EquipmentChecks.Any())
        {
            return true;
        }

        return report.EquipmentChecks.Any(check =>
            !check.IsOperational ||
            IsProblemStatus(check.PresentStatus, "Present") ||
            IsProblemStatus(check.BatteryStatus, "Full", "Acceptable", "Charging", "Not applicable", "N/A") ||
            IsProblemStatus(check.DamageStatus, "No damage", "None", "Good", "Not applicable", "N/A") ||
            IsProblemStatus(check.ReadinessImpact, "None"));
    }

    private static bool IsReadinessCheckComplete(DailyVehicleReadinessReport report)
    {
        return !string.Equals(report.WorkflowStatus, "Draft", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(report.LastSavedSection, "Equipment", StringComparison.OrdinalIgnoreCase) &&
            report.EquipmentChecks.Any();
    }

    private static CompletedCheckStatusSummary BuildCompletedCheckStatusSummary(DailyVehicleReadinessReport report)
    {
        var issueParts = new List<string>();
        var hasCriticalIssue = IsUnavailable(report.ReadinessStatus) || report.CriticalIssueCount > 0;
        var hasEquipmentIssue = HasEquipmentIssue(report);

        if (hasCriticalIssue)
        {
            issueParts.Add("critical issue captured");
        }
        if (hasEquipmentIssue)
        {
            issueParts.Add("equipment discrepancy captured");
        }
        if (report.WarningIssueCount > 0)
        {
            issueParts.Add($"{report.WarningIssueCount} warning{(report.WarningIssueCount == 1 ? string.Empty : "s")}");
        }
        if (!IsOperational(report.ReadinessStatus) && issueParts.Count == 0)
        {
            issueParts.Add(report.ReadinessStatus);
        }

        return new CompletedCheckStatusSummary(
            IsReady: issueParts.Count == 0,
            Summary: issueParts.Count == 0
                ? "daily vehicle readiness check completed with no discrepancy captured"
                : $"daily vehicle readiness check completed; {string.Join("; ", issueParts)}");
    }

    private static void AddEquipmentAlertRow(
        List<ReadinessMetricRow> rows,
        HashSet<string> dismissedAlertKeys,
        int vehicleId,
        int? reportId,
        int? issueId,
        string alertKey,
        bool isReady,
        string callsign,
        string alertText,
        string assignedManager,
        string targetUrl)
    {
        if (dismissedAlertKeys.Contains(alertKey))
        {
            return;
        }

        rows.Add(new ReadinessMetricRow
        {
            IsReady = isReady,
            VehicleId = vehicleId,
            ReportId = reportId,
            IssueId = issueId,
            AlertKey = alertKey,
            Callsign = callsign,
            Discrepancy = alertText,
            AssignedManager = assignedManager,
            TargetUrl = targetUrl
        });
    }

    private static string BuildMetricDetailUrl(int vehicleId, int? reportId, int? issueId)
    {
        if (issueId.HasValue)
        {
            return $"/ReadinessMetricDetail?metric=equipment-alerts&vehicleId={vehicleId}&issueId={issueId.Value}";
        }

        return reportId.HasValue
            ? $"/ReadinessMetricDetail?metric=equipment-alerts&vehicleId={vehicleId}&reportId={reportId.Value}"
            : $"/ReadinessMetricDetail?metric=equipment-alerts&vehicleId={vehicleId}";
    }

    private static string BuildEquipmentCheckAlertSummary(DailyVehicleEquipmentCheck check)
    {
        var parts = new List<string>();

        if (!check.IsOperational)
        {
            parts.Add("marked not operational");
        }
        if (IsProblemStatus(check.PresentStatus, "Present"))
        {
            parts.Add($"present status: {check.PresentStatus}");
        }
        if (IsProblemStatus(check.BatteryStatus, "Full", "Acceptable", "Charging", "Not applicable", "N/A"))
        {
            parts.Add($"battery: {check.BatteryStatus}");
        }
        if (IsProblemStatus(check.DamageStatus, "No damage", "None", "Good", "Not applicable", "N/A"))
        {
            parts.Add($"damage: {check.DamageStatus}");
        }
        if (IsProblemStatus(check.ReadinessImpact, "None"))
        {
            parts.Add($"readiness impact: {check.ReadinessImpact}");
        }
        if (!string.IsNullOrWhiteSpace(check.IssueNotes))
        {
            parts.Add($"note: {Truncate(check.IssueNotes, 90)}");
        }

        return string.Join("; ", parts);
    }

    private static string? BuildSerialRegisterAlert(
        Vehicle vehicle,
        DailyVehicleEquipmentCheck check,
        Dictionary<string, EquipmentItem> equipmentBySerial,
        HashSet<string> assignedEquipmentPairs)
    {
        var serialKey = NormalizeAssetId(check.SerialOrAssetId);
        if (string.IsNullOrWhiteSpace(serialKey) || IsNotApplicableValue(serialKey))
        {
            return null;
        }

        if (!equipmentBySerial.TryGetValue(serialKey, out var registeredEquipment))
        {
            return $"S/N captured but not approved: {check.SerialOrAssetId} is not in the equipment register";
        }

        if (vehicle.CurrentOperationalAreaId.HasValue &&
            registeredEquipment.CurrentOperationalAreaId.HasValue &&
            registeredEquipment.CurrentOperationalAreaId.Value != vehicle.CurrentOperationalAreaId.Value)
        {
            return $"S/N location mismatch: {check.SerialOrAssetId} is registered to a different base or area";
        }

        var expectedPair = $"{vehicle.Id}:{registeredEquipment.Id}";
        if (!assignedEquipmentPairs.Contains(expectedPair))
        {
            return $"S/N does not correspond with vehicle assignment: {check.SerialOrAssetId} is registered but not assigned to this vehicle";
        }

        return null;
    }

    private static bool IsEquipmentIssueReport(IssueReport issue)
    {
        return MatchesText(issue.Module, "equipment") ||
            MatchesText(issue.IssueType, "equipment") ||
            MatchesText(issue.RelatedItem, "equipment") ||
            MatchesText(issue.Description, "equipment");
    }

    private static string BuildIssueAlertText(IssueReport issue)
    {
        var label = string.IsNullOrWhiteSpace(issue.IssueType) ? issue.Module : issue.IssueType;
        var detail = string.IsNullOrWhiteSpace(issue.RelatedItem)
            ? issue.Description
            : $"{issue.RelatedItem} - {issue.Description}";

        return $"{label}: {Truncate(detail, 120)}";
    }

    private static string BuildVarianceAlertText(ChecklistVarianceAlert alert)
    {
        var label = string.IsNullOrWhiteSpace(alert.AssetLabel) ? "Equipment item" : alert.AssetLabel;
        var field = string.IsNullOrWhiteSpace(alert.FieldKey) ? "registered value" : alert.FieldKey;
        var newValue = string.IsNullOrWhiteSpace(alert.NewValue) ? "N/A" : alert.NewValue;
        var registerValue = string.IsNullOrWhiteSpace(alert.RegisterValue) ? "not in register" : alert.RegisterValue;

        return $"{label} {field} changed to {newValue}; register value: {registerValue}";
    }

    private static string BuildIssueSourceLabel(IssueReport issue, Vehicle? matchedVehicle)
    {
        if (matchedVehicle is not null)
        {
            return string.IsNullOrWhiteSpace(matchedVehicle.Callsign)
                ? matchedVehicle.RegistrationNumber
                : $"{matchedVehicle.Callsign} / {matchedVehicle.RegistrationNumber}";
        }

        if (!string.IsNullOrWhiteSpace(issue.RelatedItem))
        {
            return issue.RelatedItem;
        }

        if (!string.IsNullOrWhiteSpace(issue.Location))
        {
            return issue.Location;
        }

        return string.IsNullOrWhiteSpace(issue.Module) ? "General issue" : issue.Module;
    }

    private static string BuildOpenIssueSummary(IssueReport issue)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(issue.Module))
        {
            parts.Add(issue.Module);
        }
        if (!string.IsNullOrWhiteSpace(issue.IssueType))
        {
            parts.Add(issue.IssueType);
        }
        if (!string.IsNullOrWhiteSpace(issue.Severity))
        {
            parts.Add(issue.Severity);
        }
        if (!string.IsNullOrWhiteSpace(issue.OperationalStatus))
        {
            parts.Add(issue.OperationalStatus);
        }

        var prefix = parts.Count == 0 ? "Open issue" : string.Join(" / ", parts);
        return $"{prefix}: {Truncate(issue.Description, 140)}";
    }

    private static string BuildMetricAlertDismissalDetails(string? metric, string alertKey)
    {
        return $"Metric: {metric ?? "unknown"}; Key: {alertKey}";
    }

    private static string NormalizeAssetId(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private static bool IsNotApplicableValue(string value)
    {
        return string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "NA", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "NOT APPLICABLE", StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength].TrimEnd() + "...";
    }

    private static bool IsProblemStatus(string? value, params string[] acceptedValues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return !acceptedValues.Any(accepted => string.Equals(value, accepted, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsOperational(string? value)
    {
        return string.Equals(value, "Operational", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnavailable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("out of service", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("inactive", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesIssue(IssueReport issue, string? needle)
    {
        return MatchesText(issue.RelatedItem, needle) ||
            MatchesText(issue.Location, needle) ||
            MatchesText(issue.Description, needle);
    }

    private static bool MatchesText(string? haystack, string? needle)
    {
        return !string.IsNullOrWhiteSpace(haystack) &&
            !string.IsNullOrWhiteSpace(needle) &&
            haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class VehicleMetricRow
    {
        public bool IsReady { get; set; }
        public int VehicleId { get; set; }
        public int? ReportId { get; set; }
        public int? IssueId { get; set; }
        public string AlertKey { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string Discrepancy { get; set; } = string.Empty;
        public string AssignedManager { get; set; } = string.Empty;
        public string TargetUrl { get; set; } = string.Empty;
    }

    private sealed record CompletedCheckStatusSummary(bool IsReady, string Summary);

    public sealed class ReadinessMetricRow
    {
        public bool IsReady { get; set; }
        public int VehicleId { get; set; }
        public int? ReportId { get; set; }
        public int? IssueId { get; set; }
        public string AlertKey { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string Discrepancy { get; set; } = string.Empty;
        public string AssignedManager { get; set; } = string.Empty;
        public string TargetUrl { get; set; } = string.Empty;
    }
}
