using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

internal static class TestEntitlementHelper
{
    public static async Task GrantAsync(VectorDbContext db, int companyId, string tier = SubscriptionTiers.Premium)
    {
        var existing = await db.PilotEntitlements.SingleOrDefaultAsync(item => item.CompanyId == companyId);
        if (existing is null)
        {
            db.PilotEntitlements.Add(new PilotEntitlement
            {
                CompanyId = companyId,
                Tier = tier,
                Status = PilotEntitlementStatuses.Active,
                StartsAtUtc = DateTime.UtcNow.AddDays(-1),
                ExpiresAtUtc = DateTime.UtcNow.AddYears(1),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Tier = tier;
            existing.Status = PilotEntitlementStatuses.Active;
            existing.StartsAtUtc = DateTime.UtcNow.AddDays(-1);
            existing.ExpiresAtUtc = DateTime.UtcNow.AddYears(1);
            existing.RevokedAtUtc = null;
            existing.RevokedByUserId = null;
            existing.RevocationReason = null;
        }

        await db.SaveChangesAsync();
    }

    public static async Task RevokeAsync(VectorDbContext db, int companyId)
    {
        var entitlement = await db.PilotEntitlements.SingleAsync(item => item.CompanyId == companyId);
        entitlement.Status = PilotEntitlementStatuses.Revoked;
        entitlement.RevokedAtUtc = DateTime.UtcNow;
        entitlement.RevocationReason = "Test downgrade boundary";
        entitlement.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
