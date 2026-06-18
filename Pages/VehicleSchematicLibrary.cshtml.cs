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

    public VehicleSchematicLibraryModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        VehicleSchematicAssignmentService schematicAssignments)
    {
        _db = db;
        _currentUser = currentUser;
        _schematicAssignments = schematicAssignments;
    }

    public IReadOnlyList<VehicleSchematicDefinition> Schematics => VehicleSchematicLibrary.All;
    public int PublishedCount => Schematics.Count(schematic => schematic.IsPublished);
    public IReadOnlyList<string> Categories => OrderedCategories;
    public IReadOnlyList<string> FunctionOptions { get; private set; } = [];
    public IReadOnlyList<VehicleSubtypeOption> SubtypeOptions { get; private set; } = [];
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

        await _schematicAssignments.AssignSubtypeAsync(currentUser.CompanyId, currentUser.Id, vehicleFunction, vehicleSubtype, schematic.Key);
        AddAuditLog(currentUser, "Unit schematic subtype assignment", $"{currentUser.FullName} assigned {schematic.DisplayName} to subtype {vehicleSubtype}.");
        await _db.SaveChangesAsync();

        StatusMessage = $"{schematic.DisplayName} assigned to {vehicleSubtype}.";
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

        vehicle.SchematicType = schematic.Key;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;
        AddAuditLog(currentUser, "Unit schematic vehicle override", $"{currentUser.FullName} assigned {schematic.DisplayName} directly to {vehicle.Callsign} / {vehicle.RegistrationNumber}.");
        await _db.SaveChangesAsync();

        StatusMessage = $"{schematic.DisplayName} assigned directly to {vehicle.Callsign} / {vehicle.RegistrationNumber}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnassignAsync(
        string scopeType,
        string? vehicleFunction,
        string? vehicleSubtype,
        int? vehicleId)
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var normalizedScope = Normalize(scopeType);
        if (string.Equals(normalizedScope, VehicleSchematicAssignmentService.FunctionScope, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(vehicleFunction))
        {
            await _schematicAssignments.UnassignFunctionAsync(currentUser.CompanyId, vehicleFunction);
            AddAuditLog(currentUser, "Unit schematic function unassigned", $"{currentUser.FullName} unassigned the function schematic for {vehicleFunction}.");
            await _db.SaveChangesAsync();
            StatusMessage = $"Function schematic unassigned for {vehicleFunction}.";
            return RedirectToPage();
        }

        if (string.Equals(normalizedScope, VehicleSchematicAssignmentService.SubtypeScope, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(vehicleSubtype))
        {
            await _schematicAssignments.UnassignSubtypeAsync(currentUser.CompanyId, vehicleFunction, vehicleSubtype);
            AddAuditLog(currentUser, "Unit schematic subtype unassigned", $"{currentUser.FullName} unassigned the subtype schematic for {vehicleSubtype}.");
            await _db.SaveChangesAsync();
            StatusMessage = $"Subtype schematic unassigned for {vehicleSubtype}.";
            return RedirectToPage();
        }

        if (string.Equals(normalizedScope, "Vehicle", StringComparison.OrdinalIgnoreCase) && vehicleId.HasValue)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(item =>
                item.CompanyId == currentUser.CompanyId &&
                item.Id == vehicleId.Value);
            if (vehicle is not null)
            {
                vehicle.SchematicType = null;
                vehicle.UpdatedAtUtc = DateTime.UtcNow;
                AddAuditLog(currentUser, "Unit schematic vehicle override unassigned", $"{currentUser.FullName} cleared the direct schematic override for {vehicle.Callsign} / {vehicle.RegistrationNumber}.");
                await _db.SaveChangesAsync();
                StatusMessage = $"Direct schematic override cleared for {vehicle.Callsign} / {vehicle.RegistrationNumber}.";
            }
        }

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
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.CompanyId == companyId)
            .OrderBy(vehicle => vehicle.VehicleFunction)
            .ThenBy(vehicle => vehicle.VehicleSubtype)
            .ThenBy(vehicle => vehicle.Callsign)
            .ToListAsync();

        FunctionOptions = new[]
            {
                VehicleTaxonomyService.AmbulanceFunction,
                VehicleTaxonomyService.ResponseVehicleFunction
            }
            .Concat(vehicles.Select(vehicle => Normalize(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType) ?? string.Empty))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .ToList();

        SubtypeOptions = vehicles
            .Select(vehicle => new
            {
                Function = Normalize(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType),
                Subtype = Normalize(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Subtype))
            .GroupBy(item => new { item.Function, item.Subtype })
            .OrderBy(group => group.Key.Function)
            .ThenBy(group => group.Key.Subtype)
            .Select(group => new VehicleSubtypeOption(
                group.Key.Function,
                group.Key.Subtype!,
                group.Count()))
            .ToList();

        VehicleOptions = vehicles
            .Select(vehicle => new VehicleOption(
                vehicle.Id,
                vehicle.Callsign,
                vehicle.RegistrationNumber,
                Normalize(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType),
                Normalize(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType)))
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
            assignmentRows.Add(new SchematicAssignmentView(
                schematic.Key,
                assignment.ScopeType,
                AssignmentLabel(assignment),
                MatchingVehicleSummary(matchingVehicles),
                matchingVehicles.Count,
                string.Join(", ", matchingVehicles.Select(vehicle => vehicle.Callsign).Where(value => !string.IsNullOrWhiteSpace(value)).Take(5)),
                null,
                assignment.VehicleFunction,
                assignment.VehicleSubtype));
        }

        assignmentRows.AddRange(vehicles
            .Select(vehicle => new
            {
                Vehicle = vehicle,
                Schematic = VehicleSchematicLibrary.Find(vehicle.SchematicType ?? string.Empty)
            })
            .Where(item => item.Schematic is not null)
            .Select(item => new SchematicAssignmentView(
                item.Schematic!.Key,
                "Vehicle",
                $"{item.Vehicle.Callsign} / {item.Vehicle.RegistrationNumber}",
                $"{Normalize(item.Vehicle.VehicleFunction) ?? "Unassigned function"} | {Normalize(item.Vehicle.VehicleSubtype) ?? "Unassigned subtype"}",
                1,
                item.Vehicle.Callsign,
                item.Vehicle.Id,
                item.Vehicle.VehicleFunction,
                item.Vehicle.VehicleSubtype)));

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
            VehicleSchematicAssignmentService.FunctionScope => string.Equals(assignment.VehicleFunction, vehicleFunction, StringComparison.OrdinalIgnoreCase),
            VehicleSchematicAssignmentService.SubtypeScope => string.Equals(assignment.VehicleSubtype, vehicleSubtype, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(assignment.VehicleFunction) || string.Equals(assignment.VehicleFunction, vehicleFunction, StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static string AssignmentLabel(VehicleSchematicAssignment assignment)
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

        return "Assignment";
    }

    private static string MatchingVehicleSummary(IReadOnlyList<Vehicle> vehicles)
    {
        if (vehicles.Count == 0)
        {
            return "No registered vehicles currently match this assignment.";
        }

        return $"{vehicles.Count} registered vehicle(s) currently match this assignment.";
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
        "Vehicle" => 1,
        VehicleSchematicAssignmentService.SubtypeScope => 2,
        VehicleSchematicAssignmentService.FunctionScope => 3,
        _ => 4
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

    public sealed record SchematicAssignmentView(
        string SchematicKey,
        string ScopeType,
        string ScopeLabel,
        string Detail,
        int VehicleCount,
        string ExampleCallsigns,
        int? VehicleId,
        string? VehicleFunction,
        string? VehicleSubtype);
}
