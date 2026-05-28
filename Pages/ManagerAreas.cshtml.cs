using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ManagerAreasModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ManagerAreasModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public int? ManagerUserId { get; set; }
    [BindProperty] public List<int> SelectedAreaIds { get; set; } = new();

    public List<SelectListItem> ManagerOptions { get; private set; } = new();
    public List<AreaAssignmentOption> AreaOptions { get; private set; } = new();
    public ManagerAssignmentSummary? SelectedManager { get; private set; }
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        await LoadManagersAsync(currentUser.CompanyId);
        ManagerUserId ??= FirstManagerId();
        await LoadAreaOptionsAsync(currentUser.CompanyId);
        await LoadSelectedManagerAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        await LoadManagersAsync(currentUser.CompanyId);
        await LoadAreaOptionsAsync(currentUser.CompanyId);

        if (!ManagerUserId.HasValue || !await IsOperationalManagerAsync(currentUser.CompanyId, ManagerUserId.Value))
        {
            StatusMessage = "Select an operational manager before saving area access.";
            await LoadSelectedManagerAsync(currentUser.CompanyId);
            return Page();
        }

        var validAreaIds = AreaOptions.Select(area => area.Id).ToHashSet();
        var selectedAreaIds = SelectedAreaIds
            .Where(validAreaIds.Contains)
            .Distinct()
            .ToHashSet();

        var now = DateTime.UtcNow;
        var existingAssignments = await _db.ManagerOperationalAreaAssignments
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == ManagerUserId.Value)
            .ToListAsync();

        foreach (var assignment in existingAssignments)
        {
            var shouldBeActive = selectedAreaIds.Contains(assignment.OperationalAreaId);
            if (shouldBeActive && assignment.Status != "Active")
            {
                assignment.Status = "Active";
                assignment.AssignedByUserId = currentUser.Id;
                assignment.UpdatedAtUtc = now;
            }
            else if (!shouldBeActive && assignment.Status == "Active")
            {
                assignment.Status = "Removed";
                assignment.UpdatedAtUtc = now;
            }
        }

        var existingAreaIds = existingAssignments.Select(assignment => assignment.OperationalAreaId).ToHashSet();
        foreach (var areaId in selectedAreaIds.Where(areaId => !existingAreaIds.Contains(areaId)))
        {
            _db.ManagerOperationalAreaAssignments.Add(new ManagerOperationalAreaAssignment
            {
                CompanyId = currentUser.CompanyId,
                ManagerUserId = ManagerUserId.Value,
                OperationalAreaId = areaId,
                AssignedByUserId = currentUser.Id,
                Status = "Active",
                AssignedAtUtc = now
            });
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Manager area assignments updated",
            EntityType = "AppUser",
            EntityId = ManagerUserId.Value,
            Details = $"{selectedAreaIds.Count} active operational area assignment(s) saved for manager user #{ManagerUserId.Value}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Manager area access saved.";
        await LoadAreaOptionsAsync(currentUser.CompanyId);
        await LoadSelectedManagerAsync(currentUser.CompanyId);
        return Page();
    }

    private async Task LoadManagersAsync(int companyId)
    {
        ManagerOptions = await _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Where(user =>
                user.CompanyId == companyId &&
                user.Status == "Active" &&
                user.AppRole != null &&
                user.AppRole.Name == "Operational Management")
            .OrderBy(user => user.FullName)
            .Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = user.FullName + " - " + user.Email
            })
            .ToListAsync();
    }

    private async Task LoadAreaOptionsAsync(int companyId)
    {
        var selectedManagerId = ManagerUserId.GetValueOrDefault();
        var activeAssignments = selectedManagerId == 0
            ? new HashSet<int>()
            : (await _db.ManagerOperationalAreaAssignments
                .AsNoTracking()
                .Where(assignment =>
                    assignment.CompanyId == companyId &&
                    assignment.ManagerUserId == selectedManagerId &&
                    assignment.Status == "Active")
                .Select(assignment => assignment.OperationalAreaId)
                .ToListAsync())
                .ToHashSet();

        AreaOptions = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == companyId && area.Status == "Active")
            .OrderBy(area => area.AreaType)
            .ThenBy(area => area.Name)
            .Select(area => new AreaAssignmentOption
            {
                Id = area.Id,
                Name = area.Name,
                AreaType = area.AreaType,
                Address = area.Address,
                IsAssigned = activeAssignments.Contains(area.Id)
            })
            .ToListAsync();
    }

    private async Task LoadSelectedManagerAsync(int companyId)
    {
        if (!ManagerUserId.HasValue)
        {
            SelectedManager = null;
            return;
        }

        SelectedManager = await _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Where(user => user.CompanyId == companyId && user.Id == ManagerUserId.Value)
            .Select(user => new ManagerAssignmentSummary
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = user.AppRole == null ? "Unassigned" : user.AppRole.Name
            })
            .FirstOrDefaultAsync();
    }

    private async Task<bool> IsOperationalManagerAsync(int companyId, int managerUserId)
    {
        return await _db.AppUsers
            .Include(user => user.AppRole)
            .AnyAsync(user =>
                user.CompanyId == companyId &&
                user.Id == managerUserId &&
                user.Status == "Active" &&
                user.AppRole != null &&
                user.AppRole.Name == "Operational Management");
    }

    private int? FirstManagerId()
    {
        var firstValue = ManagerOptions.FirstOrDefault()?.Value;
        return int.TryParse(firstValue, out var managerId) ? managerId : null;
    }

    public class AreaAssignmentOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AreaType { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class ManagerAssignmentSummary
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}
