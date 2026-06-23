using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class OperationalStructureSetupModel : PageModel
{
    public const string FlatMode = "Flat";
    public const string AreasUnderRegionsMode = "Areas under regions";
    public const string AreasUnderBasesMode = "Areas under bases";
    public const string AreasUnderRegionsAndBasesMode = "Areas under regions and bases";

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public OperationalStructureSetupModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty] public string OperationalStructureMode { get; set; } = FlatMode;
    [BindProperty] public string? RegionName { get; set; }
    [BindProperty] public string? RegionAddress { get; set; }
    [BindProperty] public string? RegionNotes { get; set; }
    [BindProperty] public string? BaseName { get; set; }
    [BindProperty] public int? BaseParentRegionId { get; set; }
    [BindProperty] public string? BaseAddress { get; set; }
    [BindProperty] public string? BaseNotes { get; set; }
    [BindProperty] public string? AreaName { get; set; }
    [BindProperty] public int? AreaParentOperationalAreaId { get; set; }
    [BindProperty] public string? AreaAddress { get; set; }
    [BindProperty] public string? AreaNotes { get; set; }
    [BindProperty] public string? StorageName { get; set; }
    [BindProperty] public int? StorageOperationalAreaId { get; set; }
    [BindProperty] public string StorageType { get; set; } = "General store";
    [BindProperty] public string? StorageNotes { get; set; }

    public string ClientName { get; private set; } = CompanyBranding.DefaultCompanyName;
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public List<SelectListItem> StructureModeOptions { get; private set; } = new();
    public List<SelectListItem> RegionOptions { get; private set; } = new();
    public List<SelectListItem> ParentAreaOptions { get; private set; } = new();
    public List<SelectListItem> StorageParentOptions { get; private set; } = new();
    public List<OperationalStructureRow> StructureRows { get; private set; } = new();
    public List<StorageLocationRow> StorageRows { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveModeAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var company = await _db.Companies.FirstOrDefaultAsync(item =>
            item.Id == currentUser.CompanyId &&
            item.Status == "Active");
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        company.OperationalStructureMode = NormalizeMode(OperationalStructureMode);
        company.UpdatedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(BuildAudit(currentUser, "Operational structure mode updated", "Company", company.Id, $"Operational structure mode set to {company.OperationalStructureMode}."));
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Operational structure mode saved.";
        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    public Task<IActionResult> OnPostAddRegionAsync()
    {
        return AddOperationalAreaAsync("Region", RegionName, null, RegionAddress, RegionNotes);
    }

    public Task<IActionResult> OnPostAddBaseAsync()
    {
        return AddOperationalAreaAsync("Base", BaseName, BaseParentRegionId, BaseAddress, BaseNotes);
    }

    public Task<IActionResult> OnPostAddAreaAsync()
    {
        return AddOperationalAreaAsync("Operational Area", AreaName, AreaParentOperationalAreaId, AreaAddress, AreaNotes);
    }

    public async Task<IActionResult> OnPostAddStorageAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var trimmedName = StorageName?.Trim() ?? string.Empty;
        var hasErrors = false;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ModelState.AddModelError(nameof(StorageName), "Enter the storage space name.");
            hasErrors = true;
        }

        if (!StorageOperationalAreaId.HasValue)
        {
            ModelState.AddModelError(nameof(StorageOperationalAreaId), "Select the base, area, or region that owns this storage space.");
            hasErrors = true;
        }

        var parentArea = StorageOperationalAreaId.HasValue
            ? await _db.OperationalAreas.FirstOrDefaultAsync(area =>
                area.Id == StorageOperationalAreaId.Value &&
                area.CompanyId == currentUser.CompanyId &&
                area.Status == "Active")
            : null;

        if (StorageOperationalAreaId.HasValue && parentArea is null)
        {
            ModelState.AddModelError(nameof(StorageOperationalAreaId), "The selected parent location was not found.");
            hasErrors = true;
        }

        if (hasErrors)
        {
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var duplicateExists = await _db.StorageLocations.AnyAsync(location =>
            location.CompanyId == currentUser.CompanyId &&
            location.OperationalAreaId == parentArea!.Id &&
            location.Name == trimmedName);

        if (duplicateExists)
        {
            ModelState.AddModelError(nameof(StorageName), "That storage space already exists under the selected location.");
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var now = DateTime.UtcNow;
        var storage = new StorageLocation
        {
            CompanyId = currentUser.CompanyId,
            OperationalAreaId = parentArea!.Id,
            Name = trimmedName,
            StorageType = NormalizeOptional(StorageType) ?? "General store",
            Status = "Active",
            Notes = NormalizeOptional(StorageNotes),
            CreatedAtUtc = now
        };

        _db.StorageLocations.Add(storage);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(BuildAudit(currentUser, "Storage space created", "StorageLocation", storage.Id, $"{storage.StorageType} created: {storage.Name} under {parentArea.Name}."));
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = $"{storage.Name} saved as a storage space.";
        StorageName = null;
        StorageNotes = null;
        StorageOperationalAreaId = null;
        StorageType = "General store";

        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteStepAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var company = await _db.Companies.FirstOrDefaultAsync(item =>
            item.Id == currentUser.CompanyId &&
            item.Status == "Active");
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        company.OperationalStructureMode = NormalizeMode(OperationalStructureMode);
        SetupWizardProgress.MarkStepComplete(company, SetupWizardProgress.OperationalStructureStepKey);
        company.UpdatedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(BuildAudit(currentUser, "Setup step completed", "Company", company.Id, "Operational structure setup completed."));
        await _db.SaveChangesAsync();

        return RedirectToPage("/SetupWizard");
    }

    private async Task<IActionResult> AddOperationalAreaAsync(string areaType, string? name, int? parentOperationalAreaId, string? address, string? notes)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var trimmedName = name?.Trim() ?? string.Empty;
        var hasErrors = false;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ModelState.AddModelError(string.Empty, $"Enter the {areaType.ToLowerInvariant()} name.");
            hasErrors = true;
        }

        OperationalArea? parent = null;
        if (parentOperationalAreaId.HasValue)
        {
            parent = await _db.OperationalAreas.FirstOrDefaultAsync(area =>
                area.Id == parentOperationalAreaId.Value &&
                area.CompanyId == currentUser.CompanyId &&
                area.Status == "Active");

            if (parent is null)
            {
                ModelState.AddModelError(nameof(AreaParentOperationalAreaId), "The selected parent location was not found.");
                hasErrors = true;
            }
        }

        if (hasErrors)
        {
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var duplicateExists = await _db.OperationalAreas.AnyAsync(area =>
            area.CompanyId == currentUser.CompanyId &&
            area.Name == trimmedName);

        if (duplicateExists)
        {
            ModelState.AddModelError(string.Empty, "That region, base, or operational area already exists.");
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var now = DateTime.UtcNow;
        var operationalArea = new OperationalArea
        {
            CompanyId = currentUser.CompanyId,
            Name = trimmedName,
            AreaType = areaType,
            ParentOperationalAreaId = parent?.Id,
            Address = NormalizeOptional(address),
            Status = "Active",
            Notes = NormalizeOptional(notes),
            CreatedAtUtc = now
        };

        _db.OperationalAreas.Add(operationalArea);
        await _db.SaveChangesAsync();

        var parentText = parent is null ? string.Empty : $" Parent: {parent.Name}.";
        _db.AuditLogs.Add(BuildAudit(currentUser, "Operational structure item created", "OperationalArea", operationalArea.Id, $"{areaType} created: {operationalArea.Name}.{parentText}"));
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = $"{operationalArea.Name} saved as {areaType}.";
        ResetInputs();
        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    private async Task LoadPageStateAsync(int companyId)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId);

        ClientName = CompanyBranding.GetDisplayCompanyName(company);
        OperationalStructureMode = NormalizeMode(OperationalStructureMode);
        var hasPostedMode = Request.HasFormContentType &&
            !string.IsNullOrWhiteSpace(Request.Form[nameof(OperationalStructureMode)]);
        if (company is not null && !hasPostedMode)
        {
            OperationalStructureMode = NormalizeMode(company.OperationalStructureMode);
        }

        StructureRows = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == companyId)
            .OrderBy(area => area.AreaType)
            .ThenBy(area => area.ParentOperationalArea == null ? string.Empty : area.ParentOperationalArea.Name)
            .ThenBy(area => area.Name)
            .Select(area => new OperationalStructureRow
            {
                Id = area.Id,
                Name = area.Name,
                AreaType = area.AreaType,
                ParentName = area.ParentOperationalArea == null ? null : area.ParentOperationalArea.Name,
                Address = area.Address,
                Status = area.Status
            })
            .ToListAsync();

        StorageRows = await _db.StorageLocations
            .AsNoTracking()
            .Where(location => location.CompanyId == companyId)
            .OrderBy(location => location.OperationalArea == null ? string.Empty : location.OperationalArea.Name)
            .ThenBy(location => location.Name)
            .Select(location => new StorageLocationRow
            {
                Id = location.Id,
                Name = location.Name,
                StorageType = location.StorageType,
                ParentName = location.OperationalArea == null ? "Unassigned" : location.OperationalArea.Name,
                Status = location.Status
            })
            .ToListAsync();

        StructureModeOptions = BuildStructureModeOptions();
        RegionOptions = BuildAreaOptions(StructureRows.Where(row => row.AreaType == "Region"), "Select parent region");
        ParentAreaOptions = BuildAreaOptions(
            StructureRows.Where(row => row.AreaType is "Region" or "Base"),
            "No parent / flat area");
        StorageParentOptions = BuildAreaOptions(StructureRows, "Select parent location");
    }

    private List<SelectListItem> BuildStructureModeOptions()
    {
        return new[]
        {
            FlatMode,
            AreasUnderRegionsMode,
            AreasUnderBasesMode,
            AreasUnderRegionsAndBasesMode
        }
        .Select(mode => new SelectListItem
        {
            Value = mode,
            Text = mode,
            Selected = string.Equals(OperationalStructureMode, mode, StringComparison.OrdinalIgnoreCase)
        })
        .ToList();
    }

    private static List<SelectListItem> BuildAreaOptions(IEnumerable<OperationalStructureRow> rows, string placeholder)
    {
        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = placeholder }
        };

        options.AddRange(rows.Select(row => new SelectListItem
        {
            Value = row.Id.ToString(),
            Text = string.IsNullOrWhiteSpace(row.ParentName)
                ? $"{row.AreaType}: {row.Name}"
                : $"{row.AreaType}: {row.Name} under {row.ParentName}"
        }));

        return options;
    }

    private static string NormalizeMode(string? mode)
    {
        var cleaned = mode?.Trim();
        return cleaned is AreasUnderRegionsMode or AreasUnderBasesMode or AreasUnderRegionsAndBasesMode
            ? cleaned
            : FlatMode;
    }

    private static string? NormalizeOptional(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private void ResetInputs()
    {
        RegionName = null;
        RegionAddress = null;
        RegionNotes = null;
        BaseName = null;
        BaseParentRegionId = null;
        BaseAddress = null;
        BaseNotes = null;
        AreaName = null;
        AreaParentOperationalAreaId = null;
        AreaAddress = null;
        AreaNotes = null;
    }

    private static AuditLog BuildAudit(AppUser currentUser, string action, string entityType, int entityId, string details)
    {
        return new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public sealed class OperationalStructureRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AreaType { get; set; } = string.Empty;
        public string? ParentName { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public sealed class StorageLocationRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string StorageType { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
