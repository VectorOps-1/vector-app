using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public interface IUserActionAuthorizationService
{
    Task<bool> HasPermissionAsync(AppUser user, string permissionKey, CancellationToken cancellationToken = default);
    Task<bool> CanManageAreaScopedRecordAsync(AppUser user, string permissionKey, int? operationalAreaId, CancellationToken cancellationToken = default);
    Task<bool> CanCompleteMovementTaskAsync(AppUser user, int? taskId, string assetType, int assetId, CancellationToken cancellationToken = default);
}

public class UserActionAuthorizationService : IUserActionAuthorizationService
{
    private readonly VectorDbContext _db;
    private readonly IUserActionPermissionService _permissions;

    public UserActionAuthorizationService(VectorDbContext db, IUserActionPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public Task<bool> HasPermissionAsync(AppUser user, string permissionKey, CancellationToken cancellationToken = default)
    {
        return _permissions.HasPermissionAsync(user, permissionKey, cancellationToken);
    }

    public async Task<bool> CanManageAreaScopedRecordAsync(
        AppUser user,
        string permissionKey,
        int? operationalAreaId,
        CancellationToken cancellationToken = default)
    {
        if (!await _permissions.HasPermissionAsync(user, permissionKey, cancellationToken))
        {
            return false;
        }

        if (CurrentUserService.IsSeniorAccessRole(user.AppRole?.Name))
        {
            return true;
        }

        if (!string.Equals(user.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!operationalAreaId.HasValue)
        {
            return false;
        }

        var assignedAreaIds = await LoadAssignedAreaIdsAsync(user, cancellationToken);
        return assignedAreaIds.Contains(operationalAreaId.Value);
    }

    public async Task<bool> CanCompleteMovementTaskAsync(
        AppUser user,
        int? taskId,
        string assetType,
        int assetId,
        CancellationToken cancellationToken = default)
    {
        if (!taskId.HasValue || assetId <= 0)
        {
            return false;
        }

        var normalizedAssetType = AssetTypes.Normalize(assetType);
        var assetIdText = assetId.ToString();

        return await _db.TaskItems
            .AsNoTracking()
            .AnyAsync(task =>
                task.Id == taskId.Value &&
                task.CompanyId == user.CompanyId &&
                task.AssignedToUserId == user.Id &&
                task.Status == "Open" &&
                task.RelatedItemReference != null &&
                task.RelatedItemReference.StartsWith(normalizedAssetType + "|") &&
                EF.Functions.Like(task.RelatedItemReference, normalizedAssetType + "|" + assetIdText + "|%"),
                cancellationToken);
    }

    private async Task<HashSet<int>> LoadAssignedAreaIdsAsync(AppUser user, CancellationToken cancellationToken)
    {
        var assignedAreaIds = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == user.CompanyId &&
                assignment.ManagerUserId == user.Id &&
                assignment.Status == "Active")
            .Select(assignment => assignment.OperationalAreaId)
            .ToListAsync(cancellationToken);

        if (user.AssignedOperationalAreaId.HasValue)
        {
            assignedAreaIds.Add(user.AssignedOperationalAreaId.Value);
        }

        return assignedAreaIds.ToHashSet();
    }
}
