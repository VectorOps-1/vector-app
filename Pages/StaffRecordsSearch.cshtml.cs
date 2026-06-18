using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class StaffRecordsSearchModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public StaffRecordsSearchModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public int? StaffUserId { get; set; }
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }

    public StaffProfileItem? SelectedStaff { get; private set; }
    public List<StaffSearchResult> SearchResults { get; private set; } = new();
    public List<FileCategorySummary> FileCategories { get; private set; } = new();
    public StaffRecordSummary RecordSummary { get; private set; } = new();
    public string ScopeLabel { get; private set; } = "Staff profile access";
    public string? StatusMessage { get; private set; }
    public int? CurrentStaffUserId { get; private set; }
    public bool SelectedIsCurrentUser => SelectedStaff is not null && CurrentStaffUserId == SelectedStaff.Id;

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        CurrentStaffUserId = currentUser.Id;
        var scopedStaff = await BuildScopedStaffQueryAsync(currentUser);

        if (StaffUserId.HasValue)
        {
            SelectedStaff = await ProjectStaffProfile(scopedStaff)
                .FirstOrDefaultAsync(staff => staff.Id == StaffUserId.Value);

            if (SelectedStaff is null)
            {
                StatusMessage = "That staff profile is not available in your current access scope.";
                return Page();
            }

            await LoadLinkedRecordDataAsync(currentUser.CompanyId, SelectedStaff.Id);
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            SearchResults = await ProjectStaffSearch(scopedStaff)
                .Where(staff =>
                    staff.FullName.Contains(search) ||
                    staff.Email.Contains(search) ||
                    (staff.StaffIdentifier != null && staff.StaffIdentifier.Contains(search)) ||
                    (staff.NationalId != null && staff.NationalId.Contains(search)) ||
                    (staff.CellNumber != null && staff.CellNumber.Contains(search)) ||
                    (staff.QualificationFunction != null && staff.QualificationFunction.Contains(search)) ||
                    (staff.PractitionerNumber != null && staff.PractitionerNumber.Contains(search)) ||
                    (staff.CpdComplianceStatus != null && staff.CpdComplianceStatus.Contains(search)) ||
                    staff.RoleName.Contains(search) ||
                    staff.AssignedArea.Contains(search))
                .OrderBy(staff => staff.FullName)
                .Take(30)
                .ToListAsync();

            if (SearchResults.Count == 1)
            {
                return RedirectToPage("/StaffRecordsSearch", new { staffUserId = SearchResults[0].Id });
            }

            StatusMessage = SearchResults.Count == 0
                ? "No staff records matched that search."
                : "Select a staff profile to open the full record.";

            return Page();
        }

        StatusMessage = "Open a staff profile from the staff register, or search for a staff member.";
        return Page();
    }

    private async Task<IQueryable<AppUser>> BuildScopedStaffQueryAsync(AppUser currentUser)
    {
        var query = _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Include(user => user.AssignedOperationalArea)
            .Where(user => user.CompanyId == currentUser.CompanyId && user.Status != "Deleted");

        if (CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            ScopeLabel = "Senior management: all staff and managers";
            return query;
        }

        var assignedAreaIds = await LoadAssignedAreaIdsAsync(currentUser);
        ScopeLabel = assignedAreaIds.Count == 0
            ? "Operational management: no assigned staff area"
            : "Operational management: staff linked to your assigned area(s)";

        return assignedAreaIds.Count == 0
            ? query.Where(user => user.Id == currentUser.Id)
            : query.Where(user =>
                user.Id == currentUser.Id ||
                (user.AssignedOperationalAreaId.HasValue && assignedAreaIds.Contains(user.AssignedOperationalAreaId.Value)));
    }

    private async Task<List<int>> LoadAssignedAreaIdsAsync(AppUser currentUser)
    {
        var assignedAreaIds = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active")
            .Select(assignment => assignment.OperationalAreaId)
            .ToListAsync();

        if (currentUser.AssignedOperationalAreaId.HasValue &&
            !assignedAreaIds.Contains(currentUser.AssignedOperationalAreaId.Value))
        {
            assignedAreaIds.Add(currentUser.AssignedOperationalAreaId.Value);
        }

        return assignedAreaIds;
    }

    private async Task LoadLinkedRecordDataAsync(int companyId, int staffUserId)
    {
        FileCategories = await _db.AssetFiles
            .AsNoTracking()
            .Where(file =>
                file.CompanyId == companyId &&
                file.LinkedEntityType == "Staff" &&
                file.LinkedEntityId == staffUserId)
            .GroupBy(file => file.Category)
            .Select(group => new FileCategorySummary
            {
                Category = group.Key,
                FileCount = group.Count(),
                LatestUploadAtUtc = group.Max(file => file.UploadedAtUtc)
            })
            .OrderBy(summary => summary.Category)
            .ToListAsync();

        RecordSummary = new StaffRecordSummary
        {
            FileCount = FileCategories.Sum(category => category.FileCount),
            SubmittedChecks = await _db.DailyVehicleReadinessReports.CountAsync(report =>
                report.CompanyId == companyId &&
                report.PerformedByUserId == staffUserId &&
                (report.WorkflowStatus == "Saved" || report.WorkflowStatus == "Submitted")),
            DraftChecks = await _db.DailyVehicleReadinessReports.CountAsync(report =>
                report.CompanyId == companyId &&
                report.PerformedByUserId == staffUserId &&
                report.WorkflowStatus == "Draft"),
            OpenTasks = await _db.TaskItems.CountAsync(task =>
                task.CompanyId == companyId &&
                task.AssignedToUserId == staffUserId &&
                task.Status == "Open"),
            CompletedTasks = await _db.TaskItems.CountAsync(task =>
                task.CompanyId == companyId &&
                task.AssignedToUserId == staffUserId &&
                (task.Status == "Completed" || task.CompletedAtUtc.HasValue)),
            ReportedIssues = await _db.IssueReports.CountAsync(issue =>
                issue.CompanyId == companyId &&
                issue.ReportedByUserId == staffUserId &&
                issue.Status != "Deleted"),
            AssignedIssues = await _db.IssueReports.CountAsync(issue =>
                issue.CompanyId == companyId &&
                issue.AssignedToUserId == staffUserId &&
                issue.Status != "Deleted")
        };
    }

    private static IQueryable<StaffProfileItem> ProjectStaffProfile(IQueryable<AppUser> query)
    {
        return query.Select(user => new StaffProfileItem
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            StaffIdentifier = user.StaffIdentifier,
            NationalId = user.NationalId,
            CellNumber = user.CellNumber,
            QualificationFunction = user.QualificationFunction,
            PractitionerNumber = user.PractitionerNumber,
            AnnualLicenseExpiryDate = user.AnnualLicenseExpiryDate,
            CpdComplianceStatus = user.CpdComplianceStatus,
            CpdComplianceExpiryDate = user.CpdComplianceExpiryDate,
            AssignedArea = user.AssignedOperationalArea == null ? "Unassigned" : user.AssignedOperationalArea.Name,
            RoleName = user.AppRole == null ? "Unassigned" : user.AppRole.Name,
            Status = user.Status,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc
        });
    }

    private static IQueryable<StaffSearchResult> ProjectStaffSearch(IQueryable<AppUser> query)
    {
        return query.Select(user => new StaffSearchResult
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            StaffIdentifier = user.StaffIdentifier,
            NationalId = user.NationalId,
            CellNumber = user.CellNumber,
            QualificationFunction = user.QualificationFunction,
            PractitionerNumber = user.PractitionerNumber,
            AnnualLicenseExpiryDate = user.AnnualLicenseExpiryDate,
            CpdComplianceStatus = user.CpdComplianceStatus,
            CpdComplianceExpiryDate = user.CpdComplianceExpiryDate,
            AssignedArea = user.AssignedOperationalArea == null ? "Unassigned" : user.AssignedOperationalArea.Name,
            RoleName = user.AppRole == null ? "Unassigned" : user.AppRole.Name,
            Status = user.Status
        });
    }

    public class StaffProfileItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StaffIdentifier { get; set; }
        public string? NationalId { get; set; }
        public string? CellNumber { get; set; }
        public string? QualificationFunction { get; set; }
        public string? PractitionerNumber { get; set; }
        public DateTime? AnnualLicenseExpiryDate { get; set; }
        public string? CpdComplianceStatus { get; set; }
        public DateTime? CpdComplianceExpiryDate { get; set; }
        public string AssignedArea { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastLoginAtUtc { get; set; }
    }

    public class StaffSearchResult
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StaffIdentifier { get; set; }
        public string? NationalId { get; set; }
        public string? CellNumber { get; set; }
        public string? QualificationFunction { get; set; }
        public string? PractitionerNumber { get; set; }
        public DateTime? AnnualLicenseExpiryDate { get; set; }
        public string? CpdComplianceStatus { get; set; }
        public DateTime? CpdComplianceExpiryDate { get; set; }
        public string AssignedArea { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class FileCategorySummary
    {
        public string Category { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public DateTime LatestUploadAtUtc { get; set; }
    }

    public class StaffRecordSummary
    {
        public int FileCount { get; set; }
        public int SubmittedChecks { get; set; }
        public int DraftChecks { get; set; }
        public int OpenTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int ReportedIssues { get; set; }
        public int AssignedIssues { get; set; }
    }
}
