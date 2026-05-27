using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
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

    public List<StockRegisterItem> StockItems { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var query = _db.StockItems
            .Include(item => item.LastMovedByUser)
            .Where(item => item.CompanyId == currentUser.CompanyId);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(item =>
                item.ItemName.Contains(search)
                || (item.ItemType != null && item.ItemType.Contains(search))
                || (item.BatchNumber != null && item.BatchNumber.Contains(search))
                || (item.Location != null && item.Location.Contains(search))
                || (item.LastMovementType != null && item.LastMovementType.Contains(search))
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
                Quantity = item.Quantity,
                BatchNumber = item.BatchNumber,
                Location = item.Location,
                Status = item.Status,
                LastMovementType = item.LastMovementType,
                LastMovementAtUtc = item.LastMovementAtUtc,
                LastMovedByName = item.LastMovedByUser == null ? null : item.LastMovedByUser.FullName
            })
            .ToListAsync();

        return Page();
    }

    public class StockRegisterItem
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ItemType { get; set; }
        public int Quantity { get; set; }
        public string? BatchNumber { get; set; }
        public string? Location { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? LastMovementType { get; set; }
        public DateTime? LastMovementAtUtc { get; set; }
        public string? LastMovedByName { get; set; }
    }
}
