using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class AuditTrailService
{
    private readonly VectorDbContext _db;

    public AuditTrailService(VectorDbContext db)
    {
        _db = db;
    }

    public void Record(
        AppUser actor,
        string action,
        string entityType,
        int? entityId = null,
        string? details = null,
        DateTime? createdAtUtc = null)
    {
        Record(_db, actor.CompanyId, actor.Id, action, entityType, entityId, details, createdAtUtc);
    }

    public void Record(
        int companyId,
        int? appUserId,
        string action,
        string entityType,
        int? entityId = null,
        string? details = null,
        DateTime? createdAtUtc = null)
    {
        Record(_db, companyId, appUserId, action, entityType, entityId, details, createdAtUtc);
    }

    public static void Record(
        VectorDbContext db,
        int companyId,
        int? appUserId,
        string action,
        string entityType,
        int? entityId = null,
        string? details = null,
        DateTime? createdAtUtc = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            CompanyId = companyId,
            AppUserId = appUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        });
    }

    public async Task RecordAndSaveAsync(
        int companyId,
        int? appUserId,
        string action,
        string entityType,
        int? entityId = null,
        string? details = null,
        DateTime? createdAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        Record(companyId, appUserId, action, entityType, entityId, details, createdAtUtc);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
