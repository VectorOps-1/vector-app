using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EnterStockRegisterModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public EnterStockRegisterModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public IReadOnlyList<StockOrderListItem> Orders { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            Orders = [];
            return;
        }

        var isSenior = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        var statuses = new[]
        {
            StockOrderStatuses.SupplierConfirmed,
            StockOrderStatuses.RegisterEntryAuthorised,
            StockOrderStatuses.EnteredInRegister
        };

        Orders = await _db.StockOrders
            .AsNoTracking()
            .Include(order => order.RequestedByUser)
            .Include(order => order.Lines)
            .Where(order =>
                order.CompanyId == currentUser.CompanyId
                && statuses.Contains(order.Status)
                && (isSenior || order.RegisterEntryAuthorisedUserId == currentUser.Id))
            .OrderByDescending(order => order.CreatedAtUtc)
            .Select(order => new StockOrderListItem(
                order.Id,
                order.SupplierName,
                order.SupplierEmail,
                order.Status,
                order.RequestedByUser == null ? "Unknown" : order.RequestedByUser.FullName,
                order.CreatedAtUtc,
                order.Lines.Count,
                order.Lines.Sum(line => line.QuantityRequested)))
            .ToListAsync();
    }
}
