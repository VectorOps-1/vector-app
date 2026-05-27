using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
        ["/Equipment"] = ManagementAccess,
        ["/Stock"] = ManagementAccess,
        ["/Staff"] = ManagementAccess,
        ["/Medication"] = ManagementAccess,
        ["/SendTask"] = ManagementAccess,
        ["/EditChecklist"] = ManagementAccess,
        ["/AddItem"] = ManagementAccess,
        ["/UploadStaffFiles"] = ManagementAccess,
        ["/StaffRecordsSearch"] = ManagementAccess,

        ["/MasterSetup"] = SeniorAccess,
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

        if (!userId.HasValue || string.IsNullOrWhiteSpace(accessView) || !allowedAccessViews.Contains(accessView))
        {
            context.Result = new RedirectToPageResult("/RoleLogin", new { access = allowedAccessViews[0] });
            return;
        }

        await next();
    }
}
