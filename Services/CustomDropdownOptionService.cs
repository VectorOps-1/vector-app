using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class CustomDropdownOptionService
{
    public const string OtherValue = "__other";
    public const string StaffFileCategoryKey = "staff-file-category";
    public const string PersonalDocumentTypeKey = "personal-document-type";
    public const string IssueTypeKey = "issue-type";
    public const string VehicleDamageTypeKey = "vehicle-damage-type";

    public static readonly IReadOnlyList<string> StaffFileCategoryDefaults =
    [
        "ID / Passport",
        "Driver's License",
        "Medical Fitness",
        "Certificates",
        "Certifications",
        "Accreditation",
        "Training Records",
        "Employment Files",
        "Employment",
        "Clinical Governance",
        "Compliance",
        "Personal Documents"
    ];

    public static readonly IReadOnlyList<string> PersonalDocumentTypeDefaults =
    [
        "ID / Passport",
        "Driver's License",
        "Medical Fitness",
        "Certificate",
        "Accreditation",
        "Professional Registration",
        "Training Record",
        "Employment Document"
    ];

    public static readonly IReadOnlyList<string> IssueTypeDefaults =
    [
        "Damage",
        "Fault / failure",
        "Missing item",
        "Expired item",
        "Low stock",
        "Incorrect allocation",
        "Safety concern"
    ];

    public static readonly IReadOnlyList<string> VehicleDamageTypeDefaults =
    [
        "Dent",
        "Scratch",
        "Crack",
        "Missing item",
        "Fluid leak"
    ];

    private readonly VectorDbContext _db;

    public CustomDropdownOptionService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<List<SelectListItem>> BuildOptionsAsync(
        int companyId,
        string dropdownKey,
        IEnumerable<string> defaultOptions,
        string? selectedValue = null,
        string otherLabel = "Other")
    {
        var labels = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var option in defaultOptions)
        {
            AddLabel(option);
        }

        var customOptions = await _db.CustomDropdownOptions
            .AsNoTracking()
            .Where(option =>
                option.CompanyId == companyId &&
                option.DropdownKey == dropdownKey &&
                option.Status == "Active")
            .OrderBy(option => option.Value)
            .Select(option => option.Value)
            .ToListAsync();

        foreach (var option in customOptions)
        {
            AddLabel(option);
        }

        if (!string.IsNullOrWhiteSpace(selectedValue) &&
            !string.Equals(selectedValue, OtherValue, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(selectedValue, otherLabel, StringComparison.OrdinalIgnoreCase))
        {
            AddLabel(selectedValue);
        }

        var items = labels
            .Select(label => new SelectListItem(label, label, string.Equals(label, selectedValue, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        items.Add(new SelectListItem(otherLabel, OtherValue, string.Equals(selectedValue, OtherValue, StringComparison.OrdinalIgnoreCase)));
        return items;

        void AddLabel(string? label)
        {
            var normalized = NormalizeOptionLabel(label);
            if (normalized is null || string.Equals(normalized, otherLabel, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (seen.Add(normalized))
            {
                labels.Add(normalized);
            }
        }
    }

    public async Task<string?> ResolveSelectionAsync(
        int companyId,
        int createdByUserId,
        string dropdownKey,
        string? selectedValue,
        string? customOtherValue,
        string fallbackValue)
    {
        var selected = NormalizeOptionLabel(selectedValue);
        if (selected is null)
        {
            return fallbackValue;
        }

        if (!string.Equals(selected, OtherValue, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(selected, "Other", StringComparison.OrdinalIgnoreCase))
        {
            return selected;
        }

        var custom = NormalizeOptionLabel(customOtherValue);
        if (custom is null)
        {
            return null;
        }

        await EnsureCustomOptionAsync(companyId, createdByUserId, dropdownKey, custom);
        return custom;
    }

    private async Task EnsureCustomOptionAsync(int companyId, int createdByUserId, string dropdownKey, string value)
    {
        var exists = await _db.CustomDropdownOptions.AnyAsync(option =>
            option.CompanyId == companyId &&
            option.DropdownKey == dropdownKey &&
            option.Status == "Active" &&
            option.Value.ToLower() == value.ToLower());

        if (exists)
        {
            return;
        }

        _db.CustomDropdownOptions.Add(new CustomDropdownOption
        {
            CompanyId = companyId,
            CreatedByUserId = createdByUserId,
            DropdownKey = dropdownKey,
            Value = value,
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    private static string? NormalizeOptionLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= 160 ? normalized : normalized[..160];
    }
}
