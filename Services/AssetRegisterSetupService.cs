using vector_app_local.Models;

namespace vector_app_local.Services;

public class AssetRegisterSetupService
{
    public const string ChoiceManualNow = "manual-now";
    public const string ChoiceImportLater = "import-later";
    public const string ChoiceDefer = "defer";

    private static readonly IReadOnlyList<AssetRegisterChoiceOption> ChoiceOptions =
    [
        new(ChoiceManualNow, "Manually build now", "Use the app forms and registers to create records directly."),
        new(ChoiceImportLater, "Import later", "Keep this register empty for now and import client source files in a later step."),
        new(ChoiceDefer, "Defer for now", "Skip this register during setup and return to it later.")
    ];

    public IReadOnlyList<AssetRegisterChoiceOption> GetChoiceOptions() => ChoiceOptions;

    public AssetRegisterSetupSnapshot GetSnapshot(Company? company)
    {
        return new AssetRegisterSetupSnapshot(
            company?.AssetRegisterSetupConfigured == true,
            NormalizeChoice(company?.VehicleRegisterSetupChoice),
            NormalizeChoice(company?.EquipmentRegisterSetupChoice),
            NormalizeChoice(company?.StockRegisterSetupChoice),
            NormalizeChoice(company?.MedicationRegisterSetupChoice),
            NormalizeChoice(company?.StaffRegisterSetupChoice),
            NormalizeChoice(company?.StorageLocationSetupChoice),
            company?.AssetRegisterSetupNotes?.Trim());
    }

    public IReadOnlyList<AssetRegisterSetupRow> BuildRows(AssetRegisterSetupSnapshot snapshot)
    {
        return
        [
            BuildRow(
                "Vehicles",
                "Ambulances, response vehicles, callsigns, registration numbers, VINs, assigned areas, subtypes, and schematic source links.",
                snapshot.VehicleRegisterChoice,
                "/AddItem?type=vehicle",
                "/UploadVehicleRegister"),
            BuildRow(
                "Equipment",
                "Serviceable assets such as monitor defibrillators, ventilators, pumps, suction units, and other maintained equipment.",
                snapshot.EquipmentRegisterChoice,
                "/AddItem?type=equipment",
                "/UploadEquipmentRegister"),
            BuildRow(
                "Stock",
                "Disposable stock, consumables, quantities, batches, expiry dates, and storage allocation.",
                snapshot.StockRegisterChoice,
                "/AddItem?type=stock",
                "/UploadStockRegister"),
            BuildRow(
                "Medication",
                "Medication names, schedules, quantities, batches, expiry dates, and storage allocation.",
                snapshot.MedicationRegisterChoice,
                "/AddItem?type=medication",
                "/UploadMedicationRegister"),
            BuildRow(
                "Staff",
                "Staff profiles, clinical qualification/scope, practitioner numbers, licence expiry, CPD status, and assigned areas.",
                snapshot.StaffRegisterChoice,
                "/AddItem?type=staff",
                "/UploadStaffRegister"),
            BuildRow(
                "Storage locations",
                "Stores, cages, cupboards, bases, satellite stores, and other places where assets or stock can be kept.",
                snapshot.StorageLocationChoice,
                "/OperationalStructureSetup",
                null)
        ];
    }

    public static string NormalizeChoice(string? choice)
    {
        return choice?.Trim().ToLowerInvariant() switch
        {
            ChoiceManualNow => ChoiceManualNow,
            ChoiceImportLater => ChoiceImportLater,
            ChoiceDefer => ChoiceDefer,
            _ => ChoiceDefer
        };
    }

    public static string DescribeChoice(string? choice)
    {
        var normalized = NormalizeChoice(choice);
        return ChoiceOptions.First(option => option.Value == normalized).Label;
    }

    public static string? NormalizeNotes(string? notes)
    {
        return string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    private static AssetRegisterSetupRow BuildRow(
        string name,
        string description,
        string choice,
        string manualUrl,
        string? importUrl)
    {
        var normalizedChoice = NormalizeChoice(choice);
        var actionLabel = normalizedChoice switch
        {
            ChoiceManualNow => $"Open {name.ToLowerInvariant()} manual setup",
            ChoiceImportLater when importUrl is not null => $"Open {name.ToLowerInvariant()} import upload",
            ChoiceImportLater => "Deferred import path",
            _ => "No immediate action"
        };

        var actionUrl = normalizedChoice switch
        {
            ChoiceManualNow => manualUrl,
            ChoiceImportLater => importUrl,
            _ => null
        };

        var actionHelp = normalizedChoice switch
        {
            ChoiceManualNow => "This routes to the current manual register-entry path. It does not create records until the user saves a real register item.",
            ChoiceImportLater when importUrl is not null => "This routes to the current source-file upload path. AI/import automation remains a later phase.",
            ChoiceImportLater => "This choice is saved for later import planning. Storage spaces are currently built through Operational Structure setup.",
            _ => "This register is intentionally deferred and can be configured later from setup progress."
        };

        return new AssetRegisterSetupRow(
            name,
            description,
            normalizedChoice,
            DescribeChoice(normalizedChoice),
            actionLabel,
            actionUrl,
            actionHelp);
    }
}

public sealed record AssetRegisterChoiceOption(string Value, string Label, string Description);

public sealed record AssetRegisterSetupSnapshot(
    bool IsConfigured,
    string VehicleRegisterChoice,
    string EquipmentRegisterChoice,
    string StockRegisterChoice,
    string MedicationRegisterChoice,
    string StaffRegisterChoice,
    string StorageLocationChoice,
    string? Notes);

public sealed record AssetRegisterSetupRow(
    string Name,
    string Description,
    string Choice,
    string ChoiceLabel,
    string ActionLabel,
    string? ActionUrl,
    string ActionHelp);
