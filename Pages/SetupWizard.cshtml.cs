using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class SetupWizardModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly VectorDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly AssetRegisterSetupService _assetRegisterSetup;
    private readonly ChecklistSetupService _checklistSetup;
    private readonly ReadinessEngineSetupService _readinessEngineSetup;
    private readonly AuditTrailService _auditTrail;

    public SetupWizardModel(
        CurrentUserService currentUser,
        VectorDbContext db,
        IWebHostEnvironment environment,
        AssetRegisterSetupService assetRegisterSetup,
        ChecklistSetupService checklistSetup,
        ReadinessEngineSetupService readinessEngineSetup,
        AuditTrailService auditTrail)
    {
        _currentUser = currentUser;
        _db = db;
        _environment = environment;
        _assetRegisterSetup = assetRegisterSetup;
        _checklistSetup = checklistSetup;
        _readinessEngineSetup = readinessEngineSetup;
        _auditTrail = auditTrail;
    }

    public string ClientName { get; private set; } = CompanyBranding.DefaultCompanyName;
    public string CompanyLogoPath { get; private set; } = CompanyBranding.DefaultLogoPath;
    public string SetupStatus { get; private set; } = CompanyBranding.BrandingStatusIncomplete;
    public string SignedInName { get; private set; } = string.Empty;
    public string SignedInRole { get; private set; } = string.Empty;
    public bool CanManageSetup { get; private set; }
    public bool IsSetupComplete { get; private set; }
    public bool IsProgressReviewMode { get; private set; }
    public SetupWizardStepDefinition CurrentStep { get; private set; } = SetupWizardProgress.Steps[0];
    public IReadOnlySet<string> CompletedStepKeys { get; private set; } = new HashSet<string>();
    public IReadOnlyList<SetupWizardStepDefinition> SetupSteps { get; private set; } = SetupWizardProgress.Steps;
    public int CompletedStepCount { get; private set; }
    public int RemainingStepCount { get; private set; }
    public bool IsReviewStep { get; private set; }
    public IReadOnlyList<SetupReviewItem> MissingRequiredItems { get; private set; } = [];
    public IReadOnlyList<SetupReviewItem> OptionalDeferredItems { get; private set; } = [];
    public IReadOnlyList<SetupReviewItem> ImmediateActionItems { get; private set; } = [];
    public bool HasRequiredSetupGaps => MissingRequiredItems.Count > 0;
    public string? CompletionError { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? Mode { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var company = currentUser.Company ?? await _currentUser.GetCurrentCompanyAsync();
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        var canManageSetup = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        var progressReviewRequested = IsProgressReviewRequest();
        if (progressReviewRequested && !canManageSetup)
        {
            return RedirectToPage("/Home", new { permissionDenied = "true" });
        }

        if (CompanySetupState.IsSetupComplete(company))
        {
            if (!progressReviewRequested)
            {
                return RedirectToPage("/Home");
            }

            await ApplyPageStateAsync(company, currentUser, progressReviewRequested: true);
            return Page();
        }

        var currentStepChanged = SetupWizardProgress.EnsureCurrentStep(company);
        var setupStartedAuditAdded = await AddSetupStartedAuditIfNeededAsync(company, currentUser);
        if (currentStepChanged || setupStartedAuditAdded)
        {
            await _db.SaveChangesAsync();
        }

        await ApplyPageStateAsync(company, currentUser);
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteSetupAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        var company = currentUser.Company ?? await _currentUser.GetCurrentCompanyAsync();
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        await ApplyPageStateAsync(company, currentUser);
        if (!CanManageSetup)
        {
            CompletionError = "Only senior management can complete company setup.";
            return Page();
        }

        if (!IsReviewStep)
        {
            CompletionError = "Setup can only be completed from the Step 9 review screen.";
            return Page();
        }

        if (HasRequiredSetupGaps)
        {
            CompletionError = "Resolve the required setup items before completing setup.";
            return Page();
        }

        SetupWizardProgress.MarkStepComplete(company, SetupWizardProgress.ReviewStepKey);
        company.BrandingStatus = CompanyBranding.BrandingStatusConfigured;
        company.SetupWizardUpdatedAtUtc = DateTime.UtcNow;
        company.UpdatedAtUtc = DateTime.UtcNow;

        var completedStepCount = SetupSteps.Count(step =>
            CompletedStepKeys.Contains(step.Key)
            || step.Key.Equals(SetupWizardProgress.ReviewStepKey, StringComparison.OrdinalIgnoreCase));
        _auditTrail.Record(
            currentUser,
            "Setup wizard completed",
            "Company",
            company.Id,
            $"Setup wizard completed by {currentUser.FullName} ({currentUser.AppRole?.Name ?? "Unknown access"}). Completed steps: {completedStepCount}/{SetupSteps.Count}. Deferred optional items: {BuildDeferredSetupAuditSummary(OptionalDeferredItems)}. Normal Home access unlocked.");

        await _db.SaveChangesAsync();

        return RedirectToPage("/Home");
    }

    private async Task ApplyPageStateAsync(Company company, AppUser currentUser, bool progressReviewRequested = false)
    {
        ClientName = CompanyBranding.GetDisplayCompanyName(company);
        CompanyLogoPath = CompanyBranding.GetLogoPath(_environment, company);
        SetupStatus = CompanySetupState.DisplayStatus(company);
        SignedInName = currentUser.FullName;
        SignedInRole = currentUser.AppRole?.Name ?? string.Empty;
        CanManageSetup = CurrentUserService.IsSeniorAccessRole(SignedInRole);
        IsSetupComplete = CompanySetupState.IsSetupComplete(company);
        IsProgressReviewMode = progressReviewRequested && IsSetupComplete;
        CurrentStep = IsProgressReviewMode
            ? SetupWizardProgress.Steps.First(step => step.Key.Equals(SetupWizardProgress.ReviewStepKey, StringComparison.OrdinalIgnoreCase))
            : SetupWizardProgress.GetCurrentStep(company);
        CompletedStepKeys = SetupWizardProgress.GetCompletedStepKeys(company);
        SetupSteps = SetupWizardProgress.Steps;
        CompletedStepCount = SetupSteps.Count(step => CompletedStepKeys.Contains(step.Key));
        RemainingStepCount = SetupSteps.Count - CompletedStepCount;
        IsReviewStep = string.Equals(CurrentStep.Key, SetupWizardProgress.ReviewStepKey, StringComparison.OrdinalIgnoreCase);
        if (IsReviewStep)
        {
            await BuildSetupReviewAsync(company);
        }

        ViewData["ClientName"] = ClientName;
    }

    private async Task BuildSetupReviewAsync(Company company)
    {
        var missingRequiredItems = new List<SetupReviewItem>();
        var optionalDeferredItems = new List<SetupReviewItem>();
        var immediateActionItems = new List<SetupReviewItem>();

        AddRequiredIfBlank(
            missingRequiredItems,
            company.Name,
            "Company name is missing",
            "Open Company Identity and save the legal or trading name used in this tenant.",
            "/CompanyName");
        AddRequiredIfBlank(
            missingRequiredItems,
            company.ContactEmail,
            "Company contact email is missing",
            "Open Company Identity and save the main operational or administrative contact email.",
            "/CompanyName");
        AddRequiredIfBlank(
            missingRequiredItems,
            company.Country,
            "Country is missing",
            "Open Company Identity and save the country so regional compliance and defaults can be applied later.",
            "/CompanyName");
        AddRequiredIfBlank(
            missingRequiredItems,
            company.Timezone,
            "Timezone is missing",
            "Open Company Identity and save the timezone used for shifts, reports, and audit evidence.",
            "/CompanyName");

        if (string.IsNullOrWhiteSpace(company.LogoStoragePath) || company.LogoRemoved)
        {
            optionalDeferredItems.Add(new SetupReviewItem(
                "Company logo not uploaded",
                "The app can operate without a logo, but the logged-in tenant pages will use the no-logo branding state until one is uploaded.",
                "Upload logo",
                "/LogoUpload",
                "optional"));
        }

        var activeAreaCount = await _db.OperationalAreas
            .AsNoTracking()
            .CountAsync(area => area.CompanyId == company.Id && area.Status == "Active");
        var activeStorageCount = await _db.StorageLocations
            .AsNoTracking()
            .CountAsync(location => location.CompanyId == company.Id && location.Status == "Active");
        if (string.IsNullOrWhiteSpace(company.OperationalStructureMode))
        {
            missingRequiredItems.Add(new SetupReviewItem(
                "Operational structure mode is missing",
                "Choose whether the company works with a flat structure, regions with areas, or bases with areas.",
                "Open Operational Structure",
                "/OperationalStructureSetup",
                "required"));
        }
        if (activeAreaCount == 0)
        {
            missingRequiredItems.Add(new SetupReviewItem(
                "No operational areas, bases, or regions are configured",
                "Add at least one active operational structure record so registers, managers, reports, and movements have a real destination.",
                "Open Operational Structure",
                "/OperationalStructureSetup",
                "required"));
        }
        if (activeStorageCount == 0)
        {
            missingRequiredItems.Add(new SetupReviewItem(
                "No storage spaces are configured",
                "Add at least one active storage space so stock, medication, and equipment can be allocated to real locations.",
                "Open Operational Structure",
                "/OperationalStructureSetup",
                "required"));
        }

        var activeVehicleFunctionCount = await _db.VehicleFunctionSetups
            .AsNoTracking()
            .CountAsync(function => function.CompanyId == company.Id && function.Status == "Active");
        var activeVehicleSubtypeCount = await _db.VehicleSubtypeSetups
            .AsNoTracking()
            .CountAsync(subtype => subtype.CompanyId == company.Id && subtype.Status == "Active");
        if (activeVehicleFunctionCount == 0)
        {
            missingRequiredItems.Add(new SetupReviewItem(
                "No vehicle functions are configured",
                "Add at least one broad function such as Ambulance or Response Vehicle before onboarding vehicles or assigning checklists.",
                "Open Vehicle Structure",
                "/VehicleStructureSetup",
                "required"));
        }
        if (activeVehicleSubtypeCount == 0)
        {
            missingRequiredItems.Add(new SetupReviewItem(
                "No vehicle subtypes are configured",
                "Add at least one client-defined subtype so registers, checklists, and schematic assignments can target the correct units.",
                "Open Vehicle Structure",
                "/VehicleStructureSetup",
                "required"));
        }

        var activeQualificationCount = await _db.StaffQualificationSetups
            .AsNoTracking()
            .CountAsync(qualification => qualification.CompanyId == company.Id && qualification.Status == "Active");
        if (activeQualificationCount == 0)
        {
            missingRequiredItems.Add(new SetupReviewItem(
                "No clinical qualification or scope options are configured",
                "Add the qualification/scope options staff will use, such as BLS, ILS, ALS, ECP, AEA, or local equivalents.",
                "Open Staff Structure",
                "/StaffStructureSetup",
                "required"));
        }
        AddRequiredIfBlank(
            missingRequiredItems,
            company.StaffIdFormat,
            "Staff ID format is missing",
            "Define the staff ID format before staff profiles are created.",
            "/StaffStructureSetup");

        if (!company.AccessModelDefaultsConfigured)
        {
            missingRequiredItems.Add(new SetupReviewItem(
                "Access model defaults are not configured",
                "Save the default permissions for senior management, operational management, and staff before real users begin operating.",
                "Open Access Model",
                "/AccessModelSetup",
                "required"));
        }
        if (!company.AssetRegisterSetupConfigured)
        {
            missingRequiredItems.Add(new SetupReviewItem(
                "Asset register setup choices are not saved",
                "Choose whether each register will be built manually now, imported later, or deferred.",
                "Open Asset Registers",
                "/AssetRegisterSetup",
                "required"));
        }
        if (!company.ChecklistSetupConfigured)
        {
            missingRequiredItems.Add(new SetupReviewItem(
                "Checklist setup choices are not saved",
                "Choose how daily checks and Full Audit checklists will be built or deferred.",
                "Open Checklist Setup",
                "/ChecklistSetup",
                "required"));
        }
        if (!company.ReadinessEngineSetupConfigured)
        {
            missingRequiredItems.Add(new SetupReviewItem(
                "Readiness engine setup choices are not saved",
                "Choose whether readiness scoring will be activated, deferred, or customized later.",
                "Open Readiness Engine Setup",
                "/ReadinessEngineSetup",
                "required"));
        }

        foreach (var step in SetupSteps.Where(step => step.Number < CurrentStep.Number && !CompletedStepKeys.Contains(step.Key)))
        {
            missingRequiredItems.Add(new SetupReviewItem(
                $"{step.Title} is not marked complete",
                "Return to this setup step and save it before the final setup completion step.",
                $"Open {step.Title}",
                GetSetupStepUrl(step.Key),
                "required"));
        }

        if (company.AssetRegisterSetupConfigured)
        {
            AddAssetRegisterReview(company, optionalDeferredItems, immediateActionItems);
        }

        if (company.ChecklistSetupConfigured)
        {
            AddChecklistReview(company, optionalDeferredItems, immediateActionItems);
        }

        if (company.ReadinessEngineSetupConfigured)
        {
            AddReadinessReview(company, optionalDeferredItems, immediateActionItems);
        }

        if (IsSetupComplete)
        {
            immediateActionItems.Add(new SetupReviewItem(
                "Normal Home flow is unlocked",
                "Company setup is complete. Use this page to review deferred setup items and jump back into the existing setup records when changes are needed.",
                "Open Home",
                "/Home",
                "action"));
        }
        else
        {
            immediateActionItems.Add(new SetupReviewItem(
                "Open normal Home flow after final setup completion",
                "When the required setup items are complete, use the completion button below to unlock normal Home access for this company.",
                "Complete setup below",
                null,
                "action"));
        }

        MissingRequiredItems = missingRequiredItems;
        OptionalDeferredItems = optionalDeferredItems;
        ImmediateActionItems = immediateActionItems
            .GroupBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private void AddAssetRegisterReview(
        Company company,
        ICollection<SetupReviewItem> optionalDeferredItems,
        ICollection<SetupReviewItem> immediateActionItems)
    {
        var snapshot = _assetRegisterSetup.GetSnapshot(company);
        foreach (var row in _assetRegisterSetup.BuildRows(snapshot))
        {
            if (row.Choice == AssetRegisterSetupService.ChoiceManualNow && row.ActionUrl is not null)
            {
                immediateActionItems.Add(new SetupReviewItem(
                    $"Build {row.Name.ToLowerInvariant()} register",
                    row.ActionHelp,
                    row.ActionLabel,
                    row.ActionUrl,
                    "action"));
            }
            else if (row.Choice is AssetRegisterSetupService.ChoiceImportLater or AssetRegisterSetupService.ChoiceDefer)
            {
                optionalDeferredItems.Add(new SetupReviewItem(
                    $"{row.Name} register: {row.ChoiceLabel}",
                    row.ActionHelp,
                    row.ActionUrl is null ? null : row.ActionLabel,
                    row.ActionUrl,
                    "optional"));
            }
        }
    }

    private void AddChecklistReview(
        Company company,
        ICollection<SetupReviewItem> optionalDeferredItems,
        ICollection<SetupReviewItem> immediateActionItems)
    {
        var snapshot = _checklistSetup.GetSnapshot(company);
        foreach (var row in _checklistSetup.BuildRows(snapshot))
        {
            if (row.ActionUrl is not null)
            {
                immediateActionItems.Add(new SetupReviewItem(
                    row.Name,
                    row.Description,
                    row.ActionLabel,
                    row.ActionUrl,
                    "action"));
            }

            var isDeferredDaily = row.Name.Contains("Daily", StringComparison.OrdinalIgnoreCase)
                && snapshot.DailyChecklistChoice is ChecklistSetupService.DailyChoiceImportLater or ChecklistSetupService.DailyChoiceDefer;
            var isDeferredFullAudit = row.Name.Contains("Full Audit", StringComparison.OrdinalIgnoreCase)
                && snapshot.FullAuditChoice is ChecklistSetupService.FullAuditChoiceConfigureLater or ChecklistSetupService.FullAuditChoiceDefer;
            if (isDeferredDaily || isDeferredFullAudit)
            {
                optionalDeferredItems.Add(new SetupReviewItem(
                    $"{row.Name}: {row.ChoiceLabel}",
                    row.Description,
                    row.ActionUrl is null ? null : row.ActionLabel,
                    row.ActionUrl,
                    "optional"));
            }
        }
    }

    private void AddReadinessReview(
        Company company,
        ICollection<SetupReviewItem> optionalDeferredItems,
        ICollection<SetupReviewItem> immediateActionItems)
    {
        var snapshot = _readinessEngineSetup.GetSnapshot(company);
        foreach (var row in _readinessEngineSetup.BuildRows(snapshot))
        {
            if (row.ActionUrl is not null)
            {
                immediateActionItems.Add(new SetupReviewItem(
                    row.Name,
                    row.Description,
                    row.ActionLabel,
                    row.ActionUrl,
                    "action"));
            }
        }

        if (!snapshot.ReadinessScoringActivated
            || snapshot.ScoringChoice is ReadinessEngineSetupService.ChoiceCustomizeLater or ReadinessEngineSetupService.ChoiceDefer)
        {
            optionalDeferredItems.Add(new SetupReviewItem(
                $"Readiness scoring: {ReadinessEngineSetupService.DescribeScoringChoice(snapshot.ScoringChoice)}",
                snapshot.ReadinessScoringActivated
                    ? "Readiness scoring intent is active, but scoring rules still need senior review and explicit publish before live use."
                    : "Readiness scoring remains deferred until a senior user activates and publishes the engine.",
                "Open Readiness Engine",
                "/ReadinessEngine",
                "optional"));
        }
    }

    private static void AddRequiredIfBlank(
        ICollection<SetupReviewItem> items,
        string? value,
        string title,
        string detail,
        string actionUrl)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        items.Add(new SetupReviewItem(title, detail, "Fix now", actionUrl, "required"));
    }

    private static string? GetSetupStepUrl(string stepKey)
    {
        return stepKey switch
        {
            SetupWizardProgress.CompanyIdentityStepKey => "/CompanyName",
            SetupWizardProgress.OperationalStructureStepKey => "/OperationalStructureSetup",
            SetupWizardProgress.VehicleStructureStepKey => "/VehicleStructureSetup",
            SetupWizardProgress.StaffStructureStepKey => "/StaffStructureSetup",
            SetupWizardProgress.AccessModelStepKey => "/AccessModelSetup",
            SetupWizardProgress.AssetRegisterStepKey => "/AssetRegisterSetup",
            SetupWizardProgress.ChecklistSetupStepKey => "/ChecklistSetup",
            SetupWizardProgress.ReadinessEngineSetupStepKey => "/ReadinessEngineSetup",
            _ => null
        };
    }

    private bool IsProgressReviewRequest()
    {
        return string.Equals(Mode, "progress", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Mode, "review", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> AddSetupStartedAuditIfNeededAsync(Company company, AppUser currentUser)
    {
        var alreadyLogged = await _db.AuditLogs
            .AsNoTracking()
            .AnyAsync(log => log.CompanyId == company.Id
                && log.EntityType == "Company"
                && log.EntityId == company.Id
                && log.Action == "Setup wizard started");
        if (alreadyLogged)
        {
            return false;
        }

        var currentStep = SetupWizardProgress.GetCurrentStep(company);
        _auditTrail.Record(
            currentUser,
            "Setup wizard started",
            "Company",
            company.Id,
            $"Setup wizard started by {currentUser.FullName} ({currentUser.AppRole?.Name ?? "Unknown access"}). Current step: {currentStep.Number} - {currentStep.Title}.");
        return true;
    }

    private static string BuildDeferredSetupAuditSummary(IEnumerable<SetupReviewItem> optionalDeferredItems)
    {
        var deferredItems = optionalDeferredItems
            .Select(item => item.Title)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
        return deferredItems.Count == 0 ? "none" : string.Join("; ", deferredItems);
    }
}

public sealed record SetupReviewItem(
    string Title,
    string Detail,
    string? ActionLabel = null,
    string? ActionUrl = null,
    string Severity = "info");
