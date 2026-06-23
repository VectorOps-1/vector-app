using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class AccessModelSetupService
{
    public const string ScopeAssignedAreasOnly = "assigned-areas-only";
    public const string ScopeAllOperationalAreas = "all-operational-areas";

    private static readonly IReadOnlySet<string> OperationalManagerRequestOnlyRemovedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        UserActionPermissions.RegistersVehicleEdit,
        UserActionPermissions.RegistersEquipmentEdit,
        UserActionPermissions.RegistersStockEdit,
        UserActionPermissions.RegistersMedicationEdit,
        UserActionPermissions.RegistersStaffEdit,
        UserActionPermissions.RegistersDelete,
        UserActionPermissions.AssetsMove,
        UserActionPermissions.AssetsServiceUpdate,
        UserActionPermissions.ChecklistsBuild,
        UserActionPermissions.ChecklistsEdit,
        UserActionPermissions.ChecklistsPublish,
        UserActionPermissions.StockOrdersApprove,
        UserActionPermissions.ReadinessEngine,
        UserActionPermissions.SetupAreas,
        UserActionPermissions.SetupCompany,
        UserActionPermissions.SetupAudit,
        UserActionPermissions.SetupAccess
    };

    private readonly VectorDbContext _db;

    public AccessModelSetupService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<AccessModelDefaultsSnapshot> GetSnapshotAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId && item.Status == "Active", cancellationToken);

        return GetSnapshot(company);
    }

    public AccessModelDefaultsSnapshot GetSnapshot(Company? company)
    {
        var ownerDefaults = ParseOrDefault(company?.CompanyOwnerDefaultPermissionKeys, UserActionPermissions.All);
        var seniorDefaults = ParseOrDefault(company?.SeniorManagerDefaultPermissionKeys, UserActionPermissions.All);
        var operationalDefaults = ParseOrDefault(company?.OperationalManagerDefaultPermissionKeys, ProductOperationalManagerDefault());
        var staffDefaults = ParseOrDefault(company?.StaffDefaultPermissionKeys, ProductStaffDefault());
        var scopeBehavior = NormalizeScopeBehavior(company?.OperationalManagerScopeBehavior);

        return new AccessModelDefaultsSnapshot(
            company?.AccessModelDefaultsConfigured == true,
            scopeBehavior,
            ownerDefaults,
            seniorDefaults,
            operationalDefaults,
            staffDefaults,
            operationalDefaults.Contains(UserActionPermissions.ChecklistsBuild) || operationalDefaults.Contains(UserActionPermissions.ChecklistsEdit),
            HasAnyRegisterEdit(operationalDefaults),
            operationalDefaults.Contains(UserActionPermissions.StockOrdersApprove),
            IsOperationalManagerRequestOnly(operationalDefaults));
    }

    public IReadOnlyList<string> GetDefaultPermissionKeys(Company? company, string accessLevel, string? roleName = null)
    {
        var snapshot = GetSnapshot(company);
        if (string.Equals(roleName, "Company Owner", StringComparison.OrdinalIgnoreCase))
        {
            return snapshot.CompanyOwnerPermissionKeys;
        }

        return CurrentUserService.NormalizeAccessView(accessLevel) switch
        {
            CurrentUserService.SeniorManagementAccess => snapshot.SeniorManagerPermissionKeys,
            CurrentUserService.OperationalManagementAccess => snapshot.OperationalManagerPermissionKeys,
            _ => snapshot.StaffPermissionKeys
        };
    }

    public static IReadOnlyList<string> ProductOperationalManagerDefault()
    {
        return AccessPermissionCatalog.NormalizePermissionKeys(new[]
        {
            UserActionPermissions.RegistersView,
            UserActionPermissions.RegistersVehicleEdit,
            UserActionPermissions.RegistersEquipmentEdit,
            UserActionPermissions.RegistersStockEdit,
            UserActionPermissions.RegistersMedicationEdit,
            UserActionPermissions.RegistersStaffEdit,
            UserActionPermissions.AssetsMove,
            UserActionPermissions.AssetsServiceUpdate,
            UserActionPermissions.ChecklistsBuild,
            UserActionPermissions.ChecklistsEdit,
            UserActionPermissions.ChecklistsReports,
            UserActionPermissions.ChecklistsVarianceReview,
            UserActionPermissions.DailyChecksComplete,
            UserActionPermissions.DailySamePrevious,
            UserActionPermissions.IssuesReport,
            UserActionPermissions.IssuesManage,
            UserActionPermissions.TasksSend,
            UserActionPermissions.TasksManage,
            UserActionPermissions.TasksFeedback,
            UserActionPermissions.DashboardReadiness,
            UserActionPermissions.ReportsOperations
        });
    }

    public static IReadOnlyList<string> ProductStaffDefault()
    {
        return AccessPermissionCatalog.NormalizePermissionKeys(new[]
        {
            UserActionPermissions.DailyChecksComplete,
            UserActionPermissions.DailySamePrevious,
            UserActionPermissions.IssuesReport,
            UserActionPermissions.TasksFeedback
        });
    }

    public static IReadOnlyList<string> BuildOperationalManagerPermissions(
        bool canDraftChecklistChanges,
        bool canEditRegisters,
        bool canApproveStockOrders,
        bool requestOnlyMode)
    {
        var keys = ProductOperationalManagerDefault().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!canDraftChecklistChanges)
        {
            keys.Remove(UserActionPermissions.ChecklistsBuild);
            keys.Remove(UserActionPermissions.ChecklistsEdit);
        }
        else
        {
            keys.Add(UserActionPermissions.ChecklistsBuild);
            keys.Add(UserActionPermissions.ChecklistsEdit);
        }

        foreach (var registerEditKey in RegisterEditKeys())
        {
            if (canEditRegisters)
            {
                keys.Add(registerEditKey);
            }
            else
            {
                keys.Remove(registerEditKey);
            }
        }

        if (canApproveStockOrders)
        {
            keys.Add(UserActionPermissions.StockOrdersApprove);
        }
        else
        {
            keys.Remove(UserActionPermissions.StockOrdersApprove);
        }

        if (requestOnlyMode)
        {
            keys.RemoveWhere(OperationalManagerRequestOnlyRemovedKeys.Contains);
            keys.Add(UserActionPermissions.RegistersView);
            keys.Add(UserActionPermissions.ChecklistsReports);
            keys.Add(UserActionPermissions.IssuesReport);
            keys.Add(UserActionPermissions.TasksFeedback);
            keys.Add(UserActionPermissions.DashboardReadiness);
            keys.Add(UserActionPermissions.ReportsOperations);
        }

        return AccessPermissionCatalog.NormalizePermissionKeys(keys);
    }

    public static string NormalizeScopeBehavior(string? value)
    {
        return string.Equals(value, ScopeAllOperationalAreas, StringComparison.OrdinalIgnoreCase)
            ? ScopeAllOperationalAreas
            : ScopeAssignedAreasOnly;
    }

    public static string DescribeScopeBehavior(string? value)
    {
        return NormalizeScopeBehavior(value) == ScopeAllOperationalAreas
            ? "All operational areas"
            : "Assigned areas only";
    }

    public static IReadOnlyList<string> RegisterEditKeys()
    {
        return new[]
        {
            UserActionPermissions.RegistersVehicleEdit,
            UserActionPermissions.RegistersEquipmentEdit,
            UserActionPermissions.RegistersStockEdit,
            UserActionPermissions.RegistersMedicationEdit,
            UserActionPermissions.RegistersStaffEdit,
            UserActionPermissions.AssetsMove,
            UserActionPermissions.AssetsServiceUpdate
        };
    }

    private static IReadOnlyList<string> ParseOrDefault(string? serializedKeys, IEnumerable<string> fallbackKeys)
    {
        var parsed = AccessPermissionCatalog.ParsePermissionKeys(serializedKeys);
        return parsed.Count == 0
            ? AccessPermissionCatalog.NormalizePermissionKeys(fallbackKeys)
            : parsed;
    }

    private static bool HasAnyRegisterEdit(IReadOnlyCollection<string> keys)
    {
        return RegisterEditKeys().Any(keys.Contains);
    }

    private static bool IsOperationalManagerRequestOnly(IReadOnlyCollection<string> keys)
    {
        return !keys.Any(OperationalManagerRequestOnlyRemovedKeys.Contains);
    }
}

public sealed record AccessModelDefaultsSnapshot(
    bool IsConfigured,
    string OperationalManagerScopeBehavior,
    IReadOnlyList<string> CompanyOwnerPermissionKeys,
    IReadOnlyList<string> SeniorManagerPermissionKeys,
    IReadOnlyList<string> OperationalManagerPermissionKeys,
    IReadOnlyList<string> StaffPermissionKeys,
    bool OperationalManagersCanDraftChecklistChanges,
    bool OperationalManagersCanEditRegisters,
    bool OperationalManagersCanApproveStockOrders,
    bool OperationalManagersRequestOnlyMode);
