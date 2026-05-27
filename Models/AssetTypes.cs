namespace vector_app_local.Models;

public static class AssetTypes
{
    public const string Vehicle = "vehicle";
    public const string Equipment = "equipment";
    public const string Stock = "stock";
    public const string Medication = "medication";

    public static string Normalize(string? assetType)
    {
        return assetType?.Trim().ToLowerInvariant() switch
        {
            Vehicle => Vehicle,
            Equipment => Equipment,
            Stock => Stock,
            Medication => Medication,
            _ => Vehicle
        };
    }

    public static string DisplayName(string? assetType)
    {
        return Normalize(assetType) switch
        {
            Equipment => "Equipment",
            Stock => "Stock",
            Medication => "Medication",
            _ => "Vehicle"
        };
    }

    public static string TaskAction(string? assetType)
    {
        return $"Move / Reallocate {DisplayName(assetType)}";
    }
}
