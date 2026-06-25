using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ReadinessEngineSetupModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ReadinessEngineSetupService _readinessEngineSetup;

    public ReadinessEngineSetupModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ReadinessEngineSetupService readinessEngineSetup)
    {
        _db = db;
        _currentUser = currentUser;
        _readinessEngineSetup = readinessEngineSetup;
    }

    [BindProperty] public string ReadinessScoringSetupChoice { get; set; } = ReadinessEngineSetupService.ChoiceDefer;
    [BindProperty] public bool ReadinessScoringActivated { get; set; }
    [BindProperty] public bool RequireSeniorApprovalForScoringChanges { get; set; } = true;
    [BindProperty] public string? ReadinessEngineSetupNotes { get; set; }

    public string ClientName { get; private set; } = CompanyBranding.DefaultCompanyName;
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public IReadOnlyList<SelectListItem> ScoringChoiceItems { get; private set; } = [];
    public IReadOnlyList<ReadinessEngineSetupRouteRow> ReadinessRows { get; private set; } = [];

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

        var normalizedScoringChoice = ReadinessEngineSetupService.NormalizeScoringChoice(ReadinessScoringSetupChoice);
        if (normalizedScoringChoice == ReadinessEngineSetupService.ChoiceDefer && ReadinessScoringActivated)
        {
            ModelState.AddModelError(nameof(ReadinessScoringActivated), "Readiness scoring cannot be activated while the scoring setup choice is deferred.");
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

        company.ReadinessScoringSetupChoice = normalizedScoringChoice;
        company.ReadinessScoringActivated = ReadinessScoringActivated;
        company.RequireSeniorApprovalForScoringChanges = RequireSeniorApprovalForScoringChanges;
        company.ReadinessEngineSetupNotes = ReadinessEngineSetupService.NormalizeNotes(ReadinessEngineSetupNotes);
        company.ReadinessEngineSetupConfigured = true;
        company.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(BuildAudit(
            currentUser,
            "Readiness engine setup choices updated",
            "Company",
            company.Id,
            $"Readiness engine setup choices saved. Scoring: {ReadinessEngineSetupService.DescribeScoringChoice(company.ReadinessScoringSetupChoice)}; activation: {ReadinessEngineSetupService.DescribeActivation(company.ReadinessScoringActivated)}; scoring changes: {ReadinessEngineSetupService.DescribeApproval(company.RequireSeniorApprovalForScoringChanges)}."));
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Readiness engine setup choices saved.";
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

        if (!company.ReadinessEngineSetupConfigured)
        {
            StatusMessage = "Save readiness engine setup choices before completing this setup step.";
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        SetupWizardProgress.MarkStepComplete(company, SetupWizardProgress.ReadinessEngineSetupStepKey);
        company.UpdatedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(BuildAudit(currentUser, "Setup step completed", "Company", company.Id, "Readiness engine setup completed."));
        await _db.SaveChangesAsync();

        return RedirectToPage("/SetupWizard");
    }

    private async Task LoadPageStateAsync(int companyId)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId);

        ClientName = CompanyBranding.GetDisplayCompanyName(company);
        var snapshot = _readinessEngineSetup.GetSnapshot(company);

        if (!Request.HasFormContentType)
        {
            ReadinessScoringSetupChoice = snapshot.ScoringChoice;
            ReadinessScoringActivated = snapshot.ReadinessScoringActivated;
            RequireSeniorApprovalForScoringChanges = snapshot.RequireSeniorApprovalForScoringChanges;
            ReadinessEngineSetupNotes = snapshot.Notes;
        }

        ScoringChoiceItems = _readinessEngineSetup.GetScoringChoices()
            .Select(option => new SelectListItem
            {
                Value = option.Value,
                Text = option.Label
            })
            .ToList();

        ReadinessRows = _readinessEngineSetup.BuildRows(new ReadinessEngineSetupSnapshot(
            snapshot.IsConfigured,
            ReadinessScoringSetupChoice,
            ReadinessScoringActivated,
            RequireSeniorApprovalForScoringChanges,
            ReadinessEngineSetupNotes));
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
