using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class VehicleSchematicAssignmentService
{
    public const string FunctionScope = "Function";
    public const string SubtypeScope = "Subtype";

    private readonly VectorDbContext _db;

    public VehicleSchematicAssignmentService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<VehicleSchematicDefinition?> ResolveForVehicleAsync(int companyId, Vehicle vehicle)
    {
        var specific = VehicleSchematicLibrary.Find(vehicle.SchematicType ?? string.Empty);
        if (specific is not null)
        {
            return specific;
        }

        var function = Normalize(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        var subtype = Normalize(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);

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
        await UpsertAssignmentAsync(companyId, userId, FunctionScope, normalizedFunction, null, schematicKey);
    }

    public async Task AssignSubtypeAsync(int companyId, int userId, string? vehicleFunction, string vehicleSubtype, string schematicKey)
    {
        var normalizedSubtype = Normalize(vehicleSubtype)
            ?? throw new InvalidOperationException("Vehicle subtype is required.");
        await UpsertAssignmentAsync(companyId, userId, SubtypeScope, Normalize(vehicleFunction), normalizedSubtype, schematicKey);
    }

    public async Task UnassignFunctionAsync(int companyId, string vehicleFunction)
    {
        var normalizedFunction = Normalize(vehicleFunction);
        if (normalizedFunction is null)
        {
            return;
        }

        await _db.VehicleSchematicAssignments
            .Where(assignment =>
                assignment.CompanyId == companyId &&
                assignment.ScopeType == FunctionScope &&
                assignment.VehicleFunction == normalizedFunction)
            .ExecuteDeleteAsync();
    }

    public async Task UnassignSubtypeAsync(int companyId, string? vehicleFunction, string vehicleSubtype)
    {
        var normalizedSubtype = Normalize(vehicleSubtype);
        if (normalizedSubtype is null)
        {
            return;
        }

        var normalizedFunction = Normalize(vehicleFunction);
        await _db.VehicleSchematicAssignments
            .Where(assignment =>
                assignment.CompanyId == companyId &&
                assignment.ScopeType == SubtypeScope &&
                assignment.VehicleSubtype == normalizedSubtype &&
                assignment.VehicleFunction == normalizedFunction)
            .ExecuteDeleteAsync();
    }

    private async Task UpsertAssignmentAsync(
        int companyId,
        int userId,
        string scopeType,
        string? vehicleFunction,
        string? vehicleSubtype,
        string schematicKey)
    {
        var schematic = VehicleSchematicLibrary.Find(schematicKey)
            ?? throw new InvalidOperationException("Selected schematic is not configured.");
        var now = DateTime.UtcNow;

        var assignment = await _db.VehicleSchematicAssignments.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            item.ScopeType == scopeType &&
            item.VehicleFunction == vehicleFunction &&
            item.VehicleSubtype == vehicleSubtype);

        if (assignment is null)
        {
            assignment = new VehicleSchematicAssignment
            {
                CompanyId = companyId,
                ScopeType = scopeType,
                VehicleFunction = vehicleFunction,
                VehicleSubtype = vehicleSubtype,
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
