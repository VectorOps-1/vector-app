using System.ComponentModel.DataAnnotations;

namespace vector_app_local.Models;

public class PilotEntitlement
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(40)]
    public string Tier { get; set; } = SubscriptionTiers.Premium;

    [MaxLength(24)]
    public string Status { get; set; } = PilotEntitlementStatuses.Active;

    public DateTime StartsAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }

    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public int? RevokedByUserId { get; set; }
    public AppUser? RevokedByUser { get; set; }

    [MaxLength(500)]
    public string? RevocationReason { get; set; }

    [MaxLength(64)]
    public string ConcurrencyToken { get; set; } = Guid.NewGuid().ToString("N");

    public ICollection<PilotEntitlementEvent> Events { get; set; } = new List<PilotEntitlementEvent>();
}

public class PilotEntitlementEvent
{
    public int Id { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public int PilotEntitlementId { get; set; }
    public PilotEntitlement? PilotEntitlement { get; set; }

    public int? ActorUserId { get; set; }
    public AppUser? ActorUser { get; set; }

    [MaxLength(40)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(24)]
    public string? PreviousStatus { get; set; }

    [MaxLength(24)]
    public string NewStatus { get; set; } = string.Empty;

    public DateTime? PreviousExpiresAtUtc { get; set; }
    public DateTime NewExpiresAtUtc { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public static class PilotEntitlementStatuses
{
    public const string Active = "Active";
    public const string Revoked = "Revoked";
    public const string Pending = "Pending";
    public const string Expired = "Expired";
}

public static class PilotEntitlementEventTypes
{
    public const string Activated = "Activated";
    public const string Reactivated = "Reactivated";
    public const string Extended = "Extended";
    public const string Revoked = "Revoked";
}
