using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class IssueReportActionModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public IssueReportActionModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public int? IssueId { get; set; }
    [BindProperty(SupportsGet = true)] public bool Pool { get; set; }
    [BindProperty] public string? ResolutionOutcome { get; set; }
    [BindProperty] public string? ActionTaken { get; set; }

    public IssueActionDetails? Issue { get; private set; }
    public List<IssueEventDetails> Events { get; private set; } = new();
    public bool CanResolve { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadIssueAsync(currentUser);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadIssueAsync(currentUser);
        if (Issue is null)
        {
            return RedirectToPage("/IssueInbox", new { confirmation = "issue-not-found" });
        }

        if (!CanResolve)
        {
            StatusMessage = "Only the assigned manager or senior management can resolve this issue.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(ResolutionOutcome))
        {
            StatusMessage = "Select the resolution outcome before closing this issue.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(ActionTaken))
        {
            StatusMessage = "Enter the action taken before closing this issue.";
            return Page();
        }

        var issueId = IssueId.GetValueOrDefault();
        var issue = await _db.IssueReports
            .FirstAsync(report => report.Id == issueId && report.Status == "Open");
        var now = DateTime.UtcNow;

        issue.Status = "Resolved";
        issue.ResolvedByUserId = currentUser.Id;
        issue.ResolvedAtUtc = now;
        issue.ResolutionOutcome = ResolutionOutcome.Trim();
        issue.ActionTaken = ActionTaken.Trim();

        _db.IssueReportEvents.Add(new IssueReportEvent
        {
            IssueReportId = issue.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Resolved",
            Notes = $"Outcome: {issue.ResolutionOutcome}. Action taken: {issue.ActionTaken}",
            CreatedAtUtc = now
        });
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = issue.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Issue resolved",
            EntityType = "IssueReport",
            EntityId = issue.Id,
            Details = $"Issue #{issue.Id} resolved with outcome: {issue.ResolutionOutcome}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        return RedirectToPage("/IssueInbox", new { confirmation = "issue-resolved" });
    }

    private async Task LoadIssueAsync(AppUser currentUser)
    {
        if (!IssueId.HasValue)
        {
            return;
        }

        var isSenior = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);

        var issue = await _db.IssueReports
            .Include(report => report.ReportedByUser)
            .Include(report => report.AssignedToUser)
            .Include(report => report.Events)
                .ThenInclude(issueEvent => issueEvent.PerformedByUser)
            .Where(report => report.Id == IssueId.Value && report.CompanyId == currentUser.CompanyId)
            .FirstOrDefaultAsync();

        if (issue is null)
        {
            return;
        }

        if (!isSenior && issue.AssignedToUserId != currentUser.Id)
        {
            var issueScope = await LoadAssignedIssueScopeAsync(currentUser);
            if (!IssueMatchesAreaScope(issue, issueScope))
            {
                return;
            }
        }

        CanResolve = issue.Status == "Open" && (issue.AssignedToUserId == currentUser.Id || isSenior);
        Issue = new IssueActionDetails
        {
            Id = issue.Id,
            Status = issue.Status,
            Module = issue.Module,
            IssueType = issue.IssueType,
            RelatedItem = issue.RelatedItem,
            Location = issue.Location,
            Severity = issue.Severity,
            OperationalStatus = issue.OperationalStatus,
            Description = issue.Description,
            NotificationMethod = issue.NotificationMethod,
            EvidenceFileNames = issue.EvidenceFileNames,
            ReportedByName = issue.ReportedByUser?.FullName ?? "Reporter",
            AssignedToName = issue.AssignedToUser?.FullName ?? "Manager",
            CreatedAtUtc = issue.CreatedAtUtc,
            ResolvedAtUtc = issue.ResolvedAtUtc,
            ResolutionOutcome = issue.ResolutionOutcome,
            ActionTaken = issue.ActionTaken
        };
        Events = issue.Events
            .OrderByDescending(issueEvent => issueEvent.CreatedAtUtc)
            .Select(issueEvent => new IssueEventDetails
            {
                EventType = issueEvent.EventType,
                PerformedByName = issueEvent.PerformedByUser?.FullName ?? "User",
                Notes = issueEvent.Notes,
                CreatedAtUtc = issueEvent.CreatedAtUtc
            })
            .ToList();
    }

    private async Task<IssueAreaScope> LoadAssignedIssueScopeAsync(AppUser currentUser)
    {
        var areaIds = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active")
            .Select(assignment => assignment.OperationalAreaId)
            .ToListAsync();

        if (currentUser.AssignedOperationalAreaId.HasValue &&
            !areaIds.Contains(currentUser.AssignedOperationalAreaId.Value))
        {
            areaIds.Add(currentUser.AssignedOperationalAreaId.Value);
        }

        var areaNames = areaIds.Count == 0
            ? []
            : await _db.OperationalAreas
                .AsNoTracking()
                .Where(area =>
                    area.CompanyId == currentUser.CompanyId &&
                    areaIds.Contains(area.Id) &&
                    area.Status == "Active")
                .Select(area => area.Name)
                .ToListAsync();

        var vehicleLabels = areaIds.Count == 0
            ? []
            : await _db.Vehicles
                .AsNoTracking()
                .Where(vehicle =>
                    vehicle.CompanyId == currentUser.CompanyId &&
                    vehicle.CurrentOperationalAreaId.HasValue &&
                    areaIds.Contains(vehicle.CurrentOperationalAreaId.Value) &&
                    vehicle.Status != "Deleted")
                .SelectMany(vehicle => new[] { vehicle.RegistrationNumber, vehicle.Callsign })
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct()
                .ToListAsync();

        return new IssueAreaScope(areaNames, vehicleLabels);
    }

    private static bool IssueMatchesAreaScope(IssueReport issue, IssueAreaScope scope)
    {
        return scope.AreaNames.Any(areaName => MatchesText(issue.Location, areaName)) ||
            scope.VehicleLabels.Any(vehicleLabel => MatchesText(issue.Location, vehicleLabel) || MatchesText(issue.RelatedItem, vehicleLabel));
    }

    private static bool MatchesText(string? value, string? search)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            !string.IsNullOrWhiteSpace(search) &&
            value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record IssueAreaScope(IReadOnlyList<string> AreaNames, IReadOnlyList<string> VehicleLabels);

    public class IssueActionDetails
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string? RelatedItem { get; set; }
        public string? Location { get; set; }
        public string? Severity { get; set; }
        public string? OperationalStatus { get; set; }
        public string Description { get; set; } = string.Empty;
        public string NotificationMethod { get; set; } = string.Empty;
        public string? EvidenceFileNames { get; set; }
        public string ReportedByName { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ResolvedAtUtc { get; set; }
        public string? ResolutionOutcome { get; set; }
        public string? ActionTaken { get; set; }
    }

    public class IssueEventDetails
    {
        public string EventType { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
