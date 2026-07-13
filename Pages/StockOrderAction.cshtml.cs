using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class StockOrderActionModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;
    private readonly IUserActionPermissionService _permissionService;

    public StockOrderActionModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        LocationOptionService locationOptions,
        IUserActionPermissionService permissionService)
    {
        _db = db;
        _currentUser = currentUser;
        _locationOptions = locationOptions;
        _permissionService = permissionService;
    }

    [BindProperty] public int? AuthorisedUserId { get; set; }
    [BindProperty] public List<StockOrderLineUpdateInput> LineUpdates { get; set; } = [];

    public StockOrderDetail? Order { get; private set; }
    public List<SelectListItem> OperationalManagers { get; private set; } = [];
    public bool IsSeniorManager { get; private set; }
    public bool CanApproveStockOrders { get; private set; }
    public bool CanMarkEmailSent { get; private set; }
    public bool CanConfirmSupplier { get; private set; }
    public bool CanAuthoriseRegister { get; private set; }
    public bool CanEnterRegister { get; private set; }
    public bool CanAllocate { get; private set; }
    public string? SuccessMessage { get; private set; }
    public List<SelectListItem> LocationOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        SuccessMessage = TempData["SuccessMessage"] as string;
        return await LoadPageAsync(orderId);
    }

    public async Task<IActionResult> OnPostApproveAsync(int orderId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!await _permissionService.HasPermissionAsync(currentUser, UserActionPermissions.StockOrdersApprove))
        {
            TempData["SuccessMessage"] = "You do not have permission to approve stock orders.";
            return RedirectToPage(new { orderId });
        }

        var order = await LoadOrderEntityAsync(orderId, currentUser.CompanyId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != StockOrderStatuses.PendingSeniorApproval)
        {
            return RejectInvalidTransition(orderId, "This stock order is not awaiting approval.");
        }

        order.Status = StockOrderStatuses.ReadyToEmailSupplier;
        order.ApprovedBySeniorUserId = currentUser.Id;
        order.ApprovedAtUtc = DateTime.UtcNow;
        await AddAuditAsync(currentUser, order.Id, "Stock order approved", $"Stock order #{order.Id} approved for supplier email.");
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Stock order approved. Supplier email is ready.";
        return RedirectToPage(new { orderId });
    }

    public async Task<IActionResult> OnPostMarkEmailSentAsync(int orderId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var order = await LoadOrderEntityAsync(orderId, currentUser.CompanyId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != StockOrderStatuses.ReadyToEmailSupplier)
        {
            return RejectInvalidTransition(orderId, "Approve the stock order before marking the supplier email as sent.");
        }

        order.Status = StockOrderStatuses.SentToSupplier;
        order.EmailSentAtUtc = DateTime.UtcNow;
        await AddAuditAsync(currentUser, order.Id, "Stock order emailed", $"Supplier email marked sent for stock order #{order.Id}.");
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Supplier email marked as sent.";
        return RedirectToPage(new { orderId });
    }

    public async Task<IActionResult> OnPostConfirmSupplierAsync(int orderId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var order = await LoadOrderEntityAsync(orderId, currentUser.CompanyId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status is not (StockOrderStatuses.SentToSupplier or StockOrderStatuses.SupplierConfirmed))
        {
            return RejectInvalidTransition(orderId, "Mark the supplier email as sent before recording confirmation.");
        }

        foreach (var update in LineUpdates)
        {
            var line = order.Lines.FirstOrDefault(line => line.Id == update.LineId);
            if (line is null)
            {
                continue;
            }

            line.QuantityConfirmed = update.QuantityConfirmed;
            line.BatchNumber = update.BatchNumber;
            line.ExpiryDate = update.ExpiryDate;
        }

        order.Status = StockOrderStatuses.SupplierConfirmed;
        order.SupplierConfirmedAtUtc = DateTime.UtcNow;
        await AddAuditAsync(currentUser, order.Id, "Supplier confirmation recorded", $"Supplier confirmation recorded for stock order #{order.Id}.");
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Supplier confirmation saved.";
        return RedirectToPage(new { orderId });
    }

    public async Task<IActionResult> OnPostAuthoriseRegisterAsync(int orderId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            TempData["SuccessMessage"] = "Only senior management can authorise register entry.";
            return RedirectToPage(new { orderId });
        }

        var order = await LoadOrderEntityAsync(orderId, currentUser.CompanyId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status is not (StockOrderStatuses.SupplierConfirmed or StockOrderStatuses.RegisterEntryAuthorised))
        {
            return RejectInvalidTransition(orderId, "Record supplier confirmation before authorising register entry.");
        }

        order.RegisterEntryAuthorisedUserId = AuthorisedUserId;
        order.Status = StockOrderStatuses.RegisterEntryAuthorised;
        await AddAuditAsync(currentUser, order.Id, "Stock register entry authorised", $"Stock order #{order.Id} authorised for register entry.");
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Register entry authorisation saved.";
        return RedirectToPage(new { orderId });
    }

    public async Task<IActionResult> OnPostEnterRegisterAsync(int orderId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var order = await LoadOrderEntityAsync(orderId, currentUser.CompanyId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status is not (StockOrderStatuses.SupplierConfirmed or StockOrderStatuses.RegisterEntryAuthorised))
        {
            return RejectInvalidTransition(orderId, "Record supplier confirmation before entering stock into the register.");
        }

        var isSenior = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        if (!isSenior && order.RegisterEntryAuthorisedUserId != currentUser.Id)
        {
            TempData["SuccessMessage"] = "Senior management must authorise you before entering this stock into the register.";
            return RedirectToPage(new { orderId });
        }

        foreach (var update in LineUpdates)
        {
            var line = order.Lines.FirstOrDefault(line => line.Id == update.LineId);
            if (line is not null)
            {
                line.RegisterLocation = update.RegisterLocation;
                await UpsertStockItemAsync(
                    order.CompanyId,
                    currentUser.Id,
                    line.ItemName,
                    line.ItemType,
                    line.BatchNumber,
                    line.QuantityConfirmed ?? line.QuantityRequested,
                    line.RegisterLocation,
                    "Register entry",
                    line.Notes);
            }
        }

        order.Status = StockOrderStatuses.EnteredInRegister;
        order.RegisterEnteredAtUtc = DateTime.UtcNow;
        await AddAuditAsync(currentUser, order.Id, "Stock entered into register", $"Stock order #{order.Id} entered into register.");
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Stock register entry saved.";
        return RedirectToPage(new { orderId });
    }

    public async Task<IActionResult> OnPostAllocateAsync(int orderId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var order = await LoadOrderEntityAsync(orderId, currentUser.CompanyId);
        if (order is null)
        {
            return NotFound();
        }

        if (order.Status is not (StockOrderStatuses.EnteredInRegister or StockOrderStatuses.Allocated))
        {
            return RejectInvalidTransition(orderId, "Enter the received stock into the register before allocating it.");
        }

        foreach (var update in LineUpdates)
        {
            var line = order.Lines.FirstOrDefault(line => line.Id == update.LineId);
            if (line is not null)
            {
                line.QuantityAllocated = update.QuantityAllocated;
                line.AllocationLocation = update.AllocationLocation;
                await UpsertStockItemAsync(
                    order.CompanyId,
                    currentUser.Id,
                    line.ItemName,
                    line.ItemType,
                    line.BatchNumber,
                    line.QuantityAllocated ?? line.QuantityConfirmed ?? line.QuantityRequested,
                    line.AllocationLocation ?? line.RegisterLocation,
                    "Allocated",
                    line.Notes);
            }
        }

        order.Status = StockOrderStatuses.Allocated;
        order.AllocatedAtUtc = DateTime.UtcNow;
        await AddAuditAsync(currentUser, order.Id, "Stock allocated", $"Stock order #{order.Id} allocated by batch and location.");
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Stock allocation saved.";
        return RedirectToPage(new { orderId });
    }

    private async Task<IActionResult> LoadPageAsync(int orderId)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        IsSeniorManager = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        var hasApprovalPermission = await _permissionService.HasPermissionAsync(currentUser, UserActionPermissions.StockOrdersApprove);

        var order = await _db.StockOrders
            .AsNoTracking()
            .Include(item => item.RequestedByUser)
            .Include(item => item.ApprovedBySeniorUser)
            .Include(item => item.RegisterEntryAuthorisedUser)
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == orderId && item.CompanyId == currentUser.CompanyId);

        if (order is null)
        {
            return NotFound();
        }

        CanApproveStockOrders = hasApprovalPermission && order.Status == StockOrderStatuses.PendingSeniorApproval;
        CanMarkEmailSent = order.Status == StockOrderStatuses.ReadyToEmailSupplier;
        CanConfirmSupplier = order.Status is StockOrderStatuses.SentToSupplier or StockOrderStatuses.SupplierConfirmed;
        CanAuthoriseRegister = IsSeniorManager && order.Status is StockOrderStatuses.SupplierConfirmed or StockOrderStatuses.RegisterEntryAuthorised;
        CanEnterRegister = (IsSeniorManager || order.RegisterEntryAuthorisedUserId == currentUser.Id)
            && order.Status is StockOrderStatuses.SupplierConfirmed or StockOrderStatuses.RegisterEntryAuthorised;
        CanAllocate = order.Status is StockOrderStatuses.EnteredInRegister or StockOrderStatuses.Allocated;
        AuthorisedUserId = order.RegisterEntryAuthorisedUserId;
        LocationOptions = await _locationOptions.GetAssetLocationOptionsAsync(currentUser.CompanyId);
        OperationalManagers = await _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Where(user =>
                user.CompanyId == currentUser.CompanyId
                && user.Status == "Active"
                && user.AppRole != null
                && user.AppRole.Name == "Operational Management")
            .OrderBy(user => user.FullName)
            .Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = user.FullName
            })
            .ToListAsync();

        var lines = order.Lines
            .OrderBy(line => line.Id)
            .Select(line => new StockOrderLineDetail(
                line.Id,
                line.ItemName,
                line.ItemType,
                line.QuantityRequested,
                line.QuantityConfirmed,
                line.BatchNumber,
                line.ExpiryDate,
                line.RegisterLocation,
                line.QuantityAllocated,
                line.AllocationLocation,
                line.Notes))
            .ToList();

        LineUpdates = lines
            .Select(line => new StockOrderLineUpdateInput
            {
                LineId = line.Id,
                QuantityConfirmed = line.QuantityConfirmed ?? line.QuantityRequested,
                BatchNumber = line.BatchNumber,
                ExpiryDate = line.ExpiryDate,
                RegisterLocation = line.RegisterLocation,
                QuantityAllocated = line.QuantityAllocated ?? line.QuantityConfirmed ?? line.QuantityRequested,
                AllocationLocation = line.AllocationLocation
            })
            .ToList();

        Order = new StockOrderDetail(
            order.Id,
            order.SupplierName,
            order.SupplierEmail,
            order.DeliveryAddress,
            order.DeliveryInstructions,
            order.OrderNotes,
            order.Status,
            order.EmailSubject,
            order.EmailBody,
            order.RequestedByUser?.FullName ?? "Unknown",
            order.ApprovedBySeniorUser?.FullName,
            order.RegisterEntryAuthorisedUser?.FullName,
            order.CreatedAtUtc,
            order.ApprovedAtUtc,
            order.EmailSentAtUtc,
            order.SupplierConfirmedAtUtc,
            order.RegisterEnteredAtUtc,
            order.AllocatedAtUtc,
            lines);

        return Page();
    }

    private IActionResult RejectInvalidTransition(int orderId, string message)
    {
        TempData["SuccessMessage"] = message;
        return RedirectToPage(new { orderId });
    }

    private async Task<StockOrder?> LoadOrderEntityAsync(int orderId, int companyId)
    {
        return await _db.StockOrders
            .Include(order => order.Lines)
            .FirstOrDefaultAsync(order => order.Id == orderId && order.CompanyId == companyId);
    }

    private async Task AddAuditAsync(AppUser user, int orderId, string action, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = user.CompanyId,
            AppUserId = user.Id,
            Action = action,
            EntityType = "StockOrder",
            EntityId = orderId,
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        });

        await Task.CompletedTask;
    }

    private async Task UpsertStockItemAsync(
        int companyId,
        int userId,
        string itemName,
        string? itemType,
        string? batchNumber,
        int quantity,
        string? location,
        string movementType,
        string? notes)
    {
        location = LocationOptionService.NormalizeSelectedLocation(location);
        var area = await _locationOptions.FindOperationalAreaAsync(companyId, location);
        var stockItem = await _db.StockItems.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId
            && item.ItemName == itemName
            && item.BatchNumber == batchNumber
            && item.Location == location);

        var now = DateTime.UtcNow;
        if (stockItem is null)
        {
            stockItem = new StockItem
            {
                CompanyId = companyId,
                CreatedByUserId = userId,
                ItemName = itemName,
                ItemType = itemType,
                BatchNumber = batchNumber,
                Location = location,
                CurrentOperationalAreaId = area?.Id,
                Quantity = quantity,
                Status = "Active",
                Notes = notes,
                CreatedAtUtc = now
            };
            _db.StockItems.Add(stockItem);
        }
        else
        {
            stockItem.Quantity = quantity;
            stockItem.ItemType = itemType;
            stockItem.CurrentOperationalAreaId = area?.Id;
            stockItem.Notes = notes;
            stockItem.UpdatedAtUtc = now;
        }

        stockItem.LastMovedByUserId = userId;
        stockItem.LastMovementType = movementType;
        stockItem.LastMovementAtUtc = now;
    }
}

public record StockOrderDetail(
    int Id,
    string SupplierName,
    string SupplierEmail,
    string? DeliveryAddress,
    string? DeliveryInstructions,
    string? OrderNotes,
    string Status,
    string EmailSubject,
    string EmailBody,
    string RequestedByName,
    string? ApprovedByName,
    string? RegisterEntryAuthorisedName,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? EmailSentAtUtc,
    DateTime? SupplierConfirmedAtUtc,
    DateTime? RegisterEnteredAtUtc,
    DateTime? AllocatedAtUtc,
    IReadOnlyList<StockOrderLineDetail> Lines);

public record StockOrderLineDetail(
    int Id,
    string ItemName,
    string? ItemType,
    int QuantityRequested,
    int? QuantityConfirmed,
    string? BatchNumber,
    DateTime? ExpiryDate,
    string? RegisterLocation,
    int? QuantityAllocated,
    string? AllocationLocation,
    string? Notes);

public class StockOrderLineUpdateInput
{
    public int LineId { get; set; }
    public int? QuantityConfirmed { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? RegisterLocation { get; set; }
    public int? QuantityAllocated { get; set; }
    public string? AllocationLocation { get; set; }
}
