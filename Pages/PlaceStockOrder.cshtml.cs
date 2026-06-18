using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class PlaceStockOrderModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;

    public PlaceStockOrderModel(VectorDbContext db, CurrentUserService currentUser, LocationOptionService locationOptions)
    {
        _db = db;
        _currentUser = currentUser;
        _locationOptions = locationOptions;
    }

    [BindProperty] public string SupplierName { get; set; } = string.Empty;
    [BindProperty] public string SupplierEmail { get; set; } = string.Empty;
    [BindProperty] public string? DeliveryAddress { get; set; }
    [BindProperty] public string? DeliveryInstructions { get; set; }
    [BindProperty] public string? OrderNotes { get; set; }
    [BindProperty] public List<StockOrderLineInput> Lines { get; set; } = NewLineInputs();
    public List<SelectListItem> DeliveryLocationOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadDeliveryLocationOptionsAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadDeliveryLocationOptionsAsync(currentUser.CompanyId);

        var orderLines = Lines
            .Where(line => !string.IsNullOrWhiteSpace(line.ItemName) && line.QuantityRequested > 0)
            .ToList();

        if (string.IsNullOrWhiteSpace(SupplierName))
        {
            ModelState.AddModelError(nameof(SupplierName), "Enter the supplier name.");
        }

        if (string.IsNullOrWhiteSpace(SupplierEmail))
        {
            ModelState.AddModelError(nameof(SupplierEmail), "Enter the supplier email.");
        }

        if (!orderLines.Any())
        {
            ModelState.AddModelError(string.Empty, "Add at least one item with a quantity.");
        }

        if (!ModelState.IsValid)
        {
            EnsureLineRows();
            return Page();
        }

        var isSenior = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        var now = DateTime.UtcNow;
        var order = new StockOrder
        {
            CompanyId = currentUser.CompanyId,
            RequestedByUserId = currentUser.Id,
            ApprovedBySeniorUserId = isSenior ? currentUser.Id : null,
            SupplierName = SupplierName.Trim(),
            SupplierEmail = SupplierEmail.Trim(),
            DeliveryAddress = LocationOptionService.NormalizeSelectedLocation(DeliveryAddress),
            DeliveryInstructions = DeliveryInstructions,
            OrderNotes = OrderNotes,
            Status = isSenior ? StockOrderStatuses.ReadyToEmailSupplier : StockOrderStatuses.PendingSeniorApproval,
            ApprovedAtUtc = isSenior ? now : null,
            CreatedAtUtc = now
        };

        foreach (var line in orderLines)
        {
            order.Lines.Add(new StockOrderLine
            {
                ItemName = line.ItemName!.Trim(),
                ItemType = line.ItemType,
                QuantityRequested = line.QuantityRequested,
                Notes = line.Notes
            });
        }

        order.EmailSubject = $"Stock order request from {CompanyBranding.GetDisplayCompanyName(currentUser.Company)}";
        order.EmailBody = BuildEmailBody(order, currentUser.FullName);

        _db.StockOrders.Add(order);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = isSenior ? "Stock order created" : "Stock order requested",
            EntityType = "StockOrder",
            EntityId = order.Id,
            Details = isSenior
                ? $"Stock order #{order.Id} created and ready to email supplier {order.SupplierName}."
                : $"Stock order #{order.Id} created and awaiting senior approval.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = isSenior
            ? "Stock order saved. Supplier email is ready to send."
            : "Stock order saved and sent for senior approval.";

        return RedirectToPage("/StockOrderAction", new { orderId = order.Id });
    }

    private void EnsureLineRows()
    {
        while (Lines.Count < 6)
        {
            Lines.Add(new StockOrderLineInput());
        }
    }

    private static List<StockOrderLineInput> NewLineInputs()
    {
        return Enumerable.Range(0, 6).Select(_ => new StockOrderLineInput()).ToList();
    }

    private async Task LoadDeliveryLocationOptionsAsync(int companyId)
    {
        DeliveryLocationOptions = await _locationOptions.GetOperationalAreaOptionsAsync(companyId);
    }

    private static string BuildEmailBody(StockOrder order, string requestedBy)
    {
        var body = new StringBuilder();
        body.AppendLine($"Supplier: {order.SupplierName}");
        body.AppendLine($"Requested by: {requestedBy}");
        body.AppendLine();
        body.AppendLine("Please confirm availability and return confirmed quantity, batch number, and expiry date for each item.");
        body.AppendLine();
        body.AppendLine("Order lines:");

        foreach (var line in order.Lines)
        {
            body.AppendLine($"- {line.ItemName} | Type: {line.ItemType ?? "Not specified"} | Quantity: {line.QuantityRequested}");
        }

        if (!string.IsNullOrWhiteSpace(order.DeliveryAddress))
        {
            body.AppendLine();
            body.AppendLine($"Delivery address: {order.DeliveryAddress}");
        }

        if (!string.IsNullOrWhiteSpace(order.DeliveryInstructions))
        {
            body.AppendLine($"Delivery instructions: {order.DeliveryInstructions}");
        }

        if (!string.IsNullOrWhiteSpace(order.OrderNotes))
        {
            body.AppendLine();
            body.AppendLine($"Notes: {order.OrderNotes}");
        }

        return body.ToString();
    }
}

public class StockOrderLineInput
{
    public string? ItemName { get; set; }
    public string? ItemType { get; set; }
    public int QuantityRequested { get; set; }
    public string? Notes { get; set; }
}
