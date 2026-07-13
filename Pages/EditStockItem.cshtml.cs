using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditStockItemModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;
    private readonly IUserActionAuthorizationService _authorization;

    public EditStockItemModel(
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

    [BindProperty] public int StockItemId { get; set; }
    [BindProperty] public string ItemName { get; set; } = string.Empty;
    [BindProperty] public string? ItemType { get; set; }
    [BindProperty] public string? StockCategory { get; set; }
    [BindProperty] public string? BatchNumber { get; set; }
    [BindProperty] public int Quantity { get; set; }
    [BindProperty] public int? MinimumQuantity { get; set; }
    [BindProperty] public string? Unit { get; set; }
    [BindProperty] public DateTime? ExpiryDate { get; set; }
    [BindProperty] public bool IsReadinessCritical { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string Status { get; set; } = "Active";
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public string? ReturnUrl { get; set; }

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public List<SelectListItem> LocationOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int stockItemId, string? returnUrl)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var stockItem = await _db.StockItems
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == stockItemId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (stockItem is null)
        {
            return RedirectToPage("/StockRegister", new { view = "register" });
        }

        if (!await _authorization.CanManageAreaScopedRecordAsync(
                currentUser,
                UserActionPermissions.RegistersStockEdit,
                stockItem.CurrentOperationalAreaId))
        {
            return RedirectToPage("/StockRegister", new { view = "register", permissionDenied = "true" });
        }

        LoadFromStockItem(stockItem, returnUrl);
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

        var stockItem = await _db.StockItems
            .FirstOrDefaultAsync(item =>
                item.Id == StockItemId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (stockItem is null)
        {
            StatusMessage = "Stock item was not found.";
            return Page();
        }

        if (!await _authorization.CanManageAreaScopedRecordAsync(
                currentUser,
                UserActionPermissions.RegistersStockEdit,
                stockItem.CurrentOperationalAreaId))
        {
            StatusMessage = "You do not have permission to edit this stock item.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(ItemName))
        {
            StatusMessage = "Enter the stock item name before saving.";
            return Page();
        }

        var now = DateTime.UtcNow;
        var previousSummary = $"{stockItem.ItemName} / {stockItem.ItemType ?? "No type"} / {stockItem.BatchNumber ?? "No batch"} / {stockItem.Location ?? "No location"}";
        var area = await _locationOptions.FindOperationalAreaAsync(currentUser.CompanyId, Location);
        var selectedLocation = LocationOptionService.NormalizeSelectedLocation(Location);

        stockItem.ItemName = ItemName.Trim();
        stockItem.ItemType = NormalizeOptional(ItemType);
        stockItem.StockCategory = NormalizeOptional(StockCategory);
        stockItem.BatchNumber = NormalizeOptional(BatchNumber);
        stockItem.Quantity = Quantity;
        stockItem.MinimumQuantity = MinimumQuantity;
        stockItem.Unit = NormalizeOptional(Unit);
        stockItem.ExpiryDate = ExpiryDate;
        stockItem.IsReadinessCritical = IsReadinessCritical;
        stockItem.Location = selectedLocation;
        stockItem.CurrentOperationalAreaId = area?.Id;
        stockItem.Status = NormalizeOptional(Status) ?? "Active";
        stockItem.Notes = NormalizeOptional(Notes);
        stockItem.UpdatedAtUtc = now;

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Stock item updated",
            EntityType = "StockItem",
            EntityId = stockItem.Id,
            Details = $"Stock register updated from [{previousSummary}] to [{stockItem.ItemName} / {stockItem.ItemType ?? "No type"} / {stockItem.BatchNumber ?? "No batch"} / {stockItem.Location ?? "No location"}].",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Stock item updated.";
        return Page();
    }

    private async Task LoadOptionsAsync(int companyId)
    {
        LocationOptions = await _locationOptions.GetAssetLocationOptionsAsync(companyId);
    }

    private void LoadFromStockItem(StockItem stockItem, string? returnUrl)
    {
        StockItemId = stockItem.Id;
        ItemName = stockItem.ItemName;
        ItemType = stockItem.ItemType;
        StockCategory = stockItem.StockCategory;
        BatchNumber = stockItem.BatchNumber;
        Quantity = stockItem.Quantity;
        MinimumQuantity = stockItem.MinimumQuantity;
        Unit = stockItem.Unit;
        ExpiryDate = stockItem.ExpiryDate;
        IsReadinessCritical = stockItem.IsReadinessCritical;
        Location = stockItem.Location;
        Status = stockItem.Status;
        Notes = stockItem.Notes;
        ReturnUrl = returnUrl;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();
    }
}
