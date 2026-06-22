using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class VehicleSchematicAssignmentService
{
    public const string FunctionScope = "Function";
    public const string SubtypeScope = "Subtype";
    public const string AreaScope = "Area";
    public const string VehicleScope = "Vehicle";

    private readonly VectorDbContext _db;

    public VehicleSchematicAssignmentService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<VehicleSchematicDefinition?> ResolveForVehicleAsync(int companyId, Vehicle vehicle)
    {
        if (vehicle.CompanyId != companyId)
        {
            return null;
        }

        var vehicleKey = await _db.VehicleSchematicAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == companyId &&
                assignment.ScopeType == VehicleScope &&
                assignment.VehicleId == vehicle.Id)
            .OrderByDescending(assignment => assignment.UpdatedAtUtc ?? assignment.CreatedAtUtc)
            .Select(assignment => assignment.SchematicKey)
            .FirstOrDefaultAsync();

        var vehicleSchematic = VehicleSchematicLibrary.Find(vehicleKey ?? string.Empty);
        if (vehicleSchematic is not null)
        {
            return vehicleSchematic;
        }

        var function = Normalize(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        var subtype = Normalize(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);

        if (vehicle.CurrentOperationalAreaId.HasValue)
        {
            var areaKey = await _db.VehicleSchematicAssignments
                .AsNoTracking()
                .Where(assignment =>
                    assignment.CompanyId == companyId &&
                    assignment.ScopeType == AreaScope &&
                    assignment.OperationalAreaId == vehicle.CurrentOperationalAreaId.Value)
                .OrderByDescending(assignment => assignment.UpdatedAtUtc ?? assignment.CreatedAtUtc)
                .Select(assignment => assignment.SchematicKey)
                .FirstOrDefaultAsync();

            var areaSchematic = VehicleSchematicLibrary.Find(areaKey ?? string.Empty);
            if (areaSchematic is not null)
            {
                return areaSchematic;
            }
        }

        if (!string.IsNullOrWhiteSpace(subtype))
        {
            var subtypeKey = await _db.VehicleSchematicAssignments
                .AsNoTracking()
                .Where(assignment =>
                    assignment.CompanyId == companyId &&
                    assignment.ScopeType == SubtypeScope &&
                    assignment.VehicleSubtype == subtype &&
                    (assignment.VehicleFunction == function || assignment.VehicleFunction == null || assignment.VehicleFunction == ""))
                .OrderByDescending(assignment => assignment.VehicleFunction == function)
                .ThenByDescending(assignment => assignment.UpdatedAtUtc ?? assignment.CreatedAtUtc)
                .Select(assignment => assignment.SchematicKey)
                .FirstOrDefaultAsync();

            var subtypeSchematic = VehicleSchematicLibrary.Find(subtypeKey ?? string.Empty);
            if (subtypeSchematic is not null)
            {
                return subtypeSchematic;
            }
        }

        if (!string.IsNullOrWhiteSpace(function))
        {
            var functionKey = await _db.VehicleSchematicAssignments
                .AsNoTracking()
                .Where(assignment =>
                    assignment.CompanyId == companyId &&
                    assignment.ScopeType == FunctionScope &&
                    assignment.VehicleFunction == function)
                .OrderByDescending(assignment => assignment.UpdatedAtUtc ?? assignment.CreatedAtUtc)
                .Select(assignment => assignment.SchematicKey)
                .FirstOrDefaultAsync();

            return VehicleSchematicLibrary.Find(functionKey ?? string.Empty);
        }

        return null;
    }

    public async Task AssignFunctionAsync(int companyId, int userId, string vehicleFunction, string schematicKey)
    {
        var normalizedFunction = Normalize(vehicleFunction)
            ?? throw new InvalidOperationException("Vehicle function is required.");
        await UpsertAssignmentAsync(companyId, userId, FunctionScope, normalizedFunction, null, null, null, schematicKey);
    }

    public async Task AssignSubtypeAsync(int companyId, int userId, string? vehicleFunction, string vehicleSubtype, string schematicKey)
    {
        var normalizedSubtype = Normalize(vehicleSubtype)
            ?? throw new InvalidOperationException("Vehicle subtype is required.");
        await UpsertAssignmentAsync(companyId, userId, SubtypeScope, Normalize(vehicleFunction), normalizedSubtype, null, null, schematicKey);
    }

    public async Task AssignAreaAsync(int companyId, int userId, int operationalAreaId, string schematicKey)
    {
        var areaExists = await _db.OperationalAreas.AnyAsync(area =>
            area.CompanyId == companyId &&
            area.Id == operationalAreaId);
        if (!areaExists)
        {
            throw new InvalidOperationException("Selected area is not configured.");
        }

        await UpsertAssignmentAsync(companyId, userId, AreaScope, null, null, operationalAreaId, null, schematicKey);
    }

    public async Task AssignVehicleAsync(int companyId, int userId, int vehicleId, string schematicKey)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            item.Id == vehicleId);
        if (vehicle is null)
        {
            throw new InvalidOperationException("Selected vehicle is not configured.");
        }

        vehicle.SchematicType = null;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        await UpsertAssignmentAsync(
            companyId,
            userId,
            VehicleScope,
            Normalize(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType),
            Normalize(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType),
            null,
            vehicle.Id,
            schematicKey);
    }

    public async Task<bool> UnassignAssignmentAsync(int companyId, int assignmentId)
    {
        var assignment = await _db.VehicleSchematicAssignments.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            item.Id == assignmentId);
        if (assignment is null)
        {
            return false;
        }

        _db.VehicleSchematicAssignments.Remove(assignment);

        if (assignment.ScopeType == VehicleScope && assignment.VehicleId.HasValue)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(item =>
                item.CompanyId == companyId &&
                item.Id == assignment.VehicleId.Value);
            if (vehicle is not null && !string.IsNullOrWhiteSpace(vehicle.SchematicType))
            {
                vehicle.SchematicType = null;
                vehicle.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task UpsertAssignmentAsync(
        int companyId,
        int userId,
        string scopeType,
        string? vehicleFunction,
        string? vehicleSubtype,
        int? operationalAreaId,
        int? vehicleId,
        string schematicKey)
    {
        var schematic = VehicleSchematicLibrary.Find(schematicKey)
            ?? throw new InvalidOperationException("Selected schematic is not configured.");
        var now = DateTime.UtcNow;

        var assignment = await _db.VehicleSchematicAssignments.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            item.ScopeType == scopeType &&
            item.VehicleFunction == vehicleFunction &&
            item.VehicleSubtype == vehicleSubtype &&
            item.OperationalAreaId == operationalAreaId &&
            item.VehicleId == vehicleId);

        if (assignment is null)
        {
            assignment = new VehicleSchematicAssignment
            {
                CompanyId = companyId,
                ScopeType = scopeType,
                VehicleFunction = vehicleFunction,
                VehicleSubtype = vehicleSubtype,
                OperationalAreaId = operationalAreaId,
                VehicleId = vehicleId,
                CreatedByUserId = userId,
                CreatedAtUtc = now
            };
            _db.VehicleSchematicAssignments.Add(assignment);
        }

        assignment.SchematicKey = schematic.Key;
        assignment.UpdatedAtUtc = now;
        await _db.SaveChangesAsync();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
