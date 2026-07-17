using vector_app_local.Models;

namespace vector_app_local.Services;

public sealed record ImportFieldDefinition(
    string Key,
    string Label,
    string HelpText,
    string TargetProperty,
    string DataType,
    bool IsRequired,
    string RequirementRule,
    int? MaxLength,
    string Normalizer,
    string? OptionSource,
    IReadOnlyList<string> Aliases,
    bool IsDuplicateKey,
    string Example,
    string BlankUpdateBehavior);

public sealed record ImportTargetDefinition(
    string TargetType,
    string Label,
    IReadOnlyList<ImportFieldDefinition> Fields);

public interface IImportFieldRegistry
{
    int ContractVersion { get; }
    IReadOnlyList<ImportTargetDefinition> Targets { get; }
    ImportTargetDefinition? FindTarget(string targetType);
    ImportFieldDefinition? FindField(string fieldKey);
}

public sealed class ImportFieldRegistry : IImportFieldRegistry
{
    public int ContractVersion => 1;

    public IReadOnlyList<ImportTargetDefinition> Targets { get; } =
    [
        Target(ImportTargetTypes.Vehicle, "Vehicles",
            Field("vehicle.registration_number", "Registration number", "The unique vehicle registration.", "RegistrationNumber", "text", true, 120, ["reg", "reg no", "registration", "vehicle registration"], true, "CA 123-456"),
            Field("vehicle.callsign", "Callsign", "Operational callsign linked to the vehicle.", "Callsign", "text", false, 120, ["call sign", "unit", "unit number"], false, "A01"),
            Field("vehicle.function", "Function", "Broad vehicle function.", "VehicleFunction", "tenant-option", false, 80, ["vehicle function", "function", "category"], false, "Ambulance", "vehicle-functions"),
            Field("vehicle.subtype", "Subtype", "Client-defined subtype within the function.", "VehicleSubtype", "tenant-option", false, 120, ["vehicle subtype", "sub type", "type"], false, "Primary operations", "vehicle-subtypes"),
            Field("vehicle.qualification_level", "Qualification level", "Operational qualification or capability level.", "QualificationLevel", "text", false, 120, ["qualification", "capability"], false, "ALS"),
            Field("vehicle.vin", "VIN", "Vehicle identification number.", "VinNumber", "text", false, 120, ["vin", "vin number"], false, "AHT..."),
            Field("vehicle.chassis_number", "Chassis number", "Vehicle chassis number.", "ChassisNumber", "text", false, 120, ["chassis", "chassis no"], false, "CH-001"),
            Field("vehicle.licence_number", "Licence number", "Vehicle licence number.", "LicenseNumber", "text", false, 120, ["license number", "licence no"], false, "LIC-001"),
            Field("vehicle.licence_expiry", "Licence-disc expiry", "Vehicle licence-disc expiry date.", "LicenseDiscExpiryDate", "date", false, null, ["license expiry", "licence expiry", "disc expiry"], false, "2027-06-30"),
            Field("vehicle.last_service", "Last service", "Most recent service date.", "LastServiceDate", "date", false, null, ["last service date"], false, "2026-06-30"),
            Field("vehicle.next_service", "Next service", "Next planned service date.", "NextServiceDate", "date", false, null, ["next service date", "service due"], false, "2026-12-30"),
            Field("vehicle.status", "Status", "Current register status.", "Status", "status", false, 80, ["vehicle status"], false, "Active"),
            Field("vehicle.operational_area", "Operational area", "Existing tenant area assigned to the vehicle.", "CurrentOperationalAreaId", "tenant-reference", false, null, ["area", "base", "region"], false, "North Base", "operational-areas"),
            Field("vehicle.location_detail", "Location detail", "Specific location description.", "CurrentLocationDetail", "text", false, 260, ["location", "parking location"], false, "Bay 2"),
            Field("vehicle.notes", "Notes", "Optional register notes.", "Notes", "text", false, 1200, ["comments", "remarks"], false, "Spare keys at dispatch")),

        Target(ImportTargetTypes.Staff, "Staff",
            Field("staff.full_name", "Full name", "Staff member's full name.", "FullName", "text", true, 160, ["name", "staff name", "employee name"], false, "Jane Doe"),
            Field("staff.email", "Email", "Required profile contact and tenant-unique identity field. Import never grants login access.", "Email", "email", true, 180, ["email address", "work email"], true, "jane@example.org"),
            Field("staff.staff_id", "Staff ID", "Tenant-issued staff identifier.", "StaffIdentifier", "text", false, 80, ["employee id", "staff number", "personnel number"], true, "EMS-001"),
            Field("staff.national_id", "National ID", "National identity number.", "NationalId", "text", false, 120, ["id number", "identity number"], false, "900101..."),
            Field("staff.cell_number", "Cell number", "Mobile contact number.", "CellNumber", "text", false, 80, ["mobile", "phone", "contact number"], false, "+27 82 000 0000"),
            Field("staff.qualification", "Clinical qualification / scope", "Configured clinical qualification or scope.", "QualificationFunction", "tenant-option", false, 120, ["qualification", "scope", "clinical scope"], false, "ILS", "staff-qualifications"),
            Field("staff.practitioner_number", "Practitioner number", "Professional registration number.", "PractitionerNumber", "text", false, 120, ["practice number", "hpcsa number", "registration number"], false, "PR-001"),
            Field("staff.licence_expiry", "Annual licence expiry", "Annual professional licence expiry.", "AnnualLicenseExpiryDate", "date", false, null, ["license expiry", "licence expiry"], false, "2027-03-31"),
            Field("staff.cpd_status", "CPD status", "Current CPD compliance status.", "CpdComplianceStatus", "status", false, 80, ["cpd compliance", "cpd compliance status"], false, "Compliant"),
            Field("staff.cpd_expiry", "CPD expiry", "CPD compliance expiry date.", "CpdComplianceExpiryDate", "date", false, null, ["cpd expiry date"], false, "2027-12-31"),
            Field("staff.operational_area", "Assigned operational area", "Existing tenant area assigned to the profile.", "AssignedOperationalAreaId", "tenant-reference", false, null, ["area", "base", "assigned area"], false, "North Base", "operational-areas"),
            Field("staff.status", "Status", "Profile status only; this does not grant login access.", "Status", "status", false, 80, ["staff status", "employment status"], false, "Active")),

        Target(ImportTargetTypes.Equipment, "Equipment",
            Field("equipment.name", "Equipment name", "Register item name.", "Name", "text", true, 180, ["name", "item", "equipment"], false, "Monitor Defibrillator"),
            Field("equipment.type", "Equipment type", "Equipment grouping type.", "EquipmentType", "text", false, 160, ["type", "category"], false, "Defibrillator"),
            Field("equipment.model", "Model", "Manufacturer model.", "Model", "text", false, 160, ["make model", "equipment model"], false, "LIFEPAK 15"),
            Field("equipment.serial_asset_id", "Serial / asset ID", "Serial number or unique asset identifier.", "SerialOrAssetId", "text", false, 160, ["serial", "serial number", "asset id", "asset number"], true, "LP15-001"),
            Field("equipment.next_service", "Next service", "Next planned service date.", "NextServiceDate", "date", false, null, ["next service date", "service due"], false, "2027-01-15"),
            Field("equipment.battery_required", "Battery required", "Whether the item requires a battery check.", "BatteryRequired", "boolean", false, null, ["battery", "battery check"], false, "Yes"),
            Field("equipment.status", "Status", "Current register status.", "Status", "status", false, 80, ["equipment status"], false, "Active"),
            Field("equipment.operational_area", "Operational area", "Existing tenant area assigned to the item.", "CurrentOperationalAreaId", "tenant-reference", false, null, ["area", "base"], false, "North Base", "operational-areas"),
            Field("equipment.location_detail", "Location detail", "Specific location description.", "CurrentLocationDetail", "text", false, 260, ["location", "allocated location"], false, "A01 cabinet"),
            Field("equipment.notes", "Notes", "Optional register notes.", "Notes", "text", false, 1200, ["comments", "remarks"], false, "Annual calibration required")),

        Target(ImportTargetTypes.Stock, "Stock",
            Field("stock.item_name", "Item name", "Stock item name.", "ItemName", "text", true, 180, ["name", "stock item", "item"], false, "Gauze swabs"),
            Field("stock.quantity", "Quantity", "Current quantity.", "Quantity", "integer", true, null, ["qty", "stock count", "on hand"], false, "20"),
            Field("stock.item_type", "Item type", "Stock item type.", "ItemType", "text", false, 160, ["type"], false, "Consumable"),
            Field("stock.category", "Category", "Stock grouping category.", "StockCategory", "text", false, 160, ["stock category"], false, "Wound care"),
            Field("stock.batch", "Batch", "Manufacturer batch number.", "BatchNumber", "text", false, 160, ["batch number", "lot"], true, "B-1001"),
            Field("stock.minimum_quantity", "Minimum quantity", "Reorder threshold.", "MinimumQuantity", "integer", false, null, ["minimum", "min qty", "reorder level"], false, "10"),
            Field("stock.unit", "Unit", "Unit of measure.", "Unit", "text", false, 80, ["uom", "unit of measure"], false, "pack"),
            Field("stock.expiry", "Expiry", "Stock expiry date.", "ExpiryDate", "date", false, null, ["expiry date", "expires"], false, "2028-06-30"),
            Field("stock.readiness_critical", "Readiness critical", "Whether shortage affects readiness.", "IsReadinessCritical", "boolean", false, null, ["critical", "readiness"], false, "Yes"),
            Field("stock.location", "Location", "Specific storage location.", "Location", "text", false, 260, ["storage", "bin"], false, "Store A / Shelf 2"),
            Field("stock.operational_area", "Operational area", "Existing tenant operational area.", "CurrentOperationalAreaId", "tenant-reference", false, null, ["area", "base"], false, "North Base", "operational-areas"),
            Field("stock.status", "Status", "Current register status.", "Status", "status", false, 80, ["stock status"], false, "Active"),
            Field("stock.notes", "Notes", "Optional register notes.", "Notes", "text", false, 1200, ["comments", "remarks"], false, "Sterile")),

        Target(ImportTargetTypes.Medication, "Medication",
            Field("medication.name", "Medication name", "Medication register name.", "Name", "text", true, 180, ["name", "drug", "medicine"], false, "Adrenaline"),
            Field("medication.code", "Medication code", "Internal or catalogue code.", "MedicationCode", "text", false, 120, ["code", "drug code"], false, "ADR-1"),
            Field("medication.type", "Medication type", "Medication grouping type.", "MedicationType", "text", false, 160, ["type", "category"], false, "Emergency"),
            Field("medication.schedule", "Schedule", "Controlled medicine schedule.", "Schedule", "text", false, 80, ["drug schedule"], false, "S4"),
            Field("medication.batch", "Batch", "Manufacturer batch number.", "BatchNumber", "text", false, 160, ["batch number", "lot"], true, "M-1001"),
            Field("medication.storage_location", "Storage location", "Specific storage location.", "StorageLocation", "text", false, 260, ["location", "storage"], false, "Drug cupboard"),
            Field("medication.operational_area", "Operational area", "Existing tenant operational area.", "CurrentOperationalAreaId", "tenant-reference", false, null, ["area", "base"], false, "North Base", "operational-areas"),
            Field("medication.status", "Status", "Current register status.", "Status", "status", false, 80, ["medication status"], false, "Active"),
            Field("medication.quantity", "Quantity", "Current quantity.", "Quantity", "integer", false, null, ["qty", "stock count"], false, "10"),
            Field("medication.expiry", "Expiry", "Medication expiry date.", "ExpiryDate", "date", false, null, ["expiry date", "expires"], false, "2028-06-30"),
            Field("medication.notes", "Notes", "Optional register notes.", "Notes", "text", false, 1200, ["comments", "remarks"], false, "Refrigerate")),

        Target(ImportTargetTypes.OperationalArea, "Operational areas",
            Field("area.name", "Name", "Operational area name.", "Name", "text", true, 180, ["area", "area name", "base", "region"], true, "North Base"),
            Field("area.type", "Area type", "Configured operational structure type.", "AreaType", "tenant-option", true, 120, ["type", "structure type"], false, "Base", "area-types"),
            Field("area.parent", "Parent area", "Existing tenant parent area.", "ParentOperationalAreaId", "tenant-reference", false, null, ["parent", "region", "parent area"], false, "North Region", "operational-areas"),
            Field("area.address", "Address", "Physical address.", "Address", "text", false, 360, ["location address"], false, "1 Main Road"),
            Field("area.status", "Status", "Current area status.", "Status", "status", false, 80, ["area status"], false, "Active"),
            Field("area.notes", "Notes", "Optional area notes.", "Notes", "text", false, 1200, ["comments", "remarks"], false, "24-hour base")),

        Target(ImportTargetTypes.StorageLocation, "Storage locations",
            Field("storage.name", "Name", "Storage location name.", "Name", "text", true, 180, ["location name", "store", "storage"], true, "Main storeroom"),
            Field("storage.operational_area", "Operational area", "Existing tenant operational area.", "OperationalAreaId", "tenant-reference", true, null, ["area", "base"], false, "North Base", "operational-areas"),
            Field("storage.type", "Storage type", "Storage location type.", "StorageType", "text", false, 120, ["type"], false, "Medication store"),
            Field("storage.status", "Status", "Current storage status.", "Status", "status", false, 80, ["storage status"], false, "Active"),
            Field("storage.notes", "Notes", "Optional storage notes.", "Notes", "text", false, 1200, ["comments", "remarks"], false, "Restricted access")),

        new ImportTargetDefinition(ImportTargetTypes.Checklist, "Checklist", Array.Empty<ImportFieldDefinition>())
    ];

    public ImportTargetDefinition? FindTarget(string targetType) =>
        Targets.FirstOrDefault(target => string.Equals(target.TargetType, targetType, StringComparison.OrdinalIgnoreCase));

    public ImportFieldDefinition? FindField(string fieldKey) =>
        Targets.SelectMany(target => target.Fields)
            .FirstOrDefault(field => string.Equals(field.Key, fieldKey, StringComparison.OrdinalIgnoreCase));

    private static ImportTargetDefinition Target(string targetType, string label, params ImportFieldDefinition[] fields) =>
        new(targetType, label, fields);

    private static ImportFieldDefinition Field(
        string key,
        string label,
        string helpText,
        string targetProperty,
        string dataType,
        bool required,
        int? maxLength,
        string[] aliases,
        bool duplicateKey,
        string example,
        string? optionSource = null,
        string? requirementRule = null) =>
        new(key, label, helpText, targetProperty, dataType, required, requirementRule ?? (required ? "Required" : "Optional"), maxLength, "trim", optionSource, aliases, duplicateKey, example, "PreserveExisting");
}
