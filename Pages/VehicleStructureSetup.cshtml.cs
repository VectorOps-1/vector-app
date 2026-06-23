using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class VehicleStructureSetupModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly VehicleStructureSetupService _vehicleStructure;
    private readonly VehicleSchematicAssignmentService _schematicAssignments;

    public VehicleStructureSetupModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        VehicleStructureSetupService vehicleStructure,
        VehicleSchematicAssignmentService schematicAssignments)
    {
        _db = db;
        _currentUser = currentUser;
        _vehicleStructure = vehicleStructure;
        _schematicAssignments = schematicAssignments;
    }

    [BindProperty] public string? FunctionName { get; set; }
    [BindProperty] public string? FunctionNotes { get; set; }
    [BindProperty] public int? SubtypeFunctionSetupId { get; set; }
    [BindProperty] public string? SubtypeName { get; set; }
    [BindProperty] public string? SubtypeNotes { get; set; }
    [BindProperty] public int? DefaultFunctionSetupId { get; set; }
    [BindProperty] public string? DefaultFunctionSchematicKey { get; set; }
    [BindProperty] public int? DefaultSubtypeSetupId { get; set; }
    [BindProperty] public string? DefaultSubtypeSchematicKey { get; set; }

    public string ClientName { get; private set; } = CompanyBranding.DefaultCompanyName;
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public List<SelectListItem> FunctionOptions { get; private set; } = new();
    public List<SelectListItem> SubtypeOptions { get; private set; } = new();
    public List<SelectListItem> SchematicOptions { get; private set; } = new();
    public List<VehicleStructureFunctionRow> FunctionRows { get; private set; } = new();

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

    public async Task<IActionResult> OnPostAddFunctionAsync()
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var name = NormalizeOptional(FunctionName);
        if (name is null)
        {
            ModelState.AddModelError(nameof(FunctionName), "Enter the vehicle function name.");
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var duplicateExists = await _db.VehicleFunctionSetups.AnyAsync(item =>
            item.CompanyId == currentUser.CompanyId &&
            item.Status == "Active" &&
            item.Name == name);
        if (duplicateExists)
        {
            ModelState.AddModelError(nameof(FunctionName), "This vehicle function already exists.");
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var nextSortOrder = await NextFunctionSortOrderAsync(currentUser.CompanyId);
        var now = DateTime.UtcNow;
        var vehicleFunction = new VehicleFunctionSetup
        {
            CompanyId = currentUser.CompanyId,
            Name = name,
            Status = "Active",
            SortOrder = nextSortOrder,
            Notes = NormalizeOptional(FunctionNotes),
            CreatedAtUtc = now
        };

        _db.VehicleFunctionSetups.Add(vehicleFunction);
        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(BuildAudit(currentUser, "Vehicle function setup created", "VehicleFunctionSetup", vehicleFunction.Id, $"Vehicle function created: {vehicleFunction.Name}."));
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = $"{vehicleFunction.Name} added as a vehicle function.";
        FunctionName = null;
        FunctionNotes = null;
        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAddSubtypeAsync()
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var function = SubtypeFunctionSetupId.HasValue
            ? await _db.VehicleFunctionSetups.FirstOrDefaultAsync(item =>
                item.CompanyId == currentUser.CompanyId &&
                item.Id == SubtypeFunctionSetupId.Value &&
                item.Status == "Active")
            : null;
        if (function is null)
        {
            ModelState.AddModelError(nameof(SubtypeFunctionSetupId), "Select the function this subtype belongs to.");
        }

        var name = NormalizeOptional(SubtypeName);
        if (name is null)
        {
            ModelState.AddModelError(nameof(SubtypeName), "Enter the vehicle subtype name.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var duplicateExists = await _db.VehicleSubtypeSetups.AnyAsync(item =>
            item.CompanyId == currentUser.CompanyId &&
            item.VehicleFunctionSetupId == function!.Id &&
            item.Status == "Active" &&
            item.Name == name);
        if (duplicateExists)
        {
            ModelState.AddModelError(nameof(SubtypeName), "This subtype already exists under the selected function.");
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var nextSortOrder = await NextSubtypeSortOrderAsync(currentUser.CompanyId, function!.Id);
        var now = DateTime.UtcNow;
        var subtype = new VehicleSubtypeSetup
        {
            CompanyId = currentUser.CompanyId,
            VehicleFunctionSetupId = function.Id,
            Name = name!,
            Status = "Active",
            SortOrder = nextSortOrder,
            Notes = NormalizeOptional(SubtypeNotes),
            CreatedAtUtc = now
        };

        _db.VehicleSubtypeSetups.Add(subtype);
        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(BuildAudit(currentUser, "Vehicle subtype setup created", "VehicleSubtypeSetup", subtype.Id, $"Vehicle subtype created: {function.Name} / {subtype.Name}."));
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = $"{subtype.Name} added under {function.Name}.";
        SubtypeName = null;
        SubtypeNotes = null;
        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveFunctionSchematicAsync()
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var function = DefaultFunctionSetupId.HasValue
            ? await _db.VehicleFunctionSetups.FirstOrDefaultAsync(item =>
                item.CompanyId == currentUser.CompanyId &&
                item.Id == DefaultFunctionSetupId.Value &&
                item.Status == "Active")
            : null;
        if (function is null)
        {
            ModelState.AddModelError(nameof(DefaultFunctionSetupId), "Select the vehicle function to configure.");
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var schematicKey = NormalizeOptional(DefaultFunctionSchematicKey);
        if (schematicKey is null)
        {
            await _schematicAssignments.UnassignFunctionAsync(currentUser.CompanyId, function.Name);
            _db.AuditLogs.Add(BuildAudit(currentUser, "Vehicle function schematic default cleared", "VehicleFunctionSetup", function.Id, $"Default schematic cleared for {function.Name}."));
            await _db.SaveChangesAsync();
            StatusMessage = $"Default schematic cleared for {function.Name}.";
        }
        else
        {
            var schematic = VehicleSchematicLibrary.Find(schematicKey);
            if (schematic is null || !schematic.IsPublished)
            {
                ModelState.AddModelError(nameof(DefaultFunctionSchematicKey), "Select a published unit schematic.");
                await LoadPageStateAsync(currentUser.CompanyId);
                return Page();
            }

            await _schematicAssignments.AssignFunctionAsync(currentUser.CompanyId, currentUser.Id, function.Name, schematic.Key);
            _db.AuditLogs.Add(BuildAudit(currentUser, "Vehicle function schematic default set", "VehicleFunctionSetup", function.Id, $"{schematic.DisplayName} set as default schematic for {function.Name}."));
            await _db.SaveChangesAsync();
            StatusMessage = $"{schematic.DisplayName} set as default for {function.Name}.";
        }

        ActionSaved = true;
        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveSubtypeSchematicAsync()
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var subtype = DefaultSubtypeSetupId.HasValue
            ? await _db.VehicleSubtypeSetups
                .Include(item => item.VehicleFunctionSetup)
                .FirstOrDefaultAsync(item =>
                    item.CompanyId == currentUser.CompanyId &&
                    item.Id == DefaultSubtypeSetupId.Value &&
                    item.Status == "Active" &&
                    item.VehicleFunctionSetup != null &&
                    item.VehicleFunctionSetup.Status == "Active")
            : null;
        if (subtype?.VehicleFunctionSetup is null)
        {
            ModelState.AddModelError(nameof(DefaultSubtypeSetupId), "Select the vehicle subtype to configure.");
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var schematicKey = NormalizeOptional(DefaultSubtypeSchematicKey);
        if (schematicKey is null)
        {
            await _schematicAssignments.UnassignSubtypeAsync(currentUser.CompanyId, subtype.VehicleFunctionSetup.Name, subtype.Name);
            _db.AuditLogs.Add(BuildAudit(currentUser, "Vehicle subtype schematic default cleared", "VehicleSubtypeSetup", subtype.Id, $"Default schematic cleared for {subtype.VehicleFunctionSetup.Name} / {subtype.Name}."));
            await _db.SaveChangesAsync();
            StatusMessage = $"Default schematic cleared for {subtype.VehicleFunctionSetup.Name} / {subtype.Name}.";
        }
        else
        {
            var schematic = VehicleSchematicLibrary.Find(schematicKey);
            if (schematic is null || !schematic.IsPublished)
            {
                ModelState.AddModelError(nameof(DefaultSubtypeSchematicKey), "Select a published unit schematic.");
                await LoadPageStateAsync(currentUser.CompanyId);
                return Page();
            }

            await _schematicAssignments.AssignSubtypeAsync(currentUser.CompanyId, currentUser.Id, subtype.VehicleFunctionSetup.Name, subtype.Name, schematic.Key);
            _db.AuditLogs.Add(BuildAudit(currentUser, "Vehicle subtype schematic default set", "VehicleSubtypeSetup", subtype.Id, $"{schematic.DisplayName} set as default schematic for {subtype.VehicleFunctionSetup.Name} / {subtype.Name}."));
            await _db.SaveChangesAsync();
            StatusMessage = $"{schematic.DisplayName} set as default for {subtype.VehicleFunctionSetup.Name} / {subtype.Name}.";
        }

        ActionSaved = true;
        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteStepAsync()
    {
        var currentUser = await RequireCurrentUserAsync();
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

        var hasFunction = await _db.VehicleFunctionSetups.AnyAsync(item =>
            item.CompanyId == currentUser.CompanyId &&
            item.Status == "Active");
        var hasSubtype = await _db.VehicleSubtypeSetups.AnyAsync(item =>
            item.CompanyId == currentUser.CompanyId &&
            item.Status == "Active");
        if (!hasFunction || !hasSubtype)
        {
            StatusMessage = "Add at least one vehicle function and one subtype before completing this setup step.";
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        SetupWizardProgress.MarkStepComplete(company, SetupWizardProgress.VehicleStructureStepKey);
        company.UpdatedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(BuildAudit(currentUser, "Setup step completed", "Company", company.Id, "Vehicle structure setup completed."));
        await _db.SaveChangesAsync();

        return RedirectToPage("/SetupWizard");
    }

    private async Task LoadPageStateAsync(int companyId)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId);
        ClientName = CompanyBranding.GetDisplayCompanyName(company);

        var functions = await _vehicleStructure.GetFunctionOptionsAsync(companyId);
        var subtypes = await _vehicleStructure.GetSubtypeOptionsAsync(companyId);
        var assignments = await _db.VehicleSchematicAssignments
            .AsNoTracking()
            .Where(item =>
                item.CompanyId == companyId &&
                (item.ScopeType == VehicleSchematicAssignmentService.FunctionScope ||
                    item.ScopeType == VehicleSchematicAssignmentService.SubtypeScope))
            .ToListAsync();

        FunctionOptions = functions
            .Select(item => new SelectListItem
            {
                Value = item.Id.ToString(),
                Text = item.Name,
                Selected = SubtypeFunctionSetupId == item.Id || DefaultFunctionSetupId == item.Id
            })
            .ToList();

        SubtypeOptions = subtypes
            .Select(item => new SelectListItem
            {
                Value = item.Id.ToString(),
                Text = item.Label,
                Selected = DefaultSubtypeSetupId == item.Id
            })
            .ToList();

        SchematicOptions = VehicleSchematicLibrary.Published
            .OrderBy(item => item.Category)
            .ThenBy(item => item.DisplayName)
            .Select(item => new SelectListItem
            {
                Value = item.Key,
                Text = $"{item.Category}: {item.DisplayName}"
            })
            .ToList();

        FunctionRows = functions
            .Select(function =>
            {
                var functionAssignment = assignments
                    .Where(item =>
                        item.ScopeType == VehicleSchematicAssignmentService.FunctionScope &&
                        string.Equals(item.VehicleFunction, function.Name, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
                    .FirstOrDefault();

                var functionSubtypes = subtypes
                    .Where(subtype => subtype.VehicleFunctionSetupId == function.Id)
                    .Select(subtype =>
                    {
                        var subtypeAssignment = assignments
                            .Where(item =>
                                item.ScopeType == VehicleSchematicAssignmentService.SubtypeScope &&
                                string.Equals(item.VehicleFunction, function.Name, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(item.VehicleSubtype, subtype.Name, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
                            .FirstOrDefault();

                        return new VehicleStructureSubtypeRow(
                            subtype.Id,
                            subtype.Name,
                            ResolveSchematicName(subtypeAssignment?.SchematicKey));
                    })
                    .ToList();

                return new VehicleStructureFunctionRow(
                    function.Id,
                    function.Name,
                    ResolveSchematicName(functionAssignment?.SchematicKey),
                    functionSubtypes);
            })
            .ToList();
    }

    private async Task<AppUser?> RequireCurrentUserAsync()
    {
        return await _currentUser.GetCurrentUserAsync();
    }

    private async Task<int> NextFunctionSortOrderAsync(int companyId)
    {
        var existing = await _db.VehicleFunctionSetups
            .Where(item => item.CompanyId == companyId)
            .Select(item => (int?)item.SortOrder)
            .MaxAsync();
        return (existing ?? 0) + 10;
    }

    private async Task<int> NextSubtypeSortOrderAsync(int companyId, int functionSetupId)
    {
        var existing = await _db.VehicleSubtypeSetups
            .Where(item => item.CompanyId == companyId && item.VehicleFunctionSetupId == functionSetupId)
            .Select(item => (int?)item.SortOrder)
            .MaxAsync();
        return (existing ?? 0) + 10;
    }

    private static string ResolveSchematicName(string? schematicKey)
    {
        return VehicleSchematicLibrary.Find(schematicKey ?? string.Empty)?.DisplayName ?? "No default schematic";
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

    public sealed record VehicleStructureFunctionRow(
        int Id,
        string Name,
        string DefaultSchematic,
        IReadOnlyList<VehicleStructureSubtypeRow> Subtypes);

    public sealed record VehicleStructureSubtypeRow(
        int Id,
        string Name,
        string DefaultSchematic);
}
