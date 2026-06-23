using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class VehicleSchematicLibraryModel : PageModel
{
    private static readonly IReadOnlyList<string> OrderedCategories = ["Ambulance", "Response Vehicle", "Custom"];

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly VehicleSchematicAssignmentService _schematicAssignments;
    private readonly VehicleStructureSetupService _vehicleStructure;

    public VehicleSchematicLibraryModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        VehicleSchematicAssignmentService schematicAssignments,
        VehicleStructureSetupService vehicleStructure)
    {
        _db = db;
        _currentUser = currentUser;
        _schematicAssignments = schematicAssignments;
        _vehicleStructure = vehicleStructure;
    }

    public IReadOnlyList<VehicleSchematicDefinition> Schematics => VehicleSchematicLibrary.All;
    public int PublishedCount => Schematics.Count(schematic => schematic.IsPublished);
    public IReadOnlyList<string> Categories => OrderedCategories;
    public IReadOnlyList<string> FunctionOptions { get; private set; } = [];
    public IReadOnlyList<VehicleSubtypeOption> SubtypeOptions { get; private set; } = [];
    public IReadOnlyList<OperationalAreaOption> OperationalAreaOptions { get; private set; } = [];
    public IReadOnlyList<VehicleOption> VehicleOptions { get; private set; } = [];
    public Dictionary<string, IReadOnlyList<SchematicAssignmentView>> AssignmentsBySchematicKey { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        await LoadAssignmentDataAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignFunctionAsync(string schematicKey, string vehicleFunction)
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var schematic = VehicleSchematicLibrary.Find(schematicKey);
        if (schematic is null)
        {
            StatusMessage = "Select a valid unit schematic before assigning it.";
            return RedirectToPage();
        }

        var normalizedFunction = Normalize(vehicleFunction);
        if (normalizedFunction is null)
        {
            StatusMessage = "Select a function before assigning the schematic.";
            return RedirectToPage();
        }

        var functionExists = await _db.VehicleFunctionSetups.AnyAsync(item =>
            item.CompanyId == currentUser.CompanyId &&
            item.Status == "Active" &&
            item.Name == normalizedFunction);
        if (!functionExists)
        {
            StatusMessage = "Select a configured vehicle function before assigning the schematic.";
            return RedirectToPage();
        }

        await _schematicAssignments.AssignFunctionAsync(currentUser.CompanyId, currentUser.Id, normalizedFunction, schematic.Key);
        AddAuditLog(currentUser, "Unit schematic function assignment", $"{currentUser.FullName} assigned {schematic.DisplayName} to function {normalizedFunction}.");
        await _db.SaveChangesAsync();

        StatusMessage = $"{schematic.DisplayName} assigned to {normalizedFunction}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignSubtypeAsync(string schematicKey, string subtypeSelection)
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var schematic = VehicleSchematicLibrary.Find(schematicKey);
        if (schematic is null)
        {
            StatusMessage = "Select a valid unit schematic before assigning it.";
            return RedirectToPage();
        }

        var (vehicleFunction, vehicleSubtype) = ParseSubtypeSelection(subtypeSelection);
        if (vehicleSubtype is null)
        {
            StatusMessage = "Select a subtype before assigning the schematic.";
            return RedirectToPage();
        }

        var subtypeExists = await _db.VehicleSubtypeSetups
            .Include(item => item.VehicleFunctionSetup)
            .AnyAsync(item =>
                item.CompanyId == currentUser.CompanyId &&
                item.Status == "Active" &&
                item.Name == vehicleSubtype &&
                item.VehicleFunctionSetup != null &&
                item.VehicleFunctionSetup.Status == "Active" &&
                (vehicleFunction == null || item.VehicleFunctionSetup.Name == vehicleFunction));
        if (!subtypeExists)
        {
            StatusMessage = "Select a configured vehicle subtype before assigning the schematic.";
            return RedirectToPage();
        }

        await _schematicAssignments.AssignSubtypeAsync(currentUser.CompanyId, currentUser.Id, vehicleFunction, vehicleSubtype, schematic.Key);
        AddAuditLog(currentUser, "Unit schematic subtype assignment", $"{currentUser.FullName} assigned {schematic.DisplayName} to subtype {vehicleSubtype}.");
        await _db.SaveChangesAsync();

        StatusMessage = $"{schematic.DisplayName} assigned to {vehicleSubtype}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignAreaAsync(string schematicKey, int operationalAreaId)
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var schematic = VehicleSchematicLibrary.Find(schematicKey);
        if (schematic is null)
        {
            StatusMessage = "Select a valid unit schematic before assigning it.";
            return RedirectToPage();
        }

        var area = await _db.OperationalAreas
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.CompanyId == currentUser.CompanyId &&
                item.Id == operationalAreaId &&
                item.Status != "Inactive");
        if (area is null)
        {
            StatusMessage = "Select an active area before assigning the schematic.";
            return RedirectToPage();
        }

        await _schematicAssignments.AssignAreaAsync(currentUser.CompanyId, currentUser.Id, area.Id, schematic.Key);
        AddAuditLog(currentUser, "Unit schematic area assignment", $"{currentUser.FullName} assigned {schematic.DisplayName} to area {area.Name}.");
        await _db.SaveChangesAsync();

        StatusMessage = $"{schematic.DisplayName} assigned to {area.Name}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignVehicleAsync(string schematicKey, int vehicleId)
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var schematic = VehicleSchematicLibrary.Find(schematicKey);
        if (schematic is null)
        {
            StatusMessage = "Select a valid unit schematic before assigning it.";
            return RedirectToPage();
        }

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(item =>
            item.CompanyId == currentUser.CompanyId &&
            item.Id == vehicleId);
        if (vehicle is null)
        {
            StatusMessage = "Select a registered vehicle before assigning the schematic.";
            return RedirectToPage();
        }

        await _schematicAssignments.AssignVehicleAsync(currentUser.CompanyId, currentUser.Id, vehicle.Id, schematic.Key);
        AddAuditLog(currentUser, "Unit schematic vehicle override", $"{currentUser.FullName} assigned {schematic.DisplayName} directly to {vehicle.Callsign} / {vehicle.RegistrationNumber}.");
        await _db.SaveChangesAsync();

        StatusMessage = $"{schematic.DisplayName} assigned directly to {vehicle.Callsign} / {vehicle.RegistrationNumber}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnassignAsync(int? assignmentId)
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (!assignmentId.HasValue)
        {
            StatusMessage = "Select an exact schematic assignment link to unassign.";
            return RedirectToPage();
        }

        var assignment = await _db.VehicleSchematicAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.CompanyId == currentUser.CompanyId &&
                item.Id == assignmentId.Value);
        if (assignment is null)
        {
            StatusMessage = "Schematic assignment link was not found.";
            return RedirectToPage();
        }

        var areas = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == currentUser.CompanyId)
            .ToListAsync();
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle =>
                vehicle.CompanyId == currentUser.CompanyId &&
                vehicle.Status != "Deleted")
            .ToListAsync();
        var schematicName = VehicleSchematicLibrary.Find(assignment.SchematicKey)?.DisplayName ?? assignment.SchematicKey;
        var scopeLabel = AssignmentLabel(assignment, areas, vehicles);

        var removed = await _schematicAssignments.UnassignAssignmentAsync(currentUser.CompanyId, assignment.Id);
        if (!removed)
        {
            StatusMessage = "Schematic assignment link was not found.";
            return RedirectToPage();
        }

        AddAuditLog(currentUser, "Unit schematic assignment unassigned", $"{currentUser.FullName} unassigned {schematicName} from {assignment.ScopeType}: {scopeLabel}.");
        await _db.SaveChangesAsync();
        StatusMessage = $"{schematicName} unassigned from {scopeLabel}.";

        return RedirectToPage();
    }

    public IReadOnlyList<VehicleSchematicDefinition> SchematicsForCategory(string category)
    {
        return Schematics
            .Where(schematic => string.Equals(schematic.Category, category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(schematic => schematic.Subtype)
            .ThenBy(schematic => schematic.DisplayName)
            .ToList();
    }

    public IReadOnlyList<SchematicAssignmentView> AssignmentsFor(string schematicKey)
    {
        return AssignmentsBySchematicKey.TryGetValue(schematicKey, out var assignments)
            ? assignments
            : [];
    }

    private async Task LoadAssignmentDataAsync(int companyId)
    {
        var vehicleStructure = await _vehicleStructure.GetSnapshotAsync(companyId);
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle =>
                vehicle.CompanyId == companyId &&
                vehicle.Status != "Deleted")
            .OrderBy(vehicle => vehicle.VehicleFunction)
            .ThenBy(vehicle => vehicle.VehicleSubtype)
            .ThenBy(vehicle => vehicle.Callsign)
            .ToListAsync();

        FunctionOptions = vehicleStructure.Functions
            .Select(function => function.Name)
            .ToList();

        SubtypeOptions = vehicleStructure.Subtypes
            .Select(subtype => new VehicleSubtypeOption(
                subtype.FunctionName,
                subtype.Name,
                vehicles.Count(vehicle =>
                    string.Equals(
                        Normalize(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType),
                        subtype.FunctionName,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        Normalize(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType),
                        subtype.Name,
                        StringComparison.OrdinalIgnoreCase))))
            .ToList();

        VehicleOptions = vehicles
            .Select(vehicle => new VehicleOption(
                vehicle.Id,
                vehicle.Callsign,
                vehicle.RegistrationNumber,
                Normalize(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType),
                Normalize(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType)))
            .ToList();

        var operationalAreas = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area =>
                area.CompanyId == companyId &&
                area.Status != "Inactive")
            .OrderBy(area => area.Name)
            .ToListAsync();

        OperationalAreaOptions = operationalAreas
            .Select(area => new OperationalAreaOption(
                area.Id,
                area.Name,
                area.AreaType,
                vehicles.Count(vehicle => vehicle.CurrentOperationalAreaId == area.Id)))
            .ToList();

        var scopedAssignments = await _db.VehicleSchematicAssignments
            .AsNoTracking()
            .Where(assignment => assignment.CompanyId == companyId)
            .ToListAsync();

        var assignmentRows = new List<SchematicAssignmentView>();
        foreach (var assignment in scopedAssignments)
        {
            var schematic = VehicleSchematicLibrary.Find(assignment.SchematicKey);
            if (schematic is null)
            {
                continue;
            }

            var matchingVehicles = vehicles.Where(vehicle => AssignmentMatchesVehicle(assignment, vehicle)).ToList();
            var scopeLabel = AssignmentLabel(assignment, operationalAreas, vehicles);
            assignmentRows.Add(new SchematicAssignmentView(
                assignment.Id,
                schematic.Key,
                assignment.ScopeType,
                scopeLabel,
                AffectedVehicleSummary(matchingVehicles),
                matchingVehicles.Count,
                AffectedCallsignSummary(matchingVehicles),
                "Explicit assignment rule",
                assignment.OperationalAreaId,
                assignment.VehicleId,
                assignment.VehicleFunction,
                assignment.VehicleSubtype));
        }

        AssignmentsBySchematicKey = assignmentRows
            .GroupBy(item => item.SchematicKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SchematicAssignmentView>)group
                    .OrderBy(item => ScopeSort(item.ScopeType))
                    .ThenBy(item => item.ScopeLabel)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    private async Task<AppUser?> RequireCurrentUserAsync()
    {
        return await _currentUser.GetCurrentUserAsync();
    }

    private void AddAuditLog(AppUser currentUser, string action, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = action,
            EntityType = "VehicleSchematicAssignment",
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static bool AssignmentMatchesVehicle(VehicleSchematicAssignment assignment, Vehicle vehicle)
    {
        var vehicleFunction = Normalize(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        var vehicleSubtype = Normalize(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);

        return assignment.ScopeType switch
        {
            VehicleSchematicAssignmentService.VehicleScope => assignment.VehicleId == vehicle.Id,
            VehicleSchematicAssignmentService.AreaScope => assignment.OperationalAreaId.HasValue &&
                assignment.OperationalAreaId == vehicle.CurrentOperationalAreaId,
            VehicleSchematicAssignmentService.FunctionScope => string.Equals(assignment.VehicleFunction, vehicleFunction, StringComparison.OrdinalIgnoreCase),
            VehicleSchematicAssignmentService.SubtypeScope => string.Equals(assignment.VehicleSubtype, vehicleSubtype, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(assignment.VehicleFunction) || string.Equals(assignment.VehicleFunction, vehicleFunction, StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static string AssignmentLabel(
        VehicleSchematicAssignment assignment,
        IReadOnlyList<OperationalArea> operationalAreas,
        IReadOnlyList<Vehicle> vehicles)
    {
        if (assignment.ScopeType == VehicleSchematicAssignmentService.FunctionScope)
        {
            return assignment.VehicleFunction ?? "Unassigned function";
        }

        if (assignment.ScopeType == VehicleSchematicAssignmentService.SubtypeScope)
        {
            return string.IsNullOrWhiteSpace(assignment.VehicleFunction)
                ? assignment.VehicleSubtype ?? "Unassigned subtype"
                : $"{assignment.VehicleFunction} / {assignment.VehicleSubtype}";
        }

        if (assignment.ScopeType == VehicleSchematicAssignmentService.AreaScope)
        {
            return operationalAreas.FirstOrDefault(area => area.Id == assignment.OperationalAreaId)?.Name
                ?? "Unknown area";
        }

        if (assignment.ScopeType == VehicleSchematicAssignmentService.VehicleScope)
        {
            var vehicle = vehicles.FirstOrDefault(item => item.Id == assignment.VehicleId);
            return vehicle is null
                ? "Unknown vehicle"
                : $"{vehicle.Callsign} / {vehicle.RegistrationNumber}";
        }

        return "Assignment";
    }

    private static string AffectedVehicleSummary(IReadOnlyList<Vehicle> vehicles)
    {
        if (vehicles.Count == 0)
        {
            return "No registered vehicles are currently affected by this explicit rule.";
        }

        return $"{vehicles.Count} registered vehicle(s) currently affected by this rule.";
    }

    private static string AffectedCallsignSummary(IReadOnlyList<Vehicle> vehicles)
    {
        var callsigns = vehicles
            .Select(vehicle => vehicle.Callsign)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Take(5)
            .ToList();

        return callsigns.Count == 0
            ? string.Empty
            : $"Affected callsigns: {string.Join(", ", callsigns)}";
    }

    private static (string? Function, string? Subtype) ParseSubtypeSelection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, null);
        }

        var parts = value.Split("||", StringSplitOptions.None);
        if (parts.Length == 1)
        {
            return (null, Normalize(parts[0]));
        }

        return (Normalize(parts[0]), Normalize(parts[1]));
    }

    private static int ScopeSort(string scopeType) => scopeType switch
    {
        VehicleSchematicAssignmentService.VehicleScope => 1,
        VehicleSchematicAssignmentService.AreaScope => 2,
        VehicleSchematicAssignmentService.SubtypeScope => 3,
        VehicleSchematicAssignmentService.FunctionScope => 4,
        _ => 5
    };

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed record VehicleSubtypeOption(string? Function, string Subtype, int VehicleCount)
    {
        public string Value => $"{Function ?? string.Empty}||{Subtype}";
        public string Label => string.IsNullOrWhiteSpace(Function)
            ? $"{Subtype} ({VehicleCount})"
            : $"{Function} / {Subtype} ({VehicleCount})";
    }

    public sealed record VehicleOption(int Id, string Callsign, string Registration, string? Function, string? Subtype)
    {
        public string Label => $"{Callsign} / {Registration} - {Function ?? "Unassigned"} / {Subtype ?? "Unassigned"}";
    }

    public sealed record OperationalAreaOption(int Id, string Name, string AreaType, int VehicleCount)
    {
        public string Label => $"{Name} ({AreaType}) - {VehicleCount} vehicle{(VehicleCount == 1 ? string.Empty : "s")}";
    }

    public sealed record SchematicAssignmentView(
        int AssignmentId,
        string SchematicKey,
        string ScopeType,
        string ScopeLabel,
        string Detail,
        int VehicleCount,
        string ExampleCallsigns,
        string AssignmentState,
        int? OperationalAreaId,
        int? VehicleId,
        string? VehicleFunction,
        string? VehicleSubtype);
}
