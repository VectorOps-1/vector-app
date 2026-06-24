using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AssetRegisterSetupModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly AssetRegisterSetupService _assetRegisterSetup;

    public AssetRegisterSetupModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        AssetRegisterSetupService assetRegisterSetup)
    {
        _db = db;
        _currentUser = currentUser;
        _assetRegisterSetup = assetRegisterSetup;
    }

    [BindProperty] public string VehicleRegisterChoice { get; set; } = AssetRegisterSetupService.ChoiceDefer;
    [BindProperty] public string EquipmentRegisterChoice { get; set; } = AssetRegisterSetupService.ChoiceDefer;
    [BindProperty] public string StockRegisterChoice { get; set; } = AssetRegisterSetupService.ChoiceDefer;
    [BindProperty] public string MedicationRegisterChoice { get; set; } = AssetRegisterSetupService.ChoiceDefer;
    [BindProperty] public string StaffRegisterChoice { get; set; } = AssetRegisterSetupService.ChoiceDefer;
    [BindProperty] public string StorageLocationChoice { get; set; } = AssetRegisterSetupService.ChoiceDefer;
    [BindProperty] public string? AssetRegisterSetupNotes { get; set; }

    public string ClientName { get; private set; } = CompanyBranding.DefaultCompanyName;
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public IReadOnlyList<SelectListItem> ChoiceItems { get; private set; } = [];
    public IReadOnlyList<AssetRegisterSetupRow> RegisterRows { get; private set; } = [];

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

        var company = await _db.Companies.FirstOrDefaultAsync(item =>
            item.Id == currentUser.CompanyId &&
            item.Status == "Active");
        if (company is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        company.VehicleRegisterSetupChoice = AssetRegisterSetupService.NormalizeChoice(VehicleRegisterChoice);
        company.EquipmentRegisterSetupChoice = AssetRegisterSetupService.NormalizeChoice(EquipmentRegisterChoice);
        company.StockRegisterSetupChoice = AssetRegisterSetupService.NormalizeChoice(StockRegisterChoice);
        company.MedicationRegisterSetupChoice = AssetRegisterSetupService.NormalizeChoice(MedicationRegisterChoice);
        company.StaffRegisterSetupChoice = AssetRegisterSetupService.NormalizeChoice(StaffRegisterChoice);
        company.StorageLocationSetupChoice = AssetRegisterSetupService.NormalizeChoice(StorageLocationChoice);
        company.AssetRegisterSetupNotes = AssetRegisterSetupService.NormalizeNotes(AssetRegisterSetupNotes);
        company.AssetRegisterSetupConfigured = true;
        company.UpdatedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(BuildAudit(
            currentUser,
            "Asset register setup choices updated",
            "Company",
            company.Id,
            $"Asset register setup choices saved. Vehicles: {AssetRegisterSetupService.DescribeChoice(company.VehicleRegisterSetupChoice)}; Equipment: {AssetRegisterSetupService.DescribeChoice(company.EquipmentRegisterSetupChoice)}; Stock: {AssetRegisterSetupService.DescribeChoice(company.StockRegisterSetupChoice)}; Medication: {AssetRegisterSetupService.DescribeChoice(company.MedicationRegisterSetupChoice)}; Staff: {AssetRegisterSetupService.DescribeChoice(company.StaffRegisterSetupChoice)}; Storage locations: {AssetRegisterSetupService.DescribeChoice(company.StorageLocationSetupChoice)}."));
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Asset register setup choices saved.";
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

        if (!company.AssetRegisterSetupConfigured)
        {
            StatusMessage = "Save asset register setup choices before completing this setup step.";
            await LoadPageStateAsync(currentUser.CompanyId);
            return Page();
        }

        SetupWizardProgress.MarkStepComplete(company, SetupWizardProgress.AssetRegisterStepKey);
        company.UpdatedAtUtc = DateTime.UtcNow;
        _db.AuditLogs.Add(BuildAudit(currentUser, "Setup step completed", "Company", company.Id, "Asset register setup completed."));
        await _db.SaveChangesAsync();

        return RedirectToPage("/SetupWizard");
    }

    private async Task LoadPageStateAsync(int companyId)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == companyId);

        ClientName = CompanyBranding.GetDisplayCompanyName(company);
        var snapshot = _assetRegisterSetup.GetSnapshot(company);

        if (!Request.HasFormContentType)
        {
            VehicleRegisterChoice = snapshot.VehicleRegisterChoice;
            EquipmentRegisterChoice = snapshot.EquipmentRegisterChoice;
            StockRegisterChoice = snapshot.StockRegisterChoice;
            MedicationRegisterChoice = snapshot.MedicationRegisterChoice;
            StaffRegisterChoice = snapshot.StaffRegisterChoice;
            StorageLocationChoice = snapshot.StorageLocationChoice;
            AssetRegisterSetupNotes = snapshot.Notes;
        }

        ChoiceItems = _assetRegisterSetup.GetChoiceOptions()
            .Select(option => new SelectListItem
            {
                Value = option.Value,
                Text = option.Label
            })
            .ToList();

        RegisterRows = _assetRegisterSetup.BuildRows(new AssetRegisterSetupSnapshot(
            snapshot.IsConfigured,
            VehicleRegisterChoice,
            EquipmentRegisterChoice,
            StockRegisterChoice,
            MedicationRegisterChoice,
            StaffRegisterChoice,
            StorageLocationChoice,
            AssetRegisterSetupNotes));
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
