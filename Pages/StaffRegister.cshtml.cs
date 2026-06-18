using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class StaffRegisterModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public StaffRegisterModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }

    public List<StaffRegisterItem> StaffMembers { get; private set; } = new();
    public string ScopeLabel { get; private set; } = "All staff";
    public bool IsSeniorOverview { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var query = _db.AppUsers
            .Include(user => user.AppRole)
            .Include(user => user.AssignedOperationalArea)
            .Where(user => user.CompanyId == currentUser.CompanyId);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(user =>
                user.FullName.Contains(search)
                || user.Email.Contains(search)
                || (user.StaffIdentifier != null && user.StaffIdentifier.Contains(search))
                || (user.NationalId != null && user.NationalId.Contains(search))
                || (user.CellNumber != null && user.CellNumber.Contains(search))
                || (user.QualificationFunction != null && user.QualificationFunction.Contains(search))
                || (user.PractitionerNumber != null && user.PractitionerNumber.Contains(search))
                || (user.CpdComplianceStatus != null && user.CpdComplianceStatus.Contains(search))
                || (user.AssignedOperationalArea != null && user.AssignedOperationalArea.Name.Contains(search))
                || (user.AppRole != null && user.AppRole.Name.Contains(search)));
        }

        IsSeniorOverview = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        if (IsSeniorOverview)
        {
            ScopeLabel = "All company staff and managers";
        }
        else
        {
            var assignedAreaIds = await LoadAssignedAreaIdsAsync(currentUser);
            ScopeLabel = assignedAreaIds.Count == 0
                ? "No assigned staff area"
                : "Staff linked to your assigned area(s)";

            query = assignedAreaIds.Count == 0
                ? query.Where(user => user.Id == currentUser.Id)
                : query.Where(user =>
                    user.Id == currentUser.Id ||
                    (user.AssignedOperationalAreaId.HasValue && assignedAreaIds.Contains(user.AssignedOperationalAreaId.Value)));
        }

        StaffMembers = await query
            .Where(user => user.Status != "Deleted")
            .OrderBy(user => user.QualificationFunction == null || user.QualificationFunction == string.Empty ? "Unassigned clinical qualification / scope" : user.QualificationFunction)
            .ThenBy(user => user.FullName)
            .Select(user => new StaffRegisterItem
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
            })
            .ToListAsync();

        return Page();
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

    public class StaffRegisterItem
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
}
