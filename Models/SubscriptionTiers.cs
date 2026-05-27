namespace vector_app_local.Models;

public static class SubscriptionTiers
{
    public const string Base = "Base";
    public const string Pro = "Pro";
    public const string Premium = "Premium";

    public static string Normalize(string? tier)
    {
        return tier?.Trim() switch
        {
            Pro => Pro,
            Premium => Premium,
            _ => Base
        };
    }

    public static bool IsAtLeast(string? tier, string minimumTier)
    {
        return Rank(tier) >= Rank(minimumTier);
    }

    private static int Rank(string? tier)
    {
        return Normalize(tier) switch
        {
            Premium => 3,
            Pro => 2,
            _ => 1
        };
    }
}
