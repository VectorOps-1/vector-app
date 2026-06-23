using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;

namespace vector_app_local.Services;

public class VehicleStructureSetupService
{
    private readonly VectorDbContext _db;

    public VehicleStructureSetupService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<List<VehicleFunctionSetupOption>> GetFunctionOptionsAsync(int companyId)
    {
        return await _db.VehicleFunctionSetups
            .AsNoTracking()
            .Where(item =>
                item.CompanyId == companyId &&
                item.Status == "Active")
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => new VehicleFunctionSetupOption(
                item.Id,
                item.Name,
                item.SortOrder))
            .ToListAsync();
    }

    public async Task<List<VehicleSubtypeSetupOption>> GetSubtypeOptionsAsync(int companyId)
    {
        return await _db.VehicleSubtypeSetups
            .AsNoTracking()
            .Include(item => item.VehicleFunctionSetup)
            .Where(item =>
                item.CompanyId == companyId &&
                item.Status == "Active" &&
                item.VehicleFunctionSetup != null &&
                item.VehicleFunctionSetup.Status == "Active")
            .OrderBy(item => item.VehicleFunctionSetup!.SortOrder)
            .ThenBy(item => item.VehicleFunctionSetup!.Name)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => new VehicleSubtypeSetupOption(
                item.Id,
                item.VehicleFunctionSetupId,
                item.VehicleFunctionSetup!.Name,
                item.Name,
                item.SortOrder))
            .ToListAsync();
    }

    public async Task<VehicleStructureSetupSnapshot> GetSnapshotAsync(int companyId)
    {
        var functions = await GetFunctionOptionsAsync(companyId);
        var subtypes = await GetSubtypeOptionsAsync(companyId);
        return new VehicleStructureSetupSnapshot(functions, subtypes);
    }
}

public sealed record VehicleFunctionSetupOption(int Id, string Name, int SortOrder);

public sealed record VehicleSubtypeSetupOption(
    int Id,
    int VehicleFunctionSetupId,
    string FunctionName,
    string Name,
    int SortOrder)
{
    public string Value => Name;
    public string FunctionSubtypeValue => $"{FunctionName}||{Name}";
    public string Label => $"{FunctionName} / {Name}";
}

public sealed record VehicleStructureSetupSnapshot(
    IReadOnlyList<VehicleFunctionSetupOption> Functions,
    IReadOnlyList<VehicleSubtypeSetupOption> Subtypes);
