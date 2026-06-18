using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public static class UserActionPermissions
{
    public const string RegistersView = "registers.view";
    public const string RegistersVehicleEdit = "registers.vehicle.edit";
    public const string RegistersEquipmentEdit = "registers.equipment.edit";
    public const string RegistersStockEdit = "registers.stock.edit";
    public const string RegistersMedicationEdit = "registers.medication.edit";
    public const string RegistersStaffEdit = "registers.staff.edit";
    public const string RegistersDelete = "registers.delete";
    public const string AssetsMove = "assets.move";
    public const string AssetsServiceUpdate = "assets.service.update";

    public const string ChecklistsBuild = "checklists.build";
    public const string ChecklistsEdit = "checklists.edit";
    public const string ChecklistsPublish = "checklists.publish";
    public const string ChecklistsUpload = "checklists.upload";
    public const string ChecklistsReports = "checklists.reports";
    public const string ChecklistsVarianceReview = "checklists.variance.review";

    public const string DailyChecksComplete = "daily.checks.complete";
    public const string DailySamePrevious = "daily.sameprevious";
    public const string IssuesReport = "issues.report";
    public const string IssuesManage = "issues.manage";
    public const string TasksSend = "tasks.send";
    public const string TasksManage = "tasks.manage";
    public const string TasksFeedback = "tasks.feedback";

    public const string DashboardReadiness = "dashboard.readiness";
    public const string ReportsOperations = "reports.operations";
    public const string ReadinessEngine = "readiness.engine";
    public const string SetupAreas = "setup.areas";
    public const string SetupCompany = "setup.company";
    public const string SetupAudit = "setup.audit";
    public const string SetupAccess = "setup.access";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        RegistersView,
        RegistersVehicleEdit,
        RegistersEquipmentEdit,
        RegistersStockEdit,
        RegistersMedicationEdit,
        RegistersStaffEdit,
        RegistersDelete,
        AssetsMove,
        AssetsServiceUpdate,
        ChecklistsBuild,
        ChecklistsEdit,
        ChecklistsPublish,
        ChecklistsUpload,
        ChecklistsReports,
        ChecklistsVarianceReview,
        DailyChecksComplete,
        DailySamePrevious,
        IssuesReport,
        IssuesManage,
        TasksSend,
        TasksManage,
        TasksFeedback,
        DashboardReadiness,
        ReportsOperations,
        ReadinessEngine,
        SetupAreas,
        SetupCompany,
        SetupAudit,
        SetupAccess
    };

    public static IReadOnlyCollection<string> DefaultForAccess(string accessLevel)
    {
        return accessLevel switch
        {
            CurrentUserService.SeniorManagementAccess => All.ToList(),

            CurrentUserService.OperationalManagementAccess => new[]
            {
                RegistersView,
                RegistersVehicleEdit,
                RegistersEquipmentEdit,
                RegistersStockEdit,
                RegistersMedicationEdit,
                RegistersStaffEdit,
                AssetsMove,
                AssetsServiceUpdate,
                ChecklistsBuild,
                ChecklistsEdit,
                ChecklistsReports,
                ChecklistsVarianceReview,
                DailyChecksComplete,
                DailySamePrevious,
                IssuesReport,
                IssuesManage,
                TasksSend,
                TasksManage,
                TasksFeedback,
                DashboardReadiness,
                ReportsOperations
            },

            _ => new[]
            {
                DailyChecksComplete,
                DailySamePrevious,
                IssuesReport,
                TasksFeedback
            }
        };
    }
}

public interface IUserActionPermissionService
{
    Task<bool> HasPermissionAsync(AppUser user, string permissionKey, CancellationToken cancellationToken = default);
    Task<bool> HasAllPermissionsAsync(AppUser user, IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default);
    Task<bool> HasAnyPermissionAsync(AppUser user, IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default);
}

public class UserActionPermissionService : IUserActionPermissionService
{
    private readonly VectorDbContext _db;

    public UserActionPermissionService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasPermissionAsync(AppUser user, string permissionKey, CancellationToken cancellationToken = default)
    {
        return await HasAllPermissionsAsync(user, new[] { permissionKey }, cancellationToken);
    }

    public async Task<bool> HasAllPermissionsAsync(AppUser user, IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default)
    {
        var keys = NormalizeKeys(permissionKeys);
        if (keys.Count == 0 || CurrentUserService.IsSeniorAccessRole(user.AppRole?.Name))
        {
            return true;
        }

        var effectiveKeys = await LoadEffectivePermissionKeysAsync(user, cancellationToken);
        return keys.All(key => effectiveKeys.Contains(key));
    }

    public async Task<bool> HasAnyPermissionAsync(AppUser user, IEnumerable<string> permissionKeys, CancellationToken cancellationToken = default)
    {
        var keys = NormalizeKeys(permissionKeys);
        if (keys.Count == 0 || CurrentUserService.IsSeniorAccessRole(user.AppRole?.Name))
        {
            return true;
        }

        var effectiveKeys = await LoadEffectivePermissionKeysAsync(user, cancellationToken);
        return keys.Any(key => effectiveKeys.Contains(key));
    }

    private async Task<HashSet<string>> LoadEffectivePermissionKeysAsync(AppUser user, CancellationToken cancellationToken)
    {
        var savedPermissions = await _db.AppUserAccessPermissions
            .AsNoTracking()
            .Where(permission =>
                permission.CompanyId == user.CompanyId &&
                permission.AppUserId == user.Id)
            .Select(permission => new
            {
                permission.PermissionKey,
                permission.Status
            })
            .ToListAsync(cancellationToken);

        if (savedPermissions.Count == 0)
        {
            return UserActionPermissions
                .DefaultForAccess(RoleNameToAccessView(user.AppRole?.Name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return savedPermissions
            .Where(permission => string.Equals(permission.Status, "Allowed", StringComparison.OrdinalIgnoreCase))
            .Select(permission => permission.PermissionKey)
            .Where(UserActionPermissions.All.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> NormalizeKeys(IEnumerable<string> permissionKeys)
    {
        return permissionKeys
            .Where(UserActionPermissions.All.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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
}
