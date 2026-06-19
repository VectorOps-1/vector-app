using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class StockOrdersModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public StockOrdersModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string Stage { get; set; } = "all";

    public IReadOnlyList<StockOrderListItem> Orders { get; private set; } = [];
    public IReadOnlyDictionary<string, int> StageCounts { get; private set; } = new Dictionary<string, int>();
    public string? SuccessMessage { get; private set; }
    public string SelectedStage => NormalizeStage(Stage);
    public string QueueTitle => SelectedStage switch
    {
        "approval" => "Orders waiting for approval",
        "supplier" => "Supplier confirmation queue",
        "register" => "Register entry queue",
        "allocation" => "Allocation queue",
        _ => "All stock orders"
    };

    public string QueueDescription => SelectedStage switch
    {
        "approval" => "Operational manager requests that need senior approval before supplier contact.",
        "supplier" => "Orders ready for supplier email, waiting for supplier response, or recently confirmed.",
        "register" => "Confirmed orders ready to be entered into the stock register.",
        "allocation" => "Registered orders ready to be allocated to a base, store, vehicle, or operational area.",
        _ => "Every order in the order-to-distribution workflow."
    };

    public async Task OnGetAsync()
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            Orders = [];
            return;
        }

        var allOrders = await _db.StockOrders
            .AsNoTracking()
            .Include(order => order.RequestedByUser)
            .Include(order => order.Lines)
            .Where(order => order.CompanyId == currentUser.CompanyId)
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

        Stage = SelectedStage;
        StageCounts = BuildStageCounts(allOrders);

        Orders = SelectedStage == "all"
            ? allOrders
            : allOrders.Where(order => StageIncludesStatus(SelectedStage, order.Status)).ToList();
    }

    public int GetStageCount(string stage)
    {
        return StageCounts.TryGetValue(NormalizeStage(stage), out var count) ? count : 0;
    }

    private static IReadOnlyDictionary<string, int> BuildStageCounts(IReadOnlyList<StockOrderListItem> orders)
    {
        var stages = new[] { "all", "approval", "supplier", "register", "allocation" };
        return stages.ToDictionary(
            stage => stage,
            stage => stage == "all"
                ? orders.Count
                : orders.Count(order => StageIncludesStatus(stage, order.Status)));
    }

    private static bool StageIncludesStatus(string stage, string status)
    {
        return NormalizeStage(stage) switch
        {
            "approval" => status == StockOrderStatuses.PendingSeniorApproval,
            "supplier" => status is StockOrderStatuses.ReadyToEmailSupplier
                or StockOrderStatuses.SentToSupplier
                or StockOrderStatuses.SupplierConfirmed,
            "register" => status is StockOrderStatuses.SupplierConfirmed
                or StockOrderStatuses.RegisterEntryAuthorised
                or StockOrderStatuses.EnteredInRegister,
            "allocation" => status is StockOrderStatuses.EnteredInRegister
                or StockOrderStatuses.Allocated,
            _ => true
        };
    }

    private static string NormalizeStage(string? stage)
    {
        return string.Equals(stage, "approval", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(stage, "supplier", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(stage, "register", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(stage, "allocation", StringComparison.OrdinalIgnoreCase)
            ? stage!.ToLowerInvariant()
            : "all";
    }
}

public record StockOrderListItem(
    int Id,
    string SupplierName,
    string SupplierEmail,
    string Status,
    string RequestedByName,
    DateTime CreatedAtUtc,
    int LineCount,
    int QuantityRequested);
