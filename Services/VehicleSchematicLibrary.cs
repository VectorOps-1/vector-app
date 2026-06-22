namespace vector_app_local.Services;

public static class VehicleSchematicLibrary
{
    public static IReadOnlyList<VehicleSchematicDefinition> All { get; } =
    [
        new(
            "pickup-rv",
            "Response Vehicle",
            "Toyota Hilux Supercab",
            "Toyota Hilux Supercab",
            "Supercab pickup response vehicle",
            "Toyota Hilux Supercab / Extra Cab with emergency roof light bar",
            "Published",
            "Used for response vehicle and supervisor pickup inspections",
            [
                new("left", "Left", "/images/schematics/toyota-hilux-supercab/views/left.png"),
                new("right", "Right", "/images/schematics/toyota-hilux-supercab/views/right.png"),
                new("front", "Front", "/images/schematics/toyota-hilux-supercab/views/front.png"),
                new("rear", "Rear", "/images/schematics/toyota-hilux-supercab/views/rear.png")
            ],
            "/images/schematics/toyota-hilux-supercab/library/toyota-hilux-supercab-library-collage.png"),
        new(
            "toyota-quantum-hiace-high-roof",
            "Ambulance",
            "Toyota",
            "Toyota Quantum / HiAce High Roof",
            "High-roof ambulance van",
            "Toyota Quantum High Roof / Toyota HiAce H300 ambulance with emergency roof light bar",
            "Published",
            "Used for operational ambulance damage and daily readiness inspections",
            [
                new("left", "Left", "/images/schematics/toyota-quantum-hiace-high-roof/views/left.png"),
                new("right", "Right", "/images/schematics/toyota-quantum-hiace-high-roof/views/right.png"),
                new("front", "Front", "/images/schematics/toyota-quantum-hiace-high-roof/views/front.png"),
                new("rear", "Rear", "/images/schematics/toyota-quantum-hiace-high-roof/views/rear.png")
            ],
            "/images/schematics/toyota-quantum-hiace-high-roof/library/toyota-quantum-hiace-high-roof-library-collage.png")
    ];

    public static IReadOnlyList<VehicleSchematicDefinition> Published =>
        All.Where(schematic => schematic.IsPublished).ToList();

    public static VehicleSchematicDefinition? Find(string key)
    {
        var normalizedKey = NormalizeKey(key);
        return All.FirstOrDefault(schematic => string.Equals(schematic.Key, normalizedKey, StringComparison.OrdinalIgnoreCase));
    }

    public static VehicleSchematicDefinition Require(string key)
    {
        return Find(key) ?? throw new InvalidOperationException($"Unit schematic '{key}' is not configured.");
    }

    private static string NormalizeKey(string? key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
    }
}

public sealed record VehicleSchematicDefinition(
    string Key,
    string Category,
    string Subtype,
    string DisplayName,
    string BodyStyle,
    string MakeModel,
    string Status,
    string LiveUse,
    IReadOnlyList<VehicleSchematicViewDefinition> Views,
    string? LibraryAssetPath = null)
{
    public bool IsPublished => string.Equals(Status, "Published", StringComparison.OrdinalIgnoreCase);
}

public sealed record VehicleSchematicViewDefinition(string Key, string Label, string AssetPath);
