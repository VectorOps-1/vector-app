using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AccessModelSetupModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly AccessModelSetupService _accessModel;

    public AccessModelSetupModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        AccessModelSetupService accessModel)
    {
        _db = db;
        _currentUser = currentUser;
        _accessModel = accessModel;
    }

    [BindProperty] public string OperationalManagerScopeBehavior { get; set; } = AccessModelSetupService.ScopeAssignedAreasOnly;
    [BindProperty] public bool OperationalManagersCanDraftChecklistChanges { get; set; }
    [BindProperty] public bool OperationalManagersCanEditRegisters { get; set; }
    [BindProperty] public bool OperationalManagersCanApproveStockOrders { get; set; }
    [BindProperty] public bool OperationalManagersRequestOnlyMode { get; set; }
    [BindProperty] public List<string> SeniorManagerPermissionKeys { get; set; } = new();
    [BindProperty] public List<string> StaffPermissionKeys { get; set; } = new();

    public string ClientName { get; private set; } = CompanyBranding.DefaultCompanyName;
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public IReadOnlyList<AccessPermissionGroup> PermissionGroups => AccessPermissionCatalog.Groups;
    public IReadOnlyList<string> CompanyOwnerPermissionKeys { get; private set; } = UserActionPermissions.All.ToList();
    public IReadOnlyList<string> OperationalManagerPermissionKeys { get; private set; } = AccessModelSetupService.ProductOperationalManagerDefault();
    public HashSet<string> CompanyOwnerPermissionKeySet { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> SeniorManagerPermissionKeySet { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> OperationalManagerPermissionKeySet { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> StaffPermissionKeySet { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public string SeniorManagerPermissionSummary { get; private set; } = string.Empty;
    public string OperationalManagerPermissionSummary { get; private set; } = string.Empty;
    public string StaffPermissionSummary { get; private set; } = string.Empty;
    public List<SelectListItem> ScopeBehaviorOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await RequireSeniorUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveDefaultsAsync()
    {
        var currentUser = await RequireSeniorUserAsync();
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

        var seniorDefaults = AccessPermissionCatalog.NormalizePermissionKeys(SeniorManagerPermissionKeys);
        var staffDefaults = AccessPermissionCatalog.NormalizePermissionKeys(StaffPermissionKeys);
        var operationalDefaults = AccessModelSetupService.BuildOperationalManagerPermissions(
            OperationalManagersCanDraftChecklistChanges,
            OperationalManagersCanEditRegisters,
            OperationalManagersCanApproveStockOrders,
            OperationalManagersRequestOnlyMode);

        if (seniorDefaults.Count == 0)
        {
            ModelState.AddModelError(nameof(SeniorManagerPermissionKeys), "Senior management must have at least one saved permission default.");
        }

        if (staffDefaults.Count == 0)
        {
            ModelState.AddModelError(nameof(StaffPermissionKeys), "Staff must have at least one saved permission default.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageStateAsync(currentUser.CompanyId, preservePostedValues: true);
            return Page();
        }

        company.OperationalManagerScopeBehavior = AccessModelSetupService.NormalizeScopeBehavior(OperationalManagerScopeBehavior);
        company.CompanyOwnerDefaultPermissionKeys = AccessPermissionCatalog.SerializePermissionKeys(UserActionPermissions.All);
        company.SeniorManagerDefaultPermissionKeys = AccessPermissionCatalog.SerializePermissionKeys(seniorDefaults);
        company.OperationalManagerDefaultPermissionKeys = AccessPermissionCatalog.SerializePermissionKeys(operationalDefaults);
        company.StaffDefaultPermissionKeys = AccessPermissionCatalog.SerializePermissionKeys(staffDefaults);
        company.AccessModelDefaultsConfigured = true;
        company.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(BuildAudit(
            currentUser,
            "Access model defaults updated",
            "Company",
            company.Id,
            $"Access model defaults saved. Operational manager scope: {AccessModelSetupService.DescribeScopeBehavior(company.OperationalManagerScopeBehavior)}. Ops draft checklist: {OperationalManagersCanDraftChecklistChanges}; ops edit registers: {OperationalManagersCanEditRegisters}; ops approve stock: {OperationalManagersCanApproveStockOrders}; ops request-only: {OperationalManagersRequestOnlyMode}."));

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Access model defaults saved.";
        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteStepAsync()
    {
        var currentUser = await RequireSeniorUserAsync();
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

        if (!company.AccessModelDefaultsConfigured)
        {
            StatusMessage = "Save access model defaults before completing this setup step.";
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        SetupWizardProgress.MarkStepComplete(company, SetupWizardProgress.AccessModelStepKey);
        company.UpdatedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(BuildAudit(currentUser, "Setup step completed", "Company", company.Id, "Access model setup completed."));
        await _db.SaveChangesAsync();

        return RedirectToPage("/SetupWizard");
    }

    private async Task LoadPageStateAsync(int companyId, bool preservePostedValues = false)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId);
        ClientName = CompanyBranding.GetDisplayCompanyName(company);

        var snapshot = await _accessModel.GetSnapshotAsync(companyId);
        if (!preservePostedValues)
        {
            OperationalManagerScopeBehavior = snapshot.OperationalManagerScopeBehavior;
            OperationalManagersCanDraftChecklistChanges = snapshot.OperationalManagersCanDraftChecklistChanges;
            OperationalManagersCanEditRegisters = snapshot.OperationalManagersCanEditRegisters;
            OperationalManagersCanApproveStockOrders = snapshot.OperationalManagersCanApproveStockOrders;
            OperationalManagersRequestOnlyMode = snapshot.OperationalManagersRequestOnlyMode;
            SeniorManagerPermissionKeys = snapshot.SeniorManagerPermissionKeys.ToList();
            StaffPermissionKeys = snapshot.StaffPermissionKeys.ToList();
        }

        CompanyOwnerPermissionKeys = snapshot.CompanyOwnerPermissionKeys;
        OperationalManagerPermissionKeys = AccessModelSetupService.BuildOperationalManagerPermissions(
            OperationalManagersCanDraftChecklistChanges,
            OperationalManagersCanEditRegisters,
            OperationalManagersCanApproveStockOrders,
            OperationalManagersRequestOnlyMode);

        CompanyOwnerPermissionKeySet = snapshot.CompanyOwnerPermissionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        SeniorManagerPermissionKeySet = AccessPermissionCatalog.NormalizePermissionKeys(SeniorManagerPermissionKeys).ToHashSet(StringComparer.OrdinalIgnoreCase);
        OperationalManagerPermissionKeySet = OperationalManagerPermissionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        StaffPermissionKeySet = AccessPermissionCatalog.NormalizePermissionKeys(StaffPermissionKeys).ToHashSet(StringComparer.OrdinalIgnoreCase);

        SeniorManagerPermissionSummary = AccessPermissionCatalog.DescribePermissionSummary(SeniorManagerPermissionKeySet.ToList());
        OperationalManagerPermissionSummary = AccessPermissionCatalog.DescribePermissionSummary(OperationalManagerPermissionKeySet.ToList());
        StaffPermissionSummary = AccessPermissionCatalog.DescribePermissionSummary(StaffPermissionKeySet.ToList());

        ScopeBehaviorOptions = new List<SelectListItem>
        {
            new()
            {
                Value = AccessModelSetupService.ScopeAssignedAreasOnly,
                Text = "Assigned areas only",
                Selected = OperationalManagerScopeBehavior == AccessModelSetupService.ScopeAssignedAreasOnly
            },
            new()
            {
                Value = AccessModelSetupService.ScopeAllOperationalAreas,
                Text = "All operational areas",
                Selected = OperationalManagerScopeBehavior == AccessModelSetupService.ScopeAllOperationalAreas
            }
        };
    }

    private async Task<AppUser?> RequireSeniorUserAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null || !CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return null;
        }

        return currentUser;
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
}
