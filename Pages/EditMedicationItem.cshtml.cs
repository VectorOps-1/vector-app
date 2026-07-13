using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditMedicationItemModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;
    private readonly IUserActionAuthorizationService _authorization;

    public EditMedicationItemModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        LocationOptionService locationOptions,
        IUserActionAuthorizationService authorization)
    {
        _db = db;
        _currentUser = currentUser;
        _locationOptions = locationOptions;
        _authorization = authorization;
    }

    [BindProperty] public int MedicationItemId { get; set; }
    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public string? MedicationCode { get; set; }
    [BindProperty] public string? MedicationType { get; set; }
    [BindProperty] public string? Schedule { get; set; }
    [BindProperty] public string? BatchNumber { get; set; }
    [BindProperty] public int? Quantity { get; set; }
    [BindProperty] public DateTime? ExpiryDate { get; set; }
    [BindProperty] public string? StorageLocation { get; set; }
    [BindProperty] public string Status { get; set; } = "Active";
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public string? ReturnUrl { get; set; }

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public List<SelectListItem> LocationOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int medicationItemId, string? returnUrl)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var medicationItem = await _db.MedicationItems
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == medicationItemId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (medicationItem is null)
        {
            return RedirectToPage("/MedicationRegister", new { view = "register" });
        }

        if (!await _authorization.CanManageAreaScopedRecordAsync(
                currentUser,
                UserActionPermissions.RegistersMedicationEdit,
                medicationItem.CurrentOperationalAreaId))
        {
            return RedirectToPage("/MedicationRegister", new { view = "register", permissionDenied = "true" });
        }

        LoadFromMedicationItem(medicationItem, returnUrl);
        await LoadOptionsAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadOptionsAsync(currentUser.CompanyId);

        var medicationItem = await _db.MedicationItems
            .FirstOrDefaultAsync(item =>
                item.Id == MedicationItemId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (medicationItem is null)
        {
            StatusMessage = "Medication item was not found.";
            return Page();
        }

        if (!await _authorization.CanManageAreaScopedRecordAsync(
                currentUser,
                UserActionPermissions.RegistersMedicationEdit,
                medicationItem.CurrentOperationalAreaId))
        {
            StatusMessage = "You do not have permission to edit this medication item.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Enter the medication name before saving.";
            return Page();
        }

        var now = DateTime.UtcNow;
        var previousSummary = $"{medicationItem.Name} / {medicationItem.MedicationType ?? "No type"} / {medicationItem.BatchNumber ?? "No batch"} / {medicationItem.StorageLocation ?? "No location"}";
        var area = await _locationOptions.FindOperationalAreaAsync(currentUser.CompanyId, StorageLocation);
        var selectedLocation = LocationOptionService.NormalizeSelectedLocation(StorageLocation);

        medicationItem.Name = Name.Trim();
        medicationItem.MedicationCode = NormalizeOptional(MedicationCode);
        medicationItem.MedicationType = NormalizeOptional(MedicationType);
        medicationItem.Schedule = NormalizeOptional(Schedule);
        medicationItem.BatchNumber = NormalizeOptional(BatchNumber);
        medicationItem.Quantity = Quantity;
        medicationItem.ExpiryDate = ExpiryDate;
        medicationItem.StorageLocation = selectedLocation;
        medicationItem.CurrentOperationalAreaId = area?.Id;
        medicationItem.Status = NormalizeOptional(Status) ?? "Active";
        medicationItem.Notes = NormalizeOptional(Notes);
        medicationItem.UpdatedAtUtc = now;

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Medication item updated",
            EntityType = "MedicationItem",
            EntityId = medicationItem.Id,
            Details = $"Medication register updated from [{previousSummary}] to [{medicationItem.Name} / {medicationItem.MedicationType ?? "No type"} / {medicationItem.BatchNumber ?? "No batch"} / {medicationItem.StorageLocation ?? "No location"}].",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Medication item updated.";
        return Page();
    }

    private async Task LoadOptionsAsync(int companyId)
    {
        LocationOptions = await _locationOptions.GetAssetLocationOptionsAsync(companyId);
    }

    private void LoadFromMedicationItem(MedicationItem medicationItem, string? returnUrl)
    {
        MedicationItemId = medicationItem.Id;
        Name = medicationItem.Name;
        MedicationCode = medicationItem.MedicationCode;
        MedicationType = medicationItem.MedicationType;
        Schedule = medicationItem.Schedule;
        BatchNumber = medicationItem.BatchNumber;
        Quantity = medicationItem.Quantity;
        ExpiryDate = medicationItem.ExpiryDate;
        StorageLocation = medicationItem.StorageLocation;
        Status = medicationItem.Status;
        Notes = medicationItem.Notes;
        ReturnUrl = returnUrl;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();
    }
}
