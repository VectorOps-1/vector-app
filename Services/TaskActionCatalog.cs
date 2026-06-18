namespace vector_app_local.Services;

public static class TaskActionCatalog
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

    private static readonly string[] TaskSenderAccess =
    {
        CurrentUserService.OperationalManagementAccess,
        CurrentUserService.SeniorManagementAccess
    };

    private static readonly string[] SeniorTaskSenderAccess =
    {
        CurrentUserService.SeniorManagementAccess
    };

    public static IReadOnlyList<TaskActionOption> Actions { get; } = new List<TaskActionOption>
    {
        All("Daily Vehicle & Equipment Check", "/DailyVehicleChecklist", "Operational Work"),
        All("Complete Operational Check", "/CompleteChecklist", "Operational Work"),
        All("Full Audit", "/DailyVehicleChecklist", "Operational Work", ("frequency", "full-audit")),
        All("Report Issue", "/ReportIssue", "Operational Work", ("module", "general")),
        All("Update My Profile / Documents", "/MyProfile", "Operational Work"),

        Management("Shift Readiness Dashboard", "/ReadinessDashboard", "Readiness"),
        Management("Readiness Engine", "/ReadinessEngine", "Readiness"),
        Management("Operational Reports", "/OperationsReports", "Reporting"),
        Management("Review Checklist Variance Alerts", "/ChecklistVarianceAlerts", "Reporting"),
        Management("Review Readiness Alerts", "/ReadinessAlerts", "Reporting"),

        Management("Open Vehicles", "/Vehicles", "Vehicles"),
        Management("Vehicle Register", "/VehicleRegister", "Vehicles"),
        Management("Add New Vehicle", "/AddItem", "Vehicles", ("type", "vehicle")),
        Management("Reassign Vehicle Callsign", "/ReassignVehicleCallsign", "Vehicles"),
        Management("Move / Reallocate Vehicle", "/MoveAsset", "Vehicles", ("asset", "vehicle")),

        Management("Open Equipment", "/Equipment", "Equipment"),
        Management("Equipment Register", "/EquipmentRegister", "Equipment", ("view", "register")),
        Management("Add New Equipment", "/AddItem", "Equipment", ("type", "equipment")),
        Management("Update Equipment Service Dates", "/EquipmentService", "Equipment"),
        Management("Move / Reallocate Equipment", "/MoveAsset", "Equipment", ("asset", "equipment")),

        Management("Open Staff", "/Staff", "Staff"),
        Management("Staff Register", "/StaffRegister", "Staff"),
        Management("Add Staff Profile", "/AddItem", "Staff", ("type", "staff")),

        Management("Open Stock", "/Stock", "Stock"),
        Management("Stock Register", "/StockRegister", "Stock", ("view", "register")),
        Management("Place Stock Order", "/PlaceStockOrder", "Stock"),
        Management("Supplier Confirmations", "/SupplierConfirmations", "Stock"),
        Management("Enter Stock Into Register", "/EnterStockRegister", "Stock"),
        Management("Allocate Stock", "/AllocateStock", "Stock"),
        Management("Receive Stock", "/EnterStockRegister", "Stock"),
        Management("Issue / Allocate Stock", "/AllocateStock", "Stock"),
        Management("Batch Number Tracking", "/StockRegister", "Stock"),
        Management("Expiry / Compliance Check", "/StockRegister", "Stock"),
        Management("Add New Stock Item", "/AddItem", "Stock", ("type", "stock")),
        Management("Move / Reallocate Stock", "/MoveAsset", "Stock", ("asset", "stock")),

        Management("Open Medication", "/Medication", "Medication"),
        Management("Medication Register", "/MedicationRegister", "Medication", ("view", "register")),
        Management("Add Medication", "/AddItem", "Medication", ("type", "medication")),
        Management("Move / Reallocate Medication", "/MoveAsset", "Medication", ("asset", "medication")),

        Management("My Assigned Issues", "/IssueInbox", "Tasks & Issues"),
        Management("All Open Issues", "/IssueReports", "Tasks & Issues"),
        Management("Send Task", "/SendTask", "Tasks & Issues"),

        Senior("Master Setup", "/MasterSetup", "System Setup"),
        Senior("Area / Manager Control", "/AreaManagerControl", "System Setup"),
        Senior("Company Profile", "/CompanyProfile", "System Setup"),
        Senior("Company Name", "/CompanyName", "System Setup"),
        Senior("Logo Upload", "/LogoUpload", "System Setup"),
        Senior("Supplier Details", "/SupplierDetails", "System Setup"),
        Senior("Checklist Management", "/EditChecklist", "System Setup"),
        Senior("Readiness Engine Setup", "/ReadinessEngine", "System Setup"),
        Senior("Task Communication Setup", "/TaskCommunicationSetup", "System Setup"),
        Senior("Audit Log", "/AuditLog", "System Setup"),
        Senior("Upload Vehicle Register", "/UploadVehicleRegister", "System Setup"),
        Senior("Upload Equipment Register", "/UploadEquipmentRegister", "System Setup"),
        Senior("Upload Staff Register", "/UploadStaffRegister", "System Setup"),
        Senior("Upload Stock Register", "/UploadStockRegister", "System Setup"),
        Senior("Upload Medication Register", "/UploadMedicationRegister", "System Setup"),
        Senior("Onboarding Workspace Link", "/Onboarding", "System Setup")
    };

    public static IEnumerable<TaskActionOption> GetActionsForSender(string? senderAccessView)
    {
        var accessView = CurrentUserService.NormalizeAccessView(senderAccessView);
        return Actions.Where(action => action.AllowedSenderAccessViews.Contains(accessView));
    }

    public static TaskActionOption? Find(string? actionType)
    {
        return Actions.FirstOrDefault(action =>
            string.Equals(action.Value, actionType, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsAllowedForRecipient(string? actionType, string? recipientRoleName)
    {
        var recipientAccess = AccessViewForRoleName(recipientRoleName);
        var action = Find(actionType);
        return action is not null && action.AllowedRecipientAccessViews.Contains(recipientAccess);
    }

    public static bool IsAllowedForSenderAndRecipient(string? actionType, string? senderAccessView, string? recipientRoleName)
    {
        var senderAccess = CurrentUserService.NormalizeAccessView(senderAccessView);
        var recipientAccess = AccessViewForRoleName(recipientRoleName);
        var action = Find(actionType);

        return action is not null
            && action.AllowedSenderAccessViews.Contains(senderAccess)
            && action.AllowedRecipientAccessViews.Contains(recipientAccess);
    }

    public static string AccessViewForRoleName(string? roleName)
    {
        if (CurrentUserService.IsSeniorAccessRole(roleName))
        {
            return CurrentUserService.SeniorManagementAccess;
        }

        return string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase)
            ? CurrentUserService.StaffAccess
            : CurrentUserService.OperationalManagementAccess;
    }

    public static string BuildTaskUrl(string actionType, int taskId, string? relatedItemReference = null)
    {
        if (actionType.Contains("Checklist approval request", StringComparison.OrdinalIgnoreCase))
        {
            return $"/ChecklistApproval?taskId={taskId}";
        }

        if (actionType.Contains("Checklist modification request", StringComparison.OrdinalIgnoreCase) &&
            TryParseChecklistTemplateId(relatedItemReference, out var templateId))
        {
            return $"/EditVehicleChecklist?checklist=daily-vehicle&mode=edit&templateId={templateId}&taskAccess=true&taskId={taskId}";
        }

        var action = Find(actionType);
        if (action is null)
        {
            return $"/TaskFeedback?taskId={taskId}";
        }

        var queryValues = action.RouteValues
            .Concat(new[]
            {
                new KeyValuePair<string, string>("taskId", taskId.ToString()),
                new KeyValuePair<string, string>("taskAccess", "true")
            })
            .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}");

        return $"{action.PagePath}?{string.Join("&", queryValues)}";
    }

    public static bool CanBeOpenedWithTaskAccess(string pagePath)
    {
        return Actions.Any(action =>
            action.AllowedRecipientAccessViews.SequenceEqual(ManagementAccess) &&
            string.Equals(action.PagePath, pagePath, StringComparison.OrdinalIgnoreCase));
    }

    public static bool AllowsPage(string actionType, string pagePath, IQueryCollection query)
    {
        if (actionType.Contains("Checklist approval request", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(pagePath, "/ChecklistApproval", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pagePath, "/PublishChecklist", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pagePath, "/ChecklistTemplateView", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (actionType.Contains("Checklist modification request", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(pagePath, "/EditVehicleChecklist", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pagePath, "/ChecklistTemplateView", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var action = Find(actionType);
        if (action is null || !string.Equals(action.PagePath, pagePath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return action.RouteValues.All(routeValue =>
            string.Equals(query[routeValue.Key].ToString(), routeValue.Value, StringComparison.OrdinalIgnoreCase));
    }

    public static bool TryParseChecklistTemplateId(string? relatedItemReference, out int templateId)
    {
        templateId = 0;
        const string prefix = "ChecklistTemplate:";

        if (string.IsNullOrWhiteSpace(relatedItemReference) ||
            !relatedItemReference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(relatedItemReference[prefix.Length..], out templateId);
    }

    private static TaskActionOption All(
        string label,
        string pagePath,
        string category,
        params (string Key, string Value)[] routeValues)
    {
        return Create(label, pagePath, category, TaskSenderAccess, AllSignedInAccess, routeValues);
    }

    private static TaskActionOption Management(
        string label,
        string pagePath,
        string category,
        params (string Key, string Value)[] routeValues)
    {
        return Create(label, pagePath, category, TaskSenderAccess, ManagementAccess, routeValues);
    }

    private static TaskActionOption Senior(
        string label,
        string pagePath,
        string category,
        params (string Key, string Value)[] routeValues)
    {
        return Create(label, pagePath, category, SeniorTaskSenderAccess, SeniorAccess, routeValues);
    }

    private static TaskActionOption Create(
        string label,
        string pagePath,
        string category,
        IReadOnlyList<string> allowedSenderAccessViews,
        IReadOnlyList<string> allowedRecipientAccessViews,
        IReadOnlyList<(string Key, string Value)> routeValues)
    {
        return new TaskActionOption(
            label,
            label,
            pagePath,
            routeValues.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase),
            category,
            allowedSenderAccessViews,
            allowedRecipientAccessViews);
    }
}

public sealed record TaskActionOption(
    string Value,
    string Label,
    string PagePath,
    IReadOnlyDictionary<string, string> RouteValues,
    string Category,
    IReadOnlyList<string> AllowedSenderAccessViews,
    IReadOnlyList<string> AllowedRecipientAccessViews);
