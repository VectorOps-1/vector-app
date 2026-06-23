using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class StaffStructureSetupService
{
    public const string DefaultProfileFieldFullName = "Full name";
    public const string DefaultProfileFieldEmail = "Email";
    public const string DefaultProfileFieldCellNumber = "Cell number";
    public const string DefaultProfileFieldNationalId = "National ID";
    public const string DefaultProfileFieldAssignedArea = "Assigned area";
    public const string DefaultProfileFieldDocuments = "Staff documents";

    public static readonly IReadOnlyList<string> AvailableDefaultProfileFields =
    [
        DefaultProfileFieldFullName,
        DefaultProfileFieldEmail,
        DefaultProfileFieldCellNumber,
        DefaultProfileFieldNationalId,
        DefaultProfileFieldAssignedArea,
        DefaultProfileFieldDocuments
    ];

    private readonly VectorDbContext _db;

    public StaffStructureSetupService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<List<StaffQualificationSetupOption>> GetQualificationOptionsAsync(int companyId)
    {
        return await _db.StaffQualificationSetups
            .AsNoTracking()
            .Where(item =>
                item.CompanyId == companyId &&
                item.Status == "Active")
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => new StaffQualificationSetupOption(
                item.Id,
                item.Name,
                item.SortOrder))
            .ToListAsync();
    }

    public async Task<StaffStructureSetupSnapshot> GetSnapshotAsync(int companyId)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId);
        var qualifications = await GetQualificationOptionsAsync(companyId);
        return new StaffStructureSetupSnapshot(
            qualifications,
            company?.StaffIdFormat,
            company?.StaffPractitionerNumberRequired ?? false,
            company?.StaffAnnualLicenseExpiryRequired ?? false,
            company?.StaffCpdTrackingRequired ?? false,
            ParseDefaultProfileFields(company?.StaffDefaultProfileFields));
    }

    public static IReadOnlySet<string> ParseDefaultProfileFields(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return fields
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(field => AvailableDefaultProfileFields.Any(available => available.Equals(field, StringComparison.OrdinalIgnoreCase)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static string SerializeDefaultProfileFields(IEnumerable<string> fields)
    {
        var selected = fields
            .Where(field => AvailableDefaultProfileFields.Any(available => available.Equals(field, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(field => AvailableDefaultProfileFields.ToList().FindIndex(available => available.Equals(field, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return string.Join(",", selected);
    }
}

public sealed record StaffQualificationSetupOption(int Id, string Name, int SortOrder);

public sealed record StaffStructureSetupSnapshot(
    IReadOnlyList<StaffQualificationSetupOption> Qualifications,
    string? StaffIdFormat,
    bool PractitionerNumberRequired,
    bool AnnualLicenseExpiryRequired,
    bool CpdTrackingRequired,
    IReadOnlySet<string> DefaultProfileFields);
