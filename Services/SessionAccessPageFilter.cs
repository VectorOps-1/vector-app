using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

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
        ["/Home"] = AllSignedInAccess,
        ["/TaskInbox"] = AllSignedInAccess,
        ["/TaskAction"] = AllSignedInAccess,
        ["/TaskFeedback"] = AllSignedInAccess,
        ["/CompleteChecklist"] = AllSignedInAccess,
        ["/DailyVehicleChecklist"] = AllSignedInAccess,
        ["/FullAudit"] = AllSignedInAccess,
        ["/FullAuditVehicleChecklist"] = AllSignedInAccess,
        ["/FullAuditEquipmentChecklist"] = AllSignedInAccess,
        ["/MyProfile"] = AllSignedInAccess,
        ["/PersonalDocuments"] = AllSignedInAccess,
        ["/StaffRecordsSearch"] = AllSignedInAccess,
        ["/EditStaffProfile"] = AllSignedInAccess,
        ["/ReportIssue"] = AllSignedInAccess,
        ["/ExpiryNotifications"] = AllSignedInAccess,
        ["/SetupWizard"] = AllSignedInAccess,
        ["/ChangePassword"] = AllSignedInAccess,

        ["/Vehicles"] = ManagementAccess,
        ["/VehicleRegister"] = ManagementAccess,
        ["/EditVehicle"] = ManagementAccess,
        ["/ReassignVehicleCallsign"] = ManagementAccess,
        ["/Readiness"] = ManagementAccess,
        ["/ReadinessDashboard"] = ManagementAccess,
        ["/ReadinessEngine"] = ManagementAccess,
        ["/ReadinessMetric"] = ManagementAccess,
        ["/ReadinessMetricDetail"] = ManagementAccess,
        ["/ReadinessAlerts"] = ManagementAccess,
        ["/OperationsReports"] = ManagementAccess,
        ["/ChecklistReports"] = ManagementAccess,
        ["/ChecklistReportDetail"] = ManagementAccess,
        ["/ChecklistTemplateView"] = ManagementAccess,
        ["/Equipment"] = ManagementAccess,
        ["/EquipmentRegister"] = ManagementAccess,
        ["/EditEquipmentItem"] = ManagementAccess,
        ["/EquipmentService"] = ManagementAccess,
        ["/MoveAsset"] = ManagementAccess,
        ["/Stock"] = ManagementAccess,
        ["/StockRegister"] = ManagementAccess,
        ["/EditStockItem"] = ManagementAccess,
        ["/StockOrders"] = ManagementAccess,
        ["/PlaceStockOrder"] = ManagementAccess,
        ["/StockOrderAction"] = ManagementAccess,
        ["/Staff"] = ManagementAccess,
        ["/StaffRegister"] = ManagementAccess,
        ["/StaffFiles"] = ManagementAccess,
        ["/Medication"] = ManagementAccess,
        ["/MedicationRegister"] = ManagementAccess,
        ["/EditMedicationItem"] = ManagementAccess,
        ["/SendTask"] = ManagementAccess,
        ["/IssueInbox"] = ManagementAccess,
        ["/TasksIssues"] = ManagementAccess,
        ["/ChecklistVarianceAlerts"] = ManagementAccess,
        ["/ReadinessAlerts"] = ManagementAccess,
        ["/IssueReports"] = ManagementAccess,
        ["/IssueReportAction"] = ManagementAccess,
        ["/EditChecklist"] = ManagementAccess,
        ["/EditVehicleChecklist"] = ManagementAccess,
        ["/AddItem"] = ManagementAccess,
        ["/UploadStaffFiles"] = ManagementAccess,
        ["/UploadChecklist"] = ManagementAccess,
        ["/ChecklistPreview"] = ManagementAccess,
        ["/UploadVehicleRegister"] = ManagementAccess,
        ["/VehicleRegisterPreview"] = ManagementAccess,
        ["/UploadEquipmentRegister"] = ManagementAccess,
        ["/EquipmentRegisterPreview"] = ManagementAccess,
        ["/UploadStaffRegister"] = ManagementAccess,
        ["/StaffRegisterPreview"] = ManagementAccess,
        ["/UploadStockRegister"] = ManagementAccess,
        ["/StockRegisterPreview"] = ManagementAccess,
        ["/UploadMedicationRegister"] = ManagementAccess,
        ["/MedicationRegisterPreview"] = ManagementAccess,
        ["/ImportBatch"] = ManagementAccess,

        ["/MasterSetup"] = SeniorAccess,
        ["/AreaManagerControl"] = SeniorAccess,
        ["/ChecklistApproval"] = SeniorAccess,
        ["/OperationalAreas"] = SeniorAccess,
        ["/CompanyProfile"] = SeniorAccess,
        ["/CompanyName"] = SeniorAccess,
        ["/LogoUpload"] = SeniorAccess,
        ["/OperationalStructureSetup"] = SeniorAccess,
        ["/VehicleStructureSetup"] = SeniorAccess,
        ["/StaffStructureSetup"] = SeniorAccess,
        ["/AccessModelSetup"] = SeniorAccess,
        ["/AssetRegisterSetup"] = SeniorAccess,
        ["/ChecklistSetup"] = SeniorAccess,
        ["/ReadinessEngineSetup"] = SeniorAccess,
        ["/SupplierDetails"] = SeniorAccess,
        ["/VehicleSchematicLibrary"] = SeniorAccess,
        ["/CreateManagerAccess"] = SeniorAccess,
        ["/CreateOperationalStaffAccess"] = SeniorAccess,
        ["/TaskCommunicationSetup"] = SeniorAccess,
        ["/AuditLog"] = SeniorAccess,
        ["/Onboarding"] = SeniorAccess
    };

    private static readonly HashSet<string> CompanySetupAllowedPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/SetupWizard",
        "/CompanyProfile",
        "/CompanyName",
        "/LogoUpload",
        "/OperationalStructureSetup",
        "/VehicleStructureSetup",
        "/StaffStructureSetup",
        "/AccessModelSetup",
        "/AssetRegisterSetup",
        "/ChecklistSetup",
        "/ReadinessEngineSetup",
        "/ChangePassword"
    };

    private static readonly HashSet<string> TaskAccessibleManagementPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/AddItem",
        "/EquipmentService",
        "/MoveAsset",
        "/StockOrders",
        "/StockOrderAction",
        "/StockRegister"
    };

    private static readonly HashSet<string> GuidedImportPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/UploadChecklist",
        "/ChecklistPreview",
        "/UploadVehicleRegister",
        "/VehicleRegisterPreview",
        "/UploadEquipmentRegister",
        "/EquipmentRegisterPreview",
        "/UploadStaffRegister",
        "/StaffRegisterPreview",
        "/UploadStockRegister",
        "/StockRegisterPreview",
        "/UploadMedicationRegister",
        "/MedicationRegisterPreview",
        "/ImportBatch"
    };

    private sealed record PermissionRequirement(
        IReadOnlyCollection<string> AllOf,
        IReadOnlyCollection<string> AnyOf)
    {
        public static PermissionRequirement All(params string[] keys) => new(keys, Array.Empty<string>());
        public static PermissionRequirement Any(params string[] keys) => new(Array.Empty<string>(), keys);
    }

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
        var companyId = session.GetInt32(CurrentUserService.CompanyIdSessionKey);
        var accessView = session.GetString(CurrentUserService.AccessViewSessionKey);

        if (context.HttpContext.User.Identity?.IsAuthenticated != true ||
            !userId.HasValue ||
            string.IsNullOrWhiteSpace(accessView))
        {
            context.Result = new RedirectToPageResult("/RoleLogin", new { access = allowedAccessViews[0] });
            return;
        }

        if (!companyId.HasValue)
        {
            ClearCurrentUserSession(session);
            context.Result = new RedirectToPageResult("/CompanyLogin");
            return;
        }

        var currentUserService = context.HttpContext.RequestServices.GetRequiredService<CurrentUserService>();
        var currentUser = await currentUserService.GetCurrentUserAsync();
        var roleName = currentUser?.AppRole?.Name;
        if (currentUser is null || roleName is null || !CurrentUserService.AccessAllowsRole(accessView, roleName))
        {
            ClearCurrentUserSession(session);
            context.Result = new RedirectToPageResult("/RoleLogin", new { access = allowedAccessViews[0] });
            return;
        }

        if (currentUser.LoginIdentity?.MustChangePassword == true &&
            !string.Equals(pagePath, "/ChangePassword", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToPageResult("/ChangePassword");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<VectorDbContext>();

        if (await ShouldRedirectToSetupWizardAsync(db, companyId.Value, pagePath))
        {
            context.Result = new RedirectToPageResult("/SetupWizard");
            return;
        }

        if (GuidedImportPages.Contains(pagePath))
        {
            var featureAccess = context.HttpContext.RequestServices.GetRequiredService<IFeatureAccessService>();
            var featureKey = pagePath is "/UploadChecklist" or "/ChecklistPreview"
                ? VectorFeatures.GuidedChecklistImport
                : VectorFeatures.GuidedRegisterImport;
            if (!await featureAccess.CanUseFeatureAsync(featureKey, context.HttpContext.RequestAborted))
            {
                context.Result = new RedirectToPageResult("/Home", new { featureUnavailable = "guided-import" });
                return;
            }
        }

        if (allowedAccessViews.Contains(accessView, StringComparer.OrdinalIgnoreCase))
        {
            if (await HasRequiredActionPermissionAsync(context, pagePath))
            {
                await next();
            }

            return;
        }

        if (string.Equals(accessView, CurrentUserService.StaffAccess, StringComparison.OrdinalIgnoreCase)
            && await HasValidTaskAccessAsync(context, pagePath, userId.Value, companyId.Value))
        {
            await next();
            return;
        }

        context.Result = new RedirectToPageResult("/RoleLogin", new { access = allowedAccessViews[0] });
    }

    private static async Task<bool> ShouldRedirectToSetupWizardAsync(VectorDbContext db, int companyId, string pagePath)
    {
        if (CompanySetupAllowedPages.Contains(pagePath))
        {
            return false;
        }

        var company = await db.Companies
            .AsNoTracking()
            .Where(company => company.Id == companyId && company.Status == "Active")
            .FirstOrDefaultAsync();

        return company is null || CompanySetupState.RequiresSetupWizard(company);
    }

    private static async Task<bool> HasRequiredActionPermissionAsync(PageHandlerExecutingContext context, string pagePath)
    {
        var requirement = await ResolvePermissionRequirementAsync(context, pagePath);
        if (requirement is null)
        {
            return true;
        }

        var currentUserService = context.HttpContext.RequestServices.GetRequiredService<CurrentUserService>();
        var currentUser = await currentUserService.GetCurrentUserAsync();
        if (currentUser is null)
        {
            context.Result = new RedirectToPageResult("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
            return false;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IUserActionPermissionService>();
        var hasAll = requirement.AllOf.Count == 0 ||
            await permissionService.HasAllPermissionsAsync(currentUser, requirement.AllOf, context.HttpContext.RequestAborted);
        var hasAny = requirement.AnyOf.Count == 0 ||
            await permissionService.HasAnyPermissionAsync(currentUser, requirement.AnyOf, context.HttpContext.RequestAborted);

        if (hasAll && hasAny)
        {
            return true;
        }

        var hasSavedPermissionRows = await permissionService.HasSavedPermissionRowsAsync(currentUser, context.HttpContext.RequestAborted);
        context.Result = hasSavedPermissionRows
            ? new RedirectToPageResult("/Home", new { permissionDenied = "true" })
            : new RedirectToPageResult("/Home", new { permissionSetupRequired = "true" });
        return false;
    }

    private static async Task<PermissionRequirement?> ResolvePermissionRequirementAsync(PageHandlerExecutingContext context, string pagePath)
    {
        var request = context.HttpContext.Request;
        var handlerName = context.HandlerMethod?.Name ?? string.Empty;
        var isPost = string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase);
        var isDeleteHandler = handlerName.Contains("Delete", StringComparison.OrdinalIgnoreCase);

        if (isDeleteHandler && TryResolveRegisterEditPermission(pagePath, out var deleteRegisterPermission))
        {
            return PermissionRequirement.All(UserActionPermissions.RegistersDelete, deleteRegisterPermission);
        }

        if (pagePath == "/AddItem")
        {
            var type = await GetRequestValueAsync(request, "Type") ?? request.Query["type"].ToString();
            return PermissionRequirement.All(RegisterEditPermissionForType(type));
        }

        if (pagePath == "/EditStaffProfile")
        {
            var targetUserValue = await GetRequestValueAsync(request, "StaffUserId");
            var currentUserId = context.HttpContext.Session.GetInt32(CurrentUserService.UserIdSessionKey);
            if (int.TryParse(targetUserValue, out var targetUserId) &&
                currentUserId.HasValue &&
                targetUserId != currentUserId.Value)
            {
                return PermissionRequirement.All(UserActionPermissions.RegistersStaffEdit);
            }

            return null;
        }

        if (pagePath == "/DailyVehicleChecklist" && isPost)
        {
            var sameAsPreviousValue = await GetRequestValueAsync(request, "SameAsPreviousShift");

            return IsTruthyFormValue(sameAsPreviousValue)
                ? PermissionRequirement.All(UserActionPermissions.DailyChecksComplete, UserActionPermissions.DailySamePrevious)
                : PermissionRequirement.All(UserActionPermissions.DailyChecksComplete);
        }

        if (pagePath == "/EditVehicleChecklist")
        {
            if (isPost)
            {
                var actionType = await GetRequestValueAsync(request, "ActionType");
                if (string.Equals(actionType, "approve-publish", StringComparison.OrdinalIgnoreCase))
                {
                    return PermissionRequirement.All(UserActionPermissions.ChecklistsPublish);
                }

                return PermissionRequirement.Any(UserActionPermissions.ChecklistsBuild, UserActionPermissions.ChecklistsEdit);
            }

            var mode = request.Query["mode"].ToString();
            var hasTemplateId = !string.IsNullOrWhiteSpace(request.Query["templateId"].ToString());
            if (string.Equals(mode, "build", StringComparison.OrdinalIgnoreCase) && !hasTemplateId)
            {
                return PermissionRequirement.All(UserActionPermissions.ChecklistsBuild);
            }

            if (hasTemplateId || string.Equals(mode, "edit", StringComparison.OrdinalIgnoreCase))
            {
                return PermissionRequirement.All(UserActionPermissions.ChecklistsEdit);
            }

            return PermissionRequirement.Any(UserActionPermissions.ChecklistsBuild, UserActionPermissions.ChecklistsEdit);
        }

        if (pagePath == "/EditChecklist")
        {
            return isDeleteHandler
                ? PermissionRequirement.All(UserActionPermissions.ChecklistsEdit)
                : PermissionRequirement.Any(
                    UserActionPermissions.ChecklistsBuild,
                    UserActionPermissions.ChecklistsEdit,
                    UserActionPermissions.ChecklistsPublish,
                    UserActionPermissions.ChecklistsReports);
        }

        if (pagePath == "/StockOrderAction")
        {
            if (handlerName.Contains("Approve", StringComparison.OrdinalIgnoreCase))
            {
                return PermissionRequirement.All(UserActionPermissions.StockOrdersApprove);
            }

            if (!isPost)
            {
                return PermissionRequirement.Any(
                    UserActionPermissions.RegistersStockEdit,
                    UserActionPermissions.StockOrdersApprove);
            }

            return PermissionRequirement.All(UserActionPermissions.RegistersStockEdit);
        }

        if (pagePath == "/TaskInbox" && isDeleteHandler)
        {
            return PermissionRequirement.All(UserActionPermissions.TasksManage);
        }

        if (pagePath == "/IssueInbox" && isDeleteHandler)
        {
            return PermissionRequirement.All(UserActionPermissions.IssuesManage);
        }

        return pagePath switch
        {
            "/DailyVehicleChecklist" or "/FullAudit" or "/FullAuditVehicleChecklist" or "/FullAuditEquipmentChecklist"
                => PermissionRequirement.All(UserActionPermissions.DailyChecksComplete),

            "/Vehicles" or "/VehicleRegister" or "/Equipment" or "/EquipmentRegister" or "/Stock" or "/StockRegister" or "/Medication" or "/MedicationRegister" or "/Staff" or "/StaffRegister"
                => PermissionRequirement.All(UserActionPermissions.RegistersView),

            "/EditVehicle" or "/ReassignVehicleCallsign"
                => PermissionRequirement.All(UserActionPermissions.RegistersVehicleEdit),

            "/EditEquipmentItem"
                => PermissionRequirement.All(UserActionPermissions.RegistersEquipmentEdit),

            "/EditStockItem" or "/PlaceStockOrder" or "/StockOrders"
                => PermissionRequirement.All(UserActionPermissions.RegistersStockEdit),

            "/EditMedicationItem"
                => PermissionRequirement.All(UserActionPermissions.RegistersMedicationEdit),

            "/MoveAsset"
                => PermissionRequirement.All(UserActionPermissions.AssetsMove),

            "/EquipmentService" or "/ExpiryNotifications"
                => PermissionRequirement.All(UserActionPermissions.AssetsServiceUpdate),

            "/SendTask"
                => PermissionRequirement.All(UserActionPermissions.TasksSend),

            "/TaskFeedback"
                => PermissionRequirement.All(UserActionPermissions.TasksFeedback),

            "/ReportIssue"
                => PermissionRequirement.All(UserActionPermissions.IssuesReport),

            "/IssueInbox" or "/IssueReports" or "/IssueReportAction"
                => PermissionRequirement.All(UserActionPermissions.IssuesManage),

            "/TasksIssues"
                => PermissionRequirement.Any(UserActionPermissions.TasksSend, UserActionPermissions.IssuesManage),

            "/Readiness"
                => PermissionRequirement.Any(
                    UserActionPermissions.DashboardReadiness,
                    UserActionPermissions.ChecklistsBuild,
                    UserActionPermissions.ChecklistsEdit,
                    UserActionPermissions.ChecklistsPublish,
                    UserActionPermissions.ChecklistsReports,
                    UserActionPermissions.ReportsOperations,
                    UserActionPermissions.AssetsServiceUpdate),

            "/ReadinessDashboard" or "/ReadinessMetric" or "/ReadinessMetricDetail" or "/ReadinessAlerts"
                => PermissionRequirement.All(UserActionPermissions.DashboardReadiness),

            "/OperationsReports"
                => PermissionRequirement.All(UserActionPermissions.ReportsOperations),

            "/ReadinessEngine"
                => PermissionRequirement.All(UserActionPermissions.ReadinessEngine),

            "/ChecklistReports" or "/ChecklistReportDetail"
                => PermissionRequirement.All(UserActionPermissions.ChecklistsReports),

            "/ChecklistTemplateView"
                => PermissionRequirement.Any(UserActionPermissions.ChecklistsReports, UserActionPermissions.ChecklistsEdit, UserActionPermissions.ChecklistsPublish),

            "/ChecklistVarianceAlerts"
                => PermissionRequirement.All(UserActionPermissions.ChecklistsVarianceReview),

            "/ChecklistApproval"
                => PermissionRequirement.All(UserActionPermissions.ChecklistsPublish),

            "/UploadChecklist" or "/ChecklistPreview" or "/ImportBatch"
                => PermissionRequirement.All(UserActionPermissions.ImportsPrepare),

            "/VehicleSchematicLibrary"
                => PermissionRequirement.All(UserActionPermissions.ChecklistsEdit),

            "/UploadVehicleRegister" or "/VehicleRegisterPreview"
                => PermissionRequirement.All(UserActionPermissions.ImportsPrepare),

            "/UploadEquipmentRegister" or "/EquipmentRegisterPreview"
                => PermissionRequirement.All(UserActionPermissions.ImportsPrepare),

            "/UploadStockRegister" or "/StockRegisterPreview"
                => PermissionRequirement.All(UserActionPermissions.ImportsPrepare),

            "/UploadMedicationRegister" or "/MedicationRegisterPreview"
                => PermissionRequirement.All(UserActionPermissions.ImportsPrepare),

            "/UploadStaffRegister" or "/StaffRegisterPreview"
                => PermissionRequirement.All(UserActionPermissions.ImportsPrepare),

            "/UploadStaffFiles" or "/StaffFiles"
                => PermissionRequirement.All(UserActionPermissions.RegistersStaffEdit),

            "/AreaManagerControl"
                => PermissionRequirement.Any(UserActionPermissions.SetupAreas, UserActionPermissions.SetupAccess),

            "/OperationalAreas" or "/OperationalStructureSetup" or "/VehicleStructureSetup" or "/StaffStructureSetup"
                => PermissionRequirement.All(UserActionPermissions.SetupAreas),

            "/CreateManagerAccess" or "/CreateOperationalStaffAccess"
                => PermissionRequirement.All(UserActionPermissions.SetupAccess),

            "/AccessModelSetup"
                => PermissionRequirement.All(UserActionPermissions.SetupAccess),

            "/AssetRegisterSetup"
                => PermissionRequirement.Any(
                    UserActionPermissions.SetupCompany,
                    UserActionPermissions.RegistersVehicleEdit,
                    UserActionPermissions.RegistersEquipmentEdit,
                    UserActionPermissions.RegistersStockEdit,
                    UserActionPermissions.RegistersMedicationEdit,
                    UserActionPermissions.RegistersStaffEdit),

            "/ChecklistSetup"
                => PermissionRequirement.Any(
                    UserActionPermissions.SetupCompany,
                    UserActionPermissions.ChecklistsBuild,
                    UserActionPermissions.ChecklistsPublish,
                    UserActionPermissions.ChecklistsUpload),

            "/ReadinessEngineSetup"
                => PermissionRequirement.Any(
                    UserActionPermissions.SetupCompany,
                    UserActionPermissions.ReadinessEngine),

            "/CompanyProfile" or "/CompanyName" or "/LogoUpload" or "/SupplierDetails"
                => PermissionRequirement.All(UserActionPermissions.SetupCompany),

            "/AuditLog"
                => PermissionRequirement.All(UserActionPermissions.SetupAudit),

            _ => null
        };
    }

    private static bool TryResolveRegisterEditPermission(string pagePath, out string permissionKey)
    {
        permissionKey = pagePath switch
        {
            "/VehicleRegister" => UserActionPermissions.RegistersVehicleEdit,
            "/EquipmentRegister" => UserActionPermissions.RegistersEquipmentEdit,
            "/StockRegister" => UserActionPermissions.RegistersStockEdit,
            "/MedicationRegister" => UserActionPermissions.RegistersMedicationEdit,
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(permissionKey);
    }

    private static string RegisterEditPermissionForType(string? itemType)
    {
        return itemType?.Trim().ToLowerInvariant() switch
        {
            "vehicle" => UserActionPermissions.RegistersVehicleEdit,
            "stock" => UserActionPermissions.RegistersStockEdit,
            "medication" => UserActionPermissions.RegistersMedicationEdit,
            "staff" => UserActionPermissions.RegistersStaffEdit,
            _ => UserActionPermissions.RegistersEquipmentEdit
        };
    }

    private static bool IsTruthyFormValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(part =>
                string.Equals(part, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(part, "on", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(part, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(part, "1", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string?> GetRequestValueAsync(HttpRequest request, string key)
    {
        var queryValue = request.Query[key].ToString();
        if (!string.IsNullOrWhiteSpace(queryValue))
        {
            return queryValue;
        }

        if (!request.HasFormContentType)
        {
            return null;
        }

        var form = await request.ReadFormAsync();
        var formValue = form[key].ToString();
        return string.IsNullOrWhiteSpace(formValue) ? null : formValue;
    }

    private static async Task<bool> HasValidTaskAccessAsync(PageHandlerExecutingContext context, string pagePath, int currentUserId, int currentCompanyId)
    {
        var request = context.HttpContext.Request;
        var handlerName = context.HandlerMethod?.Name ?? string.Empty;
        if (string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase) &&
            handlerName.Contains("Delete", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TaskAccessibleManagementPages.Contains(pagePath) && !TaskActionCatalog.CanBeOpenedWithTaskAccess(pagePath))
        {
            return false;
        }

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
                && taskItem.CompanyId == currentCompanyId
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

        return TaskActionCatalog.AllowsPage(task.ActionType, pagePath, request.Query);
    }

    private static bool QueryEquals(IQueryCollection query, string key, string expectedValue)
    {
        return string.Equals(query[key].ToString(), expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static void ClearCurrentUserSession(ISession session)
    {
        session.Remove(CurrentUserService.UserIdSessionKey);
        session.Remove(CurrentUserService.FullNameSessionKey);
        session.Remove(CurrentUserService.RoleNameSessionKey);
        session.Remove(CurrentUserService.AccessViewSessionKey);
    }
}
