using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ChecklistSetupModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ChecklistSetupService _checklistSetup;

    public ChecklistSetupModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ChecklistSetupService checklistSetup)
    {
        _db = db;
        _currentUser = currentUser;
        _checklistSetup = checklistSetup;
    }

    [BindProperty] public string DailyChecklistChoice { get; set; } = ChecklistSetupService.DailyChoiceDefer;
    [BindProperty] public bool PublishByFunction { get; set; } = true;
    [BindProperty] public bool PublishBySubtype { get; set; } = true;
    [BindProperty] public bool PublishByCallsign { get; set; }
    [BindProperty] public string FullAuditChoice { get; set; } = ChecklistSetupService.FullAuditChoiceConfigureLater;
    [BindProperty] public string? ChecklistSetupNotes { get; set; }

    public string ClientName { get; private set; } = CompanyBranding.DefaultCompanyName;
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public IReadOnlyList<SelectListItem> DailyChecklistChoiceItems { get; private set; } = [];
    public IReadOnlyList<SelectListItem> FullAuditChoiceItems { get; private set; } = [];
    public IReadOnlyList<ChecklistSetupRouteRow> ChecklistRows { get; private set; } = [];

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

    public async Task<IActionResult> OnPostSaveChoicesAsync()
    {
        var currentUser = await RequireSeniorUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (!PublishByFunction && !PublishBySubtype && !PublishByCallsign)
        {
            ModelState.AddModelError(nameof(PublishByFunction), "Select at least one daily checklist publish target.");
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        var company = await _db.Companies.FirstOrDefaultAsync(item =>
            item.Id == currentUser.CompanyId &&
            item.Status == "Active");
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        company.DailyChecklistSetupChoice = ChecklistSetupService.NormalizeDailyChoice(DailyChecklistChoice);
        company.DailyChecklistPublishScopeKeys = ChecklistSetupService.SerializePublishScopeKeys(
            PublishByFunction,
            PublishBySubtype,
            PublishByCallsign);
        company.FullAuditChecklistSetupChoice = ChecklistSetupService.NormalizeFullAuditChoice(FullAuditChoice);
        company.ChecklistSetupNotes = ChecklistSetupService.NormalizeNotes(ChecklistSetupNotes);
        company.ChecklistSetupConfigured = true;
        company.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(BuildAudit(
            currentUser,
            "Checklist setup choices updated",
            "Company",
            company.Id,
            $"Checklist setup choices saved. Daily check: {ChecklistSetupService.DescribeDailyChoice(company.DailyChecklistSetupChoice)}; publish targets: {ChecklistSetupService.DescribePublishScopes(PublishByFunction, PublishBySubtype, PublishByCallsign)}; Full Audit: {ChecklistSetupService.DescribeFullAuditChoice(company.FullAuditChecklistSetupChoice)}."));
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Checklist setup choices saved.";
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

        if (!company.ChecklistSetupConfigured)
        {
            StatusMessage = "Save checklist setup choices before completing this setup step.";
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        SetupWizardProgress.MarkStepComplete(company, SetupWizardProgress.ChecklistSetupStepKey);
        company.UpdatedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(BuildAudit(currentUser, "Setup step completed", "Company", company.Id, "Checklist setup completed."));
        await _db.SaveChangesAsync();

        return RedirectToPage("/SetupWizard");
    }

    private async Task LoadPageStateAsync(int companyId)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId);

        ClientName = CompanyBranding.GetDisplayCompanyName(company);
        var snapshot = _checklistSetup.GetSnapshot(company);

        if (!Request.HasFormContentType)
        {
            DailyChecklistChoice = snapshot.DailyChecklistChoice;
            PublishByFunction = snapshot.PublishByFunction;
            PublishBySubtype = snapshot.PublishBySubtype;
            PublishByCallsign = snapshot.PublishByCallsign;
            FullAuditChoice = snapshot.FullAuditChoice;
            ChecklistSetupNotes = snapshot.Notes;
        }

        DailyChecklistChoiceItems = _checklistSetup.GetDailyChecklistChoices()
            .Select(option => new SelectListItem
            {
                Value = option.Value,
                Text = option.Label
            })
            .ToList();

        FullAuditChoiceItems = _checklistSetup.GetFullAuditChoices()
            .Select(option => new SelectListItem
            {
                Value = option.Value,
                Text = option.Label
            })
            .ToList();

        ChecklistRows = _checklistSetup.BuildRows(new ChecklistSetupSnapshot(
            snapshot.IsConfigured,
            DailyChecklistChoice,
            PublishByFunction,
            PublishBySubtype,
            PublishByCallsign,
            FullAuditChoice,
            ChecklistSetupNotes));
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
