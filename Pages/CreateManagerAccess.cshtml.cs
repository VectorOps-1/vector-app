using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class CreateManagerAccessModel : PageModel
{
    private static readonly IReadOnlyDictionary<string, string> AccessRoleNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [CurrentUserService.StaffAccess] = "Staff",
        [CurrentUserService.OperationalManagementAccess] = "Operational Management",
        [CurrentUserService.SeniorManagementAccess] = "Senior Management"
    };

    private static readonly IReadOnlyList<AccessPermissionGroup> PermissionCatalog = new List<AccessPermissionGroup>
    {
        new("Registers", new[]
        {
            new AccessPermissionOption("registers.view", "View registers", "Open vehicle, equipment, stock, medication, and staff registers in permitted scope."),
            new AccessPermissionOption("registers.vehicle.edit", "Edit vehicle register", "Add or edit vehicle source-of-truth records."),
            new AccessPermissionOption("registers.equipment.edit", "Edit equipment register", "Add or edit equipment source-of-truth records."),
            new AccessPermissionOption("registers.stock.edit", "Edit stock register", "Add or edit disposable stock source-of-truth records."),
            new AccessPermissionOption("registers.medication.edit", "Edit medication register", "Add or edit medication source-of-truth records."),
            new AccessPermissionOption("registers.staff.edit", "Edit staff register", "Add or edit staff profiles and register-controlled staff fields."),
            new AccessPermissionOption("registers.delete", "Delete register records", "Remove register records where deletion is allowed."),
            new AccessPermissionOption("assets.move", "Move / reallocate assets", "Move vehicles, equipment, stock, and medication between areas, vehicles, and storage."),
            new AccessPermissionOption("assets.service.update", "Update service / expiry dates", "Update service dates, expiry dates, licensing, and similar register dates.")
        }),
        new("Checklist Management", new[]
        {
            new AccessPermissionOption("checklists.build", "Build checklists", "Create new checklist templates manually."),
            new AccessPermissionOption("checklists.edit", "Edit saved checklists", "Edit existing checklist templates."),
            new AccessPermissionOption("checklists.publish", "Publish checklists", "Publish active checklists for areas, functions, subtypes, or vehicles."),
            new AccessPermissionOption("checklists.upload", "Upload checklists / registers", "Upload existing checklist and register source files."),
            new AccessPermissionOption("checklists.reports", "View checklist reports", "Open submitted checklist evidence and PDF reports."),
            new AccessPermissionOption("checklists.variance.review", "Review checklist variance alerts", "Approve or reject captured differences that may update registers.")
        }),
        new("Daily Operations", new[]
        {
            new AccessPermissionOption("daily.checks.complete", "Complete daily checks", "Complete assigned live vehicle and equipment checks."),
            new AccessPermissionOption("daily.sameprevious", "Use same as previous shift", "Use same-as-previous-shift controls where enabled."),
            new AccessPermissionOption("issues.report", "Report issues", "Create operational issues from inside the app."),
            new AccessPermissionOption("issues.manage", "Manage issues", "Assign, resolve, delete, or close issues in permitted scope."),
            new AccessPermissionOption("tasks.send", "Send tasks", "Send tasks to users in permitted scope."),
            new AccessPermissionOption("tasks.manage", "Manage tasks", "Delete, close, or update tasks in permitted scope."),
            new AccessPermissionOption("tasks.feedback", "Submit task feedback", "Submit feedback on assigned tasks or general app work.")
        }),
        new("Oversight And Setup", new[]
        {
            new AccessPermissionOption("dashboard.readiness", "View readiness dashboard", "Open readiness score, metrics, and readiness variables."),
            new AccessPermissionOption("reports.operations", "View operational reports", "Open operational reports for permitted scope."),
            new AccessPermissionOption("readiness.engine", "Use readiness engine", "Edit or request scoring rules and readiness weightings."),
            new AccessPermissionOption("setup.areas", "Manage areas / manager control", "Create areas and manage manager area assignment."),
            new AccessPermissionOption("setup.company", "Manage company profile", "Edit client details, logo, workspace, and company-level settings."),
            new AccessPermissionOption("setup.audit", "View audit log", "Open app audit logs."),
            new AccessPermissionOption("setup.access", "Manage access setup", "Change roles, areas, and granular permissions for users below own access level.")
        })
    };

    private static readonly IReadOnlyDictionary<string, string> PermissionNames = PermissionCatalog
        .SelectMany(group => group.Options)
        .ToDictionary(option => option.Key, option => option.Name, StringComparer.OrdinalIgnoreCase);

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public CreateManagerAccessModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public int? StaffUserId { get; set; }
    [BindProperty(SupportsGet = true)] public string AccessLevel { get; set; } = CurrentUserService.OperationalManagementAccess;
    [BindProperty] public int? AssignedOperationalAreaId { get; set; }
    [BindProperty] public bool AccountActive { get; set; } = true;
    [BindProperty] public List<string> SelectedPermissionKeys { get; set; } = new();

    public List<SelectListItem> StaffOptions { get; private set; } = new();
    public List<SelectListItem> AccessLevelOptions { get; private set; } = new();
    public List<SelectListItem> AreaOptions { get; private set; } = new();
    public List<AccessRegisterRow> AccessRows { get; private set; } = new();
    public IReadOnlyList<AccessPermissionGroup> PermissionGroups => PermissionCatalog;
    public HashSet<string> SelectedPermissionKeySet { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public StaffAccessProfile? SelectedStaff { get; private set; }
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        AccessLevel = CurrentUserService.NormalizeAccessView(AccessLevel);
        await LoadPageDataAsync(currentUser, useRegisterValues: true);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        AccessLevel = CurrentUserService.NormalizeAccessView(AccessLevel);

        if (!StaffUserId.HasValue)
        {
            ModelState.AddModelError(nameof(StaffUserId), "Select the existing staff profile before saving access.");
        }

        if (!AccessRoleNames.TryGetValue(AccessLevel, out var roleName))
        {
            ModelState.AddModelError(nameof(AccessLevel), "Select a valid access level.");
        }

        SelectedPermissionKeys = NormalizePermissionKeys(SelectedPermissionKeys).ToList();

        var staffUser = StaffUserId.HasValue
            ? await _db.AppUsers
                .Include(user => user.AppRole)
                .FirstOrDefaultAsync(user => user.CompanyId == currentUser.CompanyId && user.Id == StaffUserId.Value)
            : null;

        if (staffUser is null)
        {
            ModelState.AddModelError(nameof(StaffUserId), "That staff profile was not found in this company register.");
        }
        else if (staffUser.Id == currentUser.Id)
        {
            ModelState.AddModelError(nameof(StaffUserId), "Select another staff member to change access. Your own access cannot be changed while you are signed in.");
        }
        else if (!CanEditTarget(currentUser.AppRole?.Name, staffUser.AppRole?.Name))
        {
            ModelState.AddModelError(nameof(StaffUserId), "You can only update access for users below your own access level.");
        }

        var role = roleName is null
            ? null
            : await _db.AppRoles.FirstOrDefaultAsync(item => item.Name == roleName);

        if (roleName is not null && role is null)
        {
            ModelState.AddModelError(nameof(AccessLevel), $"The {roleName} role is missing from setup.");
        }

        if (AssignedOperationalAreaId.HasValue)
        {
            var areaExists = await _db.OperationalAreas.AnyAsync(area =>
                area.CompanyId == currentUser.CompanyId &&
                area.Id == AssignedOperationalAreaId.Value &&
                area.Status == "Active");

            if (!areaExists)
            {
                ModelState.AddModelError(nameof(AssignedOperationalAreaId), "Select an active area from Master Setup.");
            }
        }

        if (!ModelState.IsValid || staffUser is null || role is null)
        {
            await LoadPageDataAsync(currentUser, useRegisterValues: false);
            return Page();
        }

        var now = DateTime.UtcNow;
        var previousRole = staffUser.AppRole?.Name ?? "Unassigned";
        var previousAreaId = staffUser.AssignedOperationalAreaId;
        var previousStatus = staffUser.Status;

        staffUser.AppRoleId = role.Id;
        staffUser.AssignedOperationalAreaId = AssignedOperationalAreaId;
        staffUser.Status = AccountActive ? "Active" : "Inactive";

        await SyncManagerAreaAssignmentAsync(currentUser, staffUser, AccessLevel, AssignedOperationalAreaId, now);
        await SyncAccessPermissionsAsync(currentUser, staffUser, SelectedPermissionKeys, now);

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Staff access updated",
            EntityType = "AppUser",
            EntityId = staffUser.Id,
            Details = $"{staffUser.FullName} access changed from {previousRole} to {role.Name}; status {previousStatus} to {staffUser.Status}; area #{previousAreaId?.ToString() ?? "none"} to #{AssignedOperationalAreaId?.ToString() ?? "none"}; permissions {SelectedPermissionKeys.Count}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        StatusMessage = $"{staffUser.FullName} access updated from the staff register.";
        ActionSaved = true;
        await LoadPageDataAsync(currentUser, useRegisterValues: true);
        return Page();
    }

    private async Task LoadPageDataAsync(AppUser currentUser, bool useRegisterValues)
    {
        var companyId = currentUser.CompanyId;

        StaffOptions = await _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Where(user => user.CompanyId == companyId)
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.StaffIdentifier)
            .Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = user.FullName
                    + (string.IsNullOrWhiteSpace(user.StaffIdentifier) ? string.Empty : $" / {user.StaffIdentifier}")
                    + $" - {(user.AppRole == null ? "No access role" : user.AppRole.Name)}",
                Selected = StaffUserId.HasValue && user.Id == StaffUserId.Value
            })
            .ToListAsync();

        AccessLevelOptions = new List<SelectListItem>
        {
            new() { Value = CurrentUserService.StaffAccess, Text = "Staff", Selected = AccessLevel == CurrentUserService.StaffAccess },
            new() { Value = CurrentUserService.OperationalManagementAccess, Text = "Operational Management", Selected = AccessLevel == CurrentUserService.OperationalManagementAccess },
            new() { Value = CurrentUserService.SeniorManagementAccess, Text = "Senior Management", Selected = AccessLevel == CurrentUserService.SeniorManagementAccess }
        };

        AreaOptions = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == companyId && area.Status == "Active")
            .OrderBy(area => area.AreaType)
            .ThenBy(area => area.Name)
            .Select(area => new SelectListItem
            {
                Value = area.Id.ToString(),
                Text = area.Name + " - " + area.AreaType,
                Selected = AssignedOperationalAreaId.HasValue && area.Id == AssignedOperationalAreaId.Value
            })
            .ToListAsync();

        await LoadAccessRowsAsync(currentUser);

        if (!StaffUserId.HasValue)
        {
            SelectedStaff = null;
            return;
        }

        SelectedStaff = await _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Include(user => user.AssignedOperationalArea)
            .Where(user => user.CompanyId == companyId && user.Id == StaffUserId.Value)
            .Select(user => new StaffAccessProfile
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                CellNumber = user.CellNumber,
                StaffIdentifier = user.StaffIdentifier,
                NationalId = user.NationalId,
                QualificationFunction = user.QualificationFunction,
                PractitionerNumber = user.PractitionerNumber,
                AnnualLicenseExpiryDate = user.AnnualLicenseExpiryDate,
                CpdComplianceStatus = user.CpdComplianceStatus,
                CpdComplianceExpiryDate = user.CpdComplianceExpiryDate,
                RoleName = user.AppRole == null ? "Unassigned" : user.AppRole.Name,
                AssignedOperationalAreaId = user.AssignedOperationalAreaId,
                AssignedArea = user.AssignedOperationalArea == null ? "Unassigned" : user.AssignedOperationalArea.Name,
                Status = user.Status
            })
            .FirstOrDefaultAsync();

        if (SelectedStaff is null)
        {
            return;
        }

        if (!useRegisterValues)
        {
            SelectedPermissionKeys = NormalizePermissionKeys(SelectedPermissionKeys).ToList();
            SelectedPermissionKeySet = new HashSet<string>(SelectedPermissionKeys, StringComparer.OrdinalIgnoreCase);
            return;
        }

        AccessLevel = RoleNameToAccessView(SelectedStaff.RoleName);
        AssignedOperationalAreaId = SelectedStaff.AssignedOperationalAreaId;
        AccountActive = string.Equals(SelectedStaff.Status, "Active", StringComparison.OrdinalIgnoreCase);
        SelectedPermissionKeys = await LoadPermissionKeysAsync(companyId, SelectedStaff.Id);
        if (SelectedPermissionKeys.Count == 0)
        {
            SelectedPermissionKeys = DefaultPermissionKeysForAccess(AccessLevel).ToList();
        }

        SelectedPermissionKeySet = new HashSet<string>(SelectedPermissionKeys, StringComparer.OrdinalIgnoreCase);

        foreach (var option in AccessLevelOptions)
        {
            option.Selected = option.Value == AccessLevel;
        }

        foreach (var option in AreaOptions)
        {
            option.Selected = int.TryParse(option.Value, out var areaId) && areaId == AssignedOperationalAreaId;
        }
    }

    private async Task LoadAccessRowsAsync(AppUser currentUser)
    {
        var companyId = currentUser.CompanyId;

        var managerScopes = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Include(assignment => assignment.OperationalArea)
            .Where(assignment =>
                assignment.CompanyId == companyId &&
                assignment.Status == "Active")
            .GroupBy(assignment => assignment.ManagerUserId)
            .Select(group => new
            {
                ManagerUserId = group.Key,
                AreaNames = group
                    .Where(assignment => assignment.OperationalArea != null)
                    .Select(assignment => assignment.OperationalArea!.Name)
                    .OrderBy(name => name)
                    .ToList()
            })
            .ToListAsync();

        var scopeMap = managerScopes.ToDictionary(
            item => item.ManagerUserId,
            item => item.AreaNames.Count == 0 ? "No manager scope" : string.Join(", ", item.AreaNames));

        var savedPermissions = await _db.AppUserAccessPermissions
            .AsNoTracking()
            .Where(permission =>
                permission.CompanyId == companyId &&
                permission.Status == "Allowed")
            .Select(permission => new
            {
                permission.AppUserId,
                permission.PermissionKey
            })
            .ToListAsync();

        var permissionMap = savedPermissions
            .GroupBy(permission => permission.AppUserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(permission => permission.PermissionKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(permission => permission)
                    .ToList());

        AccessRows = await _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Include(user => user.AssignedOperationalArea)
            .Where(user => user.CompanyId == companyId)
            .OrderByDescending(user => user.AppRole != null && user.AppRole.Name == "Company Owner")
            .ThenBy(user => user.AppRole == null ? "Unassigned" : user.AppRole.Name)
            .ThenBy(user => user.FullName)
            .Select(user => new AccessRegisterRow
            {
                StaffUserId = user.Id,
                FullName = user.FullName,
                StaffIdentifier = user.StaffIdentifier,
                Email = user.Email,
                AccessRole = user.AppRole == null ? "Unassigned" : user.AppRole.Name,
                AssignedArea = user.AssignedOperationalArea == null ? "Unassigned" : user.AssignedOperationalArea.Name,
                Status = user.Status
            })
            .ToListAsync();

        foreach (var row in AccessRows)
        {
            row.ManagerScope = scopeMap.TryGetValue(row.StaffUserId, out var scope)
                ? scope
                : "No manager scope";
            row.EditAccessLevel = RoleNameToAccessView(row.AccessRole);
            row.CanEdit = row.StaffUserId != currentUser.Id && CanEditTarget(currentUser.AppRole?.Name, row.AccessRole);

            var permissionKeys = permissionMap.TryGetValue(row.StaffUserId, out var keys)
                ? keys
                : DefaultPermissionKeysForAccess(row.EditAccessLevel).ToList();

            row.PermissionCount = permissionKeys.Count;
            row.PermissionSummary = DescribePermissionSummary(permissionKeys);
        }
    }

    private async Task SyncManagerAreaAssignmentAsync(
        AppUser currentUser,
        AppUser staffUser,
        string accessLevel,
        int? assignedOperationalAreaId,
        DateTime now)
    {
        var existingAssignments = await _db.ManagerOperationalAreaAssignments
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == staffUser.Id)
            .ToListAsync();

        if (accessLevel != CurrentUserService.OperationalManagementAccess || !assignedOperationalAreaId.HasValue)
        {
            foreach (var assignment in existingAssignments.Where(assignment => assignment.Status == "Active"))
            {
                assignment.Status = "Removed";
                assignment.UpdatedAtUtc = now;
            }

            return;
        }

        foreach (var assignment in existingAssignments)
        {
            var shouldBeActive = assignment.OperationalAreaId == assignedOperationalAreaId.Value;
            if (shouldBeActive)
            {
                assignment.Status = "Active";
                assignment.AssignedByUserId = currentUser.Id;
                assignment.UpdatedAtUtc = now;
            }
            else if (assignment.Status == "Active")
            {
                assignment.Status = "Removed";
                assignment.UpdatedAtUtc = now;
            }
        }

        if (existingAssignments.All(assignment => assignment.OperationalAreaId != assignedOperationalAreaId.Value))
        {
            _db.ManagerOperationalAreaAssignments.Add(new ManagerOperationalAreaAssignment
            {
                CompanyId = currentUser.CompanyId,
                ManagerUserId = staffUser.Id,
                OperationalAreaId = assignedOperationalAreaId.Value,
                AssignedByUserId = currentUser.Id,
                Status = "Active",
                AssignedAtUtc = now
            });
        }
    }

    private async Task<List<string>> LoadPermissionKeysAsync(int companyId, int staffUserId)
    {
        return await _db.AppUserAccessPermissions
            .AsNoTracking()
            .Where(permission =>
                permission.CompanyId == companyId &&
                permission.AppUserId == staffUserId &&
                permission.Status == "Allowed")
            .OrderBy(permission => permission.PermissionKey)
            .Select(permission => permission.PermissionKey)
            .ToListAsync();
    }

    private async Task SyncAccessPermissionsAsync(AppUser currentUser, AppUser staffUser, IReadOnlyCollection<string> permissionKeys, DateTime now)
    {
        var selectedKeys = NormalizePermissionKeys(permissionKeys).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var originallySelectedKeys = selectedKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var validKeys = PermissionCatalog
            .SelectMany(group => group.Options)
            .Select(option => option.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingPermissions = await _db.AppUserAccessPermissions
            .Where(permission =>
                permission.CompanyId == currentUser.CompanyId &&
                permission.AppUserId == staffUser.Id)
            .ToListAsync();
        var existingKeys = existingPermissions
            .Select(permission => permission.PermissionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var existingPermission in existingPermissions)
        {
            if (selectedKeys.Remove(existingPermission.PermissionKey))
            {
                existingPermission.Status = "Allowed";
            }
            else
            {
                existingPermission.Status = "Removed";
            }

            existingPermission.UpdatedByUserId = currentUser.Id;
            existingPermission.UpdatedAtUtc = now;
        }

        foreach (var permissionKey in selectedKeys)
        {
            _db.AppUserAccessPermissions.Add(new AppUserAccessPermission
            {
                CompanyId = currentUser.CompanyId,
                AppUserId = staffUser.Id,
                PermissionKey = permissionKey,
                Status = "Allowed",
                UpdatedByUserId = currentUser.Id,
                UpdatedAtUtc = now
            });
        }

        foreach (var removedKey in validKeys
            .Where(key => !originallySelectedKeys.Contains(key) && !existingKeys.Contains(key)))
        {
            _db.AppUserAccessPermissions.Add(new AppUserAccessPermission
            {
                CompanyId = currentUser.CompanyId,
                AppUserId = staffUser.Id,
                PermissionKey = removedKey,
                Status = "Removed",
                UpdatedByUserId = currentUser.Id,
                UpdatedAtUtc = now
            });
        }
    }

    private static IEnumerable<string> NormalizePermissionKeys(IEnumerable<string>? permissionKeys)
    {
        var validKeys = PermissionCatalog
            .SelectMany(group => group.Options)
            .Select(option => option.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return (permissionKeys ?? Enumerable.Empty<string>())
            .Where(key => validKeys.Contains(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key);
    }

    private static IEnumerable<string> DefaultPermissionKeysForAccess(string accessLevel)
    {
        return accessLevel switch
        {
            CurrentUserService.SeniorManagementAccess => PermissionCatalog
                .SelectMany(group => group.Options)
                .Select(option => option.Key),

            CurrentUserService.OperationalManagementAccess => new[]
            {
                "registers.view",
                "registers.vehicle.edit",
                "registers.equipment.edit",
                "registers.stock.edit",
                "registers.medication.edit",
                "registers.staff.edit",
                "assets.move",
                "assets.service.update",
                "checklists.build",
                "checklists.edit",
                "checklists.reports",
                "checklists.variance.review",
                "daily.checks.complete",
                "daily.sameprevious",
                "issues.report",
                "issues.manage",
                "tasks.send",
                "tasks.manage",
                "tasks.feedback",
                "dashboard.readiness",
                "reports.operations"
            },

            _ => new[]
            {
                "daily.checks.complete",
                "daily.sameprevious",
                "issues.report",
                "tasks.feedback"
            }
        };
    }

    private static string DescribePermissionSummary(IReadOnlyCollection<string> permissionKeys)
    {
        if (permissionKeys.Count == 0)
        {
            return "No action permissions";
        }

        var permissionNames = permissionKeys
            .Select(key => PermissionNames.TryGetValue(key, out var name) ? name : key)
            .OrderBy(name => name)
            .ToList();

        var visibleNames = permissionNames.Take(4);
        var suffix = permissionNames.Count > 4
            ? $" + {permissionNames.Count - 4} more"
            : string.Empty;

        return string.Join(", ", visibleNames) + suffix;
    }

    private static string RoleNameToAccessView(string? roleName)
    {
        return roleName switch
        {
            "Staff" => CurrentUserService.StaffAccess,
            "Senior Management" or "Company Owner" => CurrentUserService.SeniorManagementAccess,
            _ => CurrentUserService.OperationalManagementAccess
        };
    }

    private static bool CanEditTarget(string? editorRole, string? targetRole)
    {
        return RoleRank(editorRole) > RoleRank(targetRole);
    }

    private static int RoleRank(string? roleName)
    {
        return roleName switch
        {
            "Company Owner" => 4,
            "Senior Management" => 3,
            "Operational Management" => 2,
            "Staff" => 1,
            _ => 0
        };
    }

    public sealed class StaffAccessProfile
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CellNumber { get; set; }
        public string? StaffIdentifier { get; set; }
        public string? NationalId { get; set; }
        public string? QualificationFunction { get; set; }
        public string? PractitionerNumber { get; set; }
        public DateTime? AnnualLicenseExpiryDate { get; set; }
        public string? CpdComplianceStatus { get; set; }
        public DateTime? CpdComplianceExpiryDate { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int? AssignedOperationalAreaId { get; set; }
        public string AssignedArea { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class AccessRegisterRow
    {
        public int StaffUserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? StaffIdentifier { get; set; }
        public string Email { get; set; } = string.Empty;
        public string AccessRole { get; set; } = string.Empty;
        public string AssignedArea { get; set; } = string.Empty;
        public string ManagerScope { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string EditAccessLevel { get; set; } = CurrentUserService.OperationalManagementAccess;
        public int PermissionCount { get; set; }
        public string PermissionSummary { get; set; } = string.Empty;
        public bool CanEdit { get; set; }
    }

    public sealed record AccessPermissionGroup(string Name, IReadOnlyList<AccessPermissionOption> Options);

    public sealed record AccessPermissionOption(string Key, string Name, string Description);
}
