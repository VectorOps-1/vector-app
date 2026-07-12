using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditEquipmentItemModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;
    private readonly IUserActionAuthorizationService _authorization;

    public EditEquipmentItemModel(
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

    [BindProperty] public int EquipmentItemId { get; set; }
    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public string? EquipmentType { get; set; }
    [BindProperty] public string? Model { get; set; }
    [BindProperty] public string? SerialOrAssetId { get; set; }
    [BindProperty] public DateTime? NextServiceDate { get; set; }
    [BindProperty] public bool BatteryRequired { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string Status { get; set; } = "Active";
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public string? ReturnUrl { get; set; }

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public List<SelectListItem> LocationOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int equipmentItemId, string? returnUrl)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var equipmentItem = await _db.EquipmentItems
            .AsNoTracking()
            .Include(item => item.CurrentOperationalArea)
            .FirstOrDefaultAsync(item =>
                item.Id == equipmentItemId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (equipmentItem is null)
        {
            return RedirectToPage("/EquipmentRegister", new { view = "register" });
        }

        if (!await _authorization.CanManageAreaScopedRecordAsync(
                currentUser,
                UserActionPermissions.RegistersEquipmentEdit,
                equipmentItem.CurrentOperationalAreaId))
        {
            return RedirectToPage("/EquipmentRegister", new { view = "register", permissionDenied = "true" });
        }

        LoadFromEquipmentItem(equipmentItem, returnUrl);
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

        var equipmentItem = await _db.EquipmentItems
            .Include(item => item.CurrentOperationalArea)
            .FirstOrDefaultAsync(item =>
                item.Id == EquipmentItemId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (equipmentItem is null)
        {
            StatusMessage = "Equipment item was not found.";
            return Page();
        }

        if (!await _authorization.CanManageAreaScopedRecordAsync(
                currentUser,
                UserActionPermissions.RegistersEquipmentEdit,
                equipmentItem.CurrentOperationalAreaId))
        {
            StatusMessage = "You do not have permission to edit this equipment item.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Enter the equipment name before saving.";
            return Page();
        }

        var now = DateTime.UtcNow;
        var previousLocation = equipmentItem.CurrentOperationalArea == null
            ? equipmentItem.CurrentLocationDetail
            : string.IsNullOrWhiteSpace(equipmentItem.CurrentLocationDetail)
                ? equipmentItem.CurrentOperationalArea.Name
                : $"{equipmentItem.CurrentOperationalArea.Name} - {equipmentItem.CurrentLocationDetail}";
        var previousSummary = $"{equipmentItem.Name} / {equipmentItem.EquipmentType ?? "No type"} / {equipmentItem.SerialOrAssetId ?? "No serial"} / {previousLocation ?? "No location"}";
        var area = await _locationOptions.FindOperationalAreaAsync(currentUser.CompanyId, Location);
        var selectedLocation = LocationOptionService.NormalizeSelectedLocation(Location);

        equipmentItem.Name = Name.Trim();
        equipmentItem.EquipmentType = NormalizeOptional(EquipmentType);
        equipmentItem.Model = NormalizeOptional(Model);
        equipmentItem.SerialOrAssetId = NormalizeOptional(SerialOrAssetId);
        equipmentItem.NextServiceDate = NextServiceDate;
        equipmentItem.BatteryRequired = BatteryRequired;
        equipmentItem.CurrentOperationalAreaId = area?.Id;
        equipmentItem.CurrentLocationDetail = area is null ? selectedLocation : null;
        equipmentItem.Status = NormalizeOptional(Status) ?? "Active";
        equipmentItem.Notes = NormalizeOptional(Notes);
        equipmentItem.UpdatedAtUtc = now;

        var newLocation = area?.Name ?? selectedLocation ?? "No location";
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Equipment item updated",
            EntityType = "EquipmentItem",
            EntityId = equipmentItem.Id,
            Details = $"Equipment register updated from [{previousSummary}] to [{equipmentItem.Name} / {equipmentItem.EquipmentType ?? "No type"} / {equipmentItem.SerialOrAssetId ?? "No serial"} / {newLocation}].",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Equipment item updated.";
        return Page();
    }

    private async Task LoadOptionsAsync(int companyId)
    {
        LocationOptions = await _locationOptions.GetAssetLocationOptionsAsync(companyId);
    }

    private void LoadFromEquipmentItem(EquipmentItem equipmentItem, string? returnUrl)
    {
        EquipmentItemId = equipmentItem.Id;
        Name = equipmentItem.Name;
        EquipmentType = equipmentItem.EquipmentType;
        Model = equipmentItem.Model;
        SerialOrAssetId = equipmentItem.SerialOrAssetId;
        NextServiceDate = equipmentItem.NextServiceDate;
        BatteryRequired = equipmentItem.BatteryRequired;
        Location = equipmentItem.CurrentOperationalArea?.Name ?? equipmentItem.CurrentLocationDetail;
        Status = equipmentItem.Status;
        Notes = equipmentItem.Notes;
        ReturnUrl = returnUrl;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();
    }
}
