namespace vector_app_local.Services;

public static class VehicleSchematicLibrary
{
    public static IReadOnlyList<VehicleSchematicDefinition> All { get; } =
    [
        new(
            "operational-ambulance",
            "Ambulance",
            "Operational Ambulance",
            "Operational Ambulance Box Body",
            "Box-body ambulance",
            "Mercedes-Benz / Ford / Toyota ambulance layouts",
            "Published",
            "Used for frontline response ambulance inspections",
            ["Left", "Right", "Front", "Rear", "Top"]),
        new(
            "ift-ambulance",
            "Ambulance",
            "IFT Ambulance",
            "Inter-Facility Transfer Ambulance",
            "Patient transfer ambulance",
            "High-roof van and box-body transfer layouts",
            "Published",
            "Used for IFT ambulance inspections",
            ["Left", "Right", "Front", "Rear", "Top"]),
        new(
            "icu-ambulance",
            "Ambulance",
            "ICU Ambulance",
            "ICU Ambulance Box Body",
            "Critical care ambulance",
            "Large box-body intensive care layouts",
            "Published",
            "Used for ICU ambulance inspections",
            ["Left", "Right", "Front", "Rear", "Top"]),
        new(
            "response-pickup",
            "Response Vehicle",
            "Pickup",
            "Response Pickup",
            "Pickup response vehicle",
            "Toyota Hilux / Ford Ranger / similar",
            "Published",
            "Used for supervisor and rapid response pickups",
            ["Left", "Right", "Front", "Rear", "Top"]),
        new(
            "response-sedan",
            "Response Vehicle",
            "Sedan",
            "Response Sedan",
            "Sedan response vehicle",
            "Toyota Camry / Nissan Altima / similar",
            "Published",
            "Used for sedan response and command vehicles",
            ["Left", "Right", "Front", "Rear", "Top"]),
        new(
            "rescue-vehicle",
            "Rescue Vehicle",
            "Rescue Unit",
            "Rescue Unit",
            "Rescue support vehicle",
            "Box rescue / technical rescue layouts",
            "Draft",
            "Prepared for rescue vehicle inspections after approval",
            ["Left", "Right", "Front", "Rear", "Top"])
    ];

    public static IReadOnlyList<VehicleSchematicDefinition> Published =>
        All.Where(schematic => schematic.IsPublished).ToList();

    public static VehicleSchematicDefinition? Find(string key)
    {
        return All.FirstOrDefault(schematic => string.Equals(schematic.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    public static VehicleSchematicDefinition Require(string key)
    {
        return Find(key) ?? throw new InvalidOperationException($"Vehicle schematic '{key}' is not configured.");
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
    IReadOnlyList<string> Views)
{
    public bool IsPublished => string.Equals(Status, "Published", StringComparison.OrdinalIgnoreCase);
}
