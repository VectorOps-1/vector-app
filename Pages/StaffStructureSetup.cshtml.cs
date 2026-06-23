using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class StaffStructureSetupModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly StaffStructureSetupService _staffStructure;

    public StaffStructureSetupModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        StaffStructureSetupService staffStructure)
    {
        _db = db;
        _currentUser = currentUser;
        _staffStructure = staffStructure;
    }

    [BindProperty] public string? QualificationName { get; set; }
    [BindProperty] public string? QualificationNotes { get; set; }
    [BindProperty] public string? StaffIdFormat { get; set; }
    [BindProperty] public bool PractitionerNumberRequired { get; set; }
    [BindProperty] public bool AnnualLicenseExpiryRequired { get; set; }
    [BindProperty] public bool CpdTrackingRequired { get; set; }
    [BindProperty] public List<string> DefaultProfileFields { get; set; } = new();

    public string ClientName { get; private set; } = CompanyBranding.DefaultCompanyName;
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public IReadOnlyList<string> AvailableDefaultProfileFields { get; private set; } = StaffStructureSetupService.AvailableDefaultProfileFields;
    public List<StaffQualificationRow> QualificationRows { get; private set; } = new();

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

    public async Task<IActionResult> OnPostAddQualificationAsync()
    {
        var currentUser = await RequireCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var name = NormalizeOptional(QualificationName);
        if (name is null)
        {
            ModelState.AddModelError(nameof(QualificationName), "Enter the clinical qualification or scope name.");
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var duplicateExists = await _db.StaffQualificationSetups.AnyAsync(item =>
            item.CompanyId == currentUser.CompanyId &&
            item.Status == "Active" &&
            item.Name == name);
        if (duplicateExists)
        {
            ModelState.AddModelError(nameof(QualificationName), "This clinical qualification or scope already exists.");
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var now = DateTime.UtcNow;
        var qualification = new StaffQualificationSetup
        {
            CompanyId = currentUser.CompanyId,
            Name = name,
            Status = "Active",
            SortOrder = await NextQualificationSortOrderAsync(currentUser.CompanyId),
            Notes = NormalizeOptional(QualificationNotes),
            CreatedAtUtc = now
        };

        _db.StaffQualificationSetups.Add(qualification);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(BuildAudit(
            currentUser,
            "Staff clinical qualification setup created",
            "StaffQualificationSetup",
            qualification.Id,
            $"Clinical qualification/scope created: {qualification.Name}."));
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = $"{qualification.Name} added as a staff clinical qualification/scope option.";
        QualificationName = null;
        QualificationNotes = null;
        await LoadPageStateAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveRulesAsync()
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

        company.StaffIdFormat = NormalizeOptional(StaffIdFormat);
        company.StaffPractitionerNumberRequired = PractitionerNumberRequired;
        company.StaffAnnualLicenseExpiryRequired = AnnualLicenseExpiryRequired;
        company.StaffCpdTrackingRequired = CpdTrackingRequired;
        company.StaffDefaultProfileFields = StaffStructureSetupService.SerializeDefaultProfileFields(DefaultProfileFields);
        company.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(BuildAudit(
            currentUser,
            "Staff structure rules updated",
            "Company",
            company.Id,
            "Staff ID format, credential requirements, CPD tracking, and default staff profile fields updated."));
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Staff structure rules saved.";
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

        var activeQualificationCount = await _db.StaffQualificationSetups.CountAsync(item =>
            item.CompanyId == currentUser.CompanyId &&
            item.Status == "Active");
        if (activeQualificationCount == 0)
        {
            StatusMessage = "Add at least one clinical qualification/scope option before completing staff structure setup.";
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        if (string.IsNullOrWhiteSpace(company.StaffIdFormat))
        {
            StatusMessage = "Save a staff ID format before completing staff structure setup.";
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        SetupWizardProgress.MarkStepComplete(company, SetupWizardProgress.StaffStructureStepKey);
        company.UpdatedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(BuildAudit(currentUser, "Setup step completed", "Company", company.Id, "Staff structure setup completed."));
        await _db.SaveChangesAsync();

        return RedirectToPage("/SetupWizard");
    }

    private async Task LoadPageStateAsync(int companyId)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId);
        ClientName = CompanyBranding.GetDisplayCompanyName(company);

        var snapshot = await _staffStructure.GetSnapshotAsync(companyId);
        var postedRules = Request.HasFormContentType &&
            Request.Form.ContainsKey(nameof(StaffIdFormat));
        if (!postedRules)
        {
            StaffIdFormat = snapshot.StaffIdFormat;
            PractitionerNumberRequired = snapshot.PractitionerNumberRequired;
            AnnualLicenseExpiryRequired = snapshot.AnnualLicenseExpiryRequired;
            CpdTrackingRequired = snapshot.CpdTrackingRequired;
            DefaultProfileFields = snapshot.DefaultProfileFields.ToList();
        }

        QualificationRows = snapshot.Qualifications
            .Select(item => new StaffQualificationRow(item.Id, item.Name, item.SortOrder))
            .ToList();
    }

    private async Task<AppUser?> RequireCurrentUserAsync()
    {
        return await _currentUser.GetCurrentUserAsync();
    }

    private async Task<int> NextQualificationSortOrderAsync(int companyId)
    {
        var existing = await _db.StaffQualificationSetups
            .Where(item => item.CompanyId == companyId)
            .Select(item => (int?)item.SortOrder)
            .MaxAsync();
        return (existing ?? 0) + 10;
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

    public sealed record StaffQualificationRow(int Id, string Name, int SortOrder);
}
