using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public sealed record ImportAccessDecision(bool Allowed, string? Message)
{
    public static ImportAccessDecision Permit() => new(true, null);
    public static ImportAccessDecision Deny(string message) => new(false, message);
}

public sealed class ImportBatchService
{
    private readonly VectorDbContext _db;
    private readonly IUserActionPermissionService _permissions;

    public ImportBatchService(VectorDbContext db, IUserActionPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public async Task<ImportAccessDecision> CanPrepareAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var tier = await _db.Companies
            .AsNoTracking()
            .Where(company => company.Id == user.CompanyId && company.Status == "Active")
            .Select(company => company.SubscriptionTier)
            .SingleOrDefaultAsync(cancellationToken);

        if (!SubscriptionTiers.IsAtLeast(tier, SubscriptionTiers.Pro))
        {
            return ImportAccessDecision.Deny("Guided Excel and CSV importing is available on Pro and higher tiers.");
        }

        return await _permissions.HasPermissionAsync(user, UserActionPermissions.ImportsPrepare, cancellationToken)
            ? ImportAccessDecision.Permit()
            : ImportAccessDecision.Deny("You do not have permission to prepare imports.");
    }

    public async Task<ImportAccessDecision> CanCommitAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var prepare = await CanPrepareAsync(user, cancellationToken);
        if (!prepare.Allowed)
        {
            return prepare;
        }

        return await _permissions.HasPermissionAsync(user, UserActionPermissions.ImportsCommit, cancellationToken)
            ? ImportAccessDecision.Permit()
            : ImportAccessDecision.Deny("You do not have permission to commit imports.");
    }

    public async Task<ImportBatch> CreateUploadedBatchAsync(
        AppUser user,
        AssetFile sourceFile,
        string targetType,
        ImportSourceProfile sourceProfile,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        var access = await CanPrepareAsync(user, cancellationToken);
        if (!access.Allowed)
        {
            throw new UnauthorizedAccessException(access.Message);
        }

        if (sourceFile.CompanyId != user.CompanyId)
        {
            throw new InvalidOperationException("The import source does not belong to the current company.");
        }

        if (!ImportTargetTypes.All.Contains(targetType))
        {
            throw new InvalidOperationException("The selected import target is not supported.");
        }

        var batch = new ImportBatch
        {
            CompanyId = user.CompanyId,
            SourceAssetFile = sourceFile,
            TargetType = targetType,
            Status = ImportBatchStatuses.Uploaded,
            FileHash = sourceProfile.FileHash,
            OriginalFileName = sourceFile.OriginalFileName,
            ParserContractVersion = sourceProfile.ContractVersion,
            SourceProfileJson = sourceProfile.ToJson(),
            WorksheetCount = sourceProfile.WorksheetCount,
            SourceRowCount = sourceProfile.TotalRows,
            CreatedByUserId = user.Id,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        };

        _db.ImportBatches.Add(batch);
        return batch;
    }

    public async Task<ImportBatch?> GetForCurrentTenantAsync(
        AppUser user,
        int importBatchId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ImportBatches
            .AsNoTracking()
            .Include(batch => batch.SourceAssetFile)
            .Include(batch => batch.CreatedByUser)
            .SingleOrDefaultAsync(batch =>
                batch.Id == importBatchId &&
                batch.CompanyId == user.CompanyId &&
                batch.SourceAssetFile != null &&
                batch.SourceAssetFile.CompanyId == user.CompanyId,
                cancellationToken);
    }
}
