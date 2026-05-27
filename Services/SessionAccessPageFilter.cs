using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;

namespace vector_app_local.Services;

public class SessionAccessPageFilter : IAsyncPageFilter
{
    private static readonly string[] AllSignedInAccess =
    {
        CurrentUserService.StaffAccess,
        CurrentUserService.OperationalManagementAccess,
        CurrentUserService.SeniorManagementAccess
    };

    private static readonly string[] ManagementAccess =
    {
        CurrentUserService.OperationalManagementAccess,
        CurrentUserService.SeniorManagementAccess
    };

    private static readonly string[] SeniorAccess =
    {
        CurrentUserService.SeniorManagementAccess
    };

    private static readonly Dictionary<string, string[]> AccessRules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/TaskInbox"] = AllSignedInAccess,
        ["/TaskAction"] = AllSignedInAccess,
        ["/TaskFeedback"] = AllSignedInAccess,
        ["/CompleteChecklist"] = AllSignedInAccess,
        ["/DailyChecklist"] = AllSignedInAccess,
        ["/DailyVehicleChecklist"] = AllSignedInAccess,
        ["/DailyEquipmentChecklist"] = AllSignedInAccess,
        ["/MonthlyChecklist"] = AllSignedInAccess,
        ["/MonthlyVehicleChecklist"] = AllSignedInAccess,
        ["/MonthlyEquipmentChecklist"] = AllSignedInAccess,
        ["/PersonalDocuments"] = AllSignedInAccess,
        ["/ReportIssue"] = AllSignedInAccess,

        ["/Vehicles"] = ManagementAccess,
        ["/VehicleRegister"] = ManagementAccess,
        ["/Equipment"] = ManagementAccess,
        ["/EquipmentRegister"] = ManagementAccess,
        ["/MoveAsset"] = ManagementAccess,
        ["/Stock"] = ManagementAccess,
        ["/StockRegister"] = ManagementAccess,
        ["/StockOrders"] = ManagementAccess,
        ["/PlaceStockOrder"] = ManagementAccess,
        ["/StockOrderAction"] = ManagementAccess,
        ["/SupplierConfirmations"] = ManagementAccess,
        ["/EnterStockRegister"] = ManagementAccess,
        ["/AllocateStock"] = ManagementAccess,
        ["/Staff"] = ManagementAccess,
        ["/StaffRegister"] = ManagementAccess,
        ["/StaffFiles"] = ManagementAccess,
        ["/Medication"] = ManagementAccess,
        ["/MedicationRegister"] = ManagementAccess,
        ["/SendTask"] = ManagementAccess,
        ["/IssueInbox"] = ManagementAccess,
        ["/IssueReports"] = ManagementAccess,
        ["/IssueReportAction"] = ManagementAccess,
        ["/EditChecklist"] = ManagementAccess,
        ["/EditVehicleChecklist"] = ManagementAccess,
        ["/EditEquipmentChecklist"] = ManagementAccess,
        ["/AddItem"] = ManagementAccess,
        ["/UploadStaffFiles"] = ManagementAccess,
        ["/StaffRecordsSearch"] = ManagementAccess,

        ["/MasterSetup"] = SeniorAccess,
        ["/OperationalAreas"] = SeniorAccess,
        ["/CompanyProfile"] = SeniorAccess,
        ["/CompanyName"] = SeniorAccess,
        ["/LogoUpload"] = SeniorAccess,
        ["/SupplierDetails"] = SeniorAccess,
        ["/UploadChecklist"] = SeniorAccess,
        ["/UploadVehicleRegister"] = SeniorAccess,
        ["/VehicleSchematicLibrary"] = SeniorAccess,
        ["/UploadEquipmentRegister"] = SeniorAccess,
        ["/UploadStockRegister"] = SeniorAccess,
        ["/CreateManagerAccess"] = SeniorAccess,
        ["/CreateOperationalStaffAccess"] = SeniorAccess,
        ["/TaskCommunicationSetup"] = SeniorAccess,
        ["/AuditLog"] = SeniorAccess,
        ["/Onboarding"] = SeniorAccess,
        ["/CompanyLogin"] = SeniorAccess
    };

    private static readonly HashSet<string> TaskAccessibleManagementPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/AddItem",
        "/Stock",
        "/MoveAsset"
    };

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var pagePath = context.ActionDescriptor.ViewEnginePath;
        if (!AccessRules.TryGetValue(pagePath, out var allowedAccessViews))
        {
            await next();
            return;
        }

        var session = context.HttpContext.Session;
        var userId = session.GetInt32(CurrentUserService.UserIdSessionKey);
        var accessView = session.GetString(CurrentUserService.AccessViewSessionKey);

        if (!userId.HasValue || string.IsNullOrWhiteSpace(accessView))
        {
            context.Result = new RedirectToPageResult("/RoleLogin", new { access = allowedAccessViews[0] });
            return;
        }

        if (allowedAccessViews.Contains(accessView))
        {
            await next();
            return;
        }

        if (string.Equals(accessView, CurrentUserService.StaffAccess, StringComparison.OrdinalIgnoreCase)
            && await HasValidTaskAccessAsync(context, pagePath, userId.Value))
        {
            await next();
            return;
        }

        context.Result = new RedirectToPageResult("/RoleLogin", new { access = allowedAccessViews[0] });
    }

    private static async Task<bool> HasValidTaskAccessAsync(PageHandlerExecutingContext context, string pagePath, int currentUserId)
    {
        if (!TaskAccessibleManagementPages.Contains(pagePath))
        {
            return false;
        }

        var request = context.HttpContext.Request;
        if (!QueryEquals(request.Query, "taskAccess", "true"))
        {
            return false;
        }

        if (!int.TryParse(request.Query["taskId"], out var taskId))
        {
            return false;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<VectorDbContext>();
        var task = await db.TaskItems
            .AsNoTracking()
            .Where(taskItem =>
                taskItem.Id == taskId
                && taskItem.AssignedToUserId == currentUserId
                && taskItem.Status == "Open")
            .Select(taskItem => new
            {
                taskItem.ActionType,
                taskItem.ExpiresAtUtc
            })
            .FirstOrDefaultAsync();

        if (task is null)
        {
            return false;
        }

        if (task.ExpiresAtUtc.HasValue && task.ExpiresAtUtc.Value < DateTime.UtcNow)
        {
            return false;
        }

        return TaskAllowsPage(task.ActionType, pagePath, request.Query);
    }

    private static bool TaskAllowsPage(string actionType, string pagePath, IQueryCollection query)
    {
        return actionType switch
        {
            "Add New Vehicle" => IsAddItemRequest(pagePath, query, "vehicle"),
            "Add New Equipment" => IsAddItemRequest(pagePath, query, "equipment"),
            "Add New Stock Item" => IsAddItemRequest(pagePath, query, "stock"),
            "Add Medication" => IsAddItemRequest(pagePath, query, "medication"),
            "Move / Reallocate Vehicle" => IsMoveAssetRequest(pagePath, query, "vehicle"),
            "Move / Reallocate Equipment" => IsMoveAssetRequest(pagePath, query, "equipment"),
            "Move / Reallocate Stock" => IsMoveAssetRequest(pagePath, query, "stock"),
            "Move / Reallocate Medication" => IsMoveAssetRequest(pagePath, query, "medication"),
            "Receive Stock" => IsStockRequest(pagePath),
            "Issue / Allocate Stock" => IsStockRequest(pagePath),
            "Batch Number Tracking" => IsStockRequest(pagePath),
            "Expiry / Compliance Check" => IsStockRequest(pagePath),
            _ => false
        };
    }

    private static bool IsAddItemRequest(string pagePath, IQueryCollection query, string itemType)
    {
        return string.Equals(pagePath, "/AddItem", StringComparison.OrdinalIgnoreCase)
            && QueryEquals(query, "type", itemType);
    }

    private static bool IsStockRequest(string pagePath)
    {
        return string.Equals(pagePath, "/Stock", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMoveAssetRequest(string pagePath, IQueryCollection query, string assetType)
    {
        return string.Equals(pagePath, "/MoveAsset", StringComparison.OrdinalIgnoreCase)
            && QueryEquals(query, "asset", assetType);
    }

    private static bool QueryEquals(IQueryCollection query, string key, string expectedValue)
    {
        return string.Equals(query[key].ToString(), expectedValue, StringComparison.OrdinalIgnoreCase);
    }
}
