using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class StockRegisterModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public StockRegisterModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true, Name = "view")] public string? RequestedViewMode { get; set; }
    [BindProperty(SupportsGet = true)] public string GroupBy { get; set; } = "name";

    public List<StockRegisterItem> StockItems { get; private set; } = new();
    public List<StockRegisterGroup> StockGroups { get; private set; } = new();
    public string ViewMode { get; private set; } = "register";
    public bool IsRegisterView { get; private set; }
    public bool HasSearchTerm => !string.IsNullOrWhiteSpace(SearchTerm);
    public bool ShouldShowResults => IsRegisterView || HasSearchTerm;
    public string? StatusMessage { get; private set; }
    public string PageHeading => "Stock Register";
    public string PageSubtitle => "Open the full stock register grouped by item name or current location. Use the search field to narrow results, then expand a group and select a row to view full details.";
    public string CurrentReturnUrl => "/StockRegister?view=register&GroupBy=" + Uri.EscapeDataString(NormalizeGroupBy(GroupBy)) + (HasSearchTerm ? "&SearchTerm=" + Uri.EscapeDataString(SearchTerm!.Trim()) : string.Empty);

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        ViewMode = NormalizeViewMode(RequestedViewMode);
        IsRegisterView = ViewMode == "register";
        GroupBy = NormalizeGroupBy(GroupBy);
        StatusMessage = TempData["SuccessMessage"] as string;

        if (!ShouldShowResults)
        {
            return Page();
        }

        var query = _db.StockItems
            .Include(item => item.LastMovedByUser)
            .Where(item => item.CompanyId == currentUser.CompanyId && item.Status != "Deleted");

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(item =>
                item.ItemName.Contains(search)
                || (item.ItemType != null && item.ItemType.Contains(search))
                || (item.BatchNumber != null && item.BatchNumber.Contains(search))
                || (item.Location != null && item.Location.Contains(search))
                || (item.LastMovementType != null && item.LastMovementType.Contains(search))
                || item.Status.Contains(search)
                || (item.LastMovedByUser != null && item.LastMovedByUser.FullName.Contains(search)));
        }

        StockItems = await query
            .OrderBy(item => item.ItemName)
            .ThenBy(item => item.BatchNumber)
            .ThenBy(item => item.Location)
            .Select(item => new StockRegisterItem
            {
                Id = item.Id,
                ItemName = item.ItemName,
                ItemType = item.ItemType,
                StockCategory = item.StockCategory,
                Quantity = item.Quantity,
                MinimumQuantity = item.MinimumQuantity,
                Unit = item.Unit,
                BatchNumber = item.BatchNumber,
                ExpiryDate = item.ExpiryDate,
                IsReadinessCritical = item.IsReadinessCritical,
                Location = item.Location,
                Status = item.Status,
                LastMovementType = item.LastMovementType,
                LastMovementAtUtc = item.LastMovementAtUtc,
                LastMovedByName = item.LastMovedByUser == null ? null : item.LastMovedByUser.FullName,
                Notes = item.Notes
            })
            .ToListAsync();

        StockGroups = StockItems
            .GroupBy(item => BuildGroupLabel(item))
            .OrderBy(group => group.Key)
            .Select(group => new StockRegisterGroup
            {
                Label = group.Key,
                Items = group
                    .OrderBy(item => item.ItemType)
                    .ThenBy(item => item.BatchNumber)
                    .ThenBy(item => item.Location)
                    .ToList()
            })
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int stockItemId, string? returnUrl)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var stockItem = await _db.StockItems
            .FirstOrDefaultAsync(item =>
                item.Id == stockItemId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (stockItem is null)
        {
            TempData["SuccessMessage"] = "Stock item was not found.";
            return RedirectBack(returnUrl);
        }

        var now = DateTime.UtcNow;
        stockItem.Status = "Deleted";
        stockItem.UpdatedAtUtc = now;

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Stock item deleted",
            EntityType = "StockItem",
            EntityId = stockItem.Id,
            Details = $"Stock item deleted from register: {stockItem.ItemName}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Stock item deleted.";
        return RedirectBack(returnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/StockRegister", new { view = "register" });
    }

    private static string NormalizeViewMode(string? viewMode)
    {
        return "register";
    }

    private string BuildGroupLabel(StockRegisterItem item)
    {
        if (string.Equals(GroupBy, "location", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(item.Location)
                ? "No location assigned"
                : item.Location.Trim();
        }

        return string.IsNullOrWhiteSpace(item.ItemName)
            ? "Unnamed stock item"
            : item.ItemName.Trim();
    }

    private static string NormalizeGroupBy(string? groupBy)
    {
        return string.Equals(groupBy, "location", StringComparison.OrdinalIgnoreCase)
            ? "location"
            : "name";
    }

    public class StockRegisterGroup
    {
        public string Label { get; set; } = string.Empty;
        public List<StockRegisterItem> Items { get; set; } = new();
    }

    public class StockRegisterItem
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ItemType { get; set; }
        public string? StockCategory { get; set; }
        public int Quantity { get; set; }
        public int? MinimumQuantity { get; set; }
        public string? Unit { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsReadinessCritical { get; set; }
        public string? Location { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? LastMovementType { get; set; }
        public DateTime? LastMovementAtUtc { get; set; }
        public string? LastMovedByName { get; set; }
        public string? Notes { get; set; }
    }
}
