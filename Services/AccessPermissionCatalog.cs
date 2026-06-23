namespace vector_app_local.Services;

public static class AccessPermissionCatalog
{
    public static readonly IReadOnlyList<AccessPermissionGroup> Groups = new List<AccessPermissionGroup>
    {
        new("Registers", new[]
        {
            new AccessPermissionOption(UserActionPermissions.RegistersView, "View registers", "Open vehicle, equipment, stock, medication, and staff registers in permitted scope."),
            new AccessPermissionOption(UserActionPermissions.RegistersVehicleEdit, "Edit vehicle register", "Add or edit vehicle source-of-truth records."),
            new AccessPermissionOption(UserActionPermissions.RegistersEquipmentEdit, "Edit equipment register", "Add or edit equipment source-of-truth records."),
            new AccessPermissionOption(UserActionPermissions.RegistersStockEdit, "Edit stock register", "Add or edit disposable stock source-of-truth records."),
            new AccessPermissionOption(UserActionPermissions.RegistersMedicationEdit, "Edit medication register", "Add or edit medication source-of-truth records."),
            new AccessPermissionOption(UserActionPermissions.RegistersStaffEdit, "Edit staff register", "Add or edit staff profiles and register-controlled staff fields."),
            new AccessPermissionOption(UserActionPermissions.RegistersDelete, "Delete register records", "Remove register records where deletion is allowed."),
            new AccessPermissionOption(UserActionPermissions.AssetsMove, "Move / reallocate assets", "Move vehicles, equipment, stock, and medication between areas, vehicles, and storage."),
            new AccessPermissionOption(UserActionPermissions.AssetsServiceUpdate, "Update service / expiry dates", "Update service dates, expiry dates, licensing, and similar register dates.")
        }),
        new("Checklist Management", new[]
        {
            new AccessPermissionOption(UserActionPermissions.ChecklistsBuild, "Build checklists", "Create new checklist templates manually."),
            new AccessPermissionOption(UserActionPermissions.ChecklistsEdit, "Edit saved checklists", "Edit existing checklist templates."),
            new AccessPermissionOption(UserActionPermissions.ChecklistsPublish, "Publish checklists", "Publish active checklists for areas, functions, subtypes, or vehicles."),
            new AccessPermissionOption(UserActionPermissions.ChecklistsUpload, "Upload checklists / registers", "Upload existing checklist and register source files."),
            new AccessPermissionOption(UserActionPermissions.ChecklistsReports, "View checklist reports", "Open submitted checklist evidence and PDF reports."),
            new AccessPermissionOption(UserActionPermissions.ChecklistsVarianceReview, "Review checklist variance alerts", "Approve or reject captured differences that may update registers.")
        }),
        new("Daily Operations", new[]
        {
            new AccessPermissionOption(UserActionPermissions.DailyChecksComplete, "Complete daily checks", "Complete assigned live vehicle and equipment checks."),
            new AccessPermissionOption(UserActionPermissions.DailySamePrevious, "Use same as previous shift", "Use same-as-previous-shift controls where enabled."),
            new AccessPermissionOption(UserActionPermissions.IssuesReport, "Report issues", "Create operational issues from inside the app."),
            new AccessPermissionOption(UserActionPermissions.IssuesManage, "Manage issues", "Assign, resolve, delete, or close issues in permitted scope."),
            new AccessPermissionOption(UserActionPermissions.TasksSend, "Send tasks", "Send tasks to users in permitted scope."),
            new AccessPermissionOption(UserActionPermissions.TasksManage, "Manage tasks", "Delete, close, or update tasks in permitted scope."),
            new AccessPermissionOption(UserActionPermissions.TasksFeedback, "Submit task feedback", "Submit feedback on assigned tasks or general app work."),
            new AccessPermissionOption(UserActionPermissions.StockOrdersApprove, "Approve stock orders", "Approve stock order requests for supplier and register workflow.")
        }),
        new("Oversight And Setup", new[]
        {
            new AccessPermissionOption(UserActionPermissions.DashboardReadiness, "View readiness dashboard", "Open readiness score, metrics, and readiness variables."),
            new AccessPermissionOption(UserActionPermissions.ReportsOperations, "View operational reports", "Open operational reports for permitted scope."),
            new AccessPermissionOption(UserActionPermissions.ReadinessEngine, "Use readiness engine", "Edit or request scoring rules and readiness weightings."),
            new AccessPermissionOption(UserActionPermissions.SetupAreas, "Manage areas / manager control", "Create areas and manage manager area assignment."),
            new AccessPermissionOption(UserActionPermissions.SetupCompany, "Manage company profile", "Edit client details, logo, workspace, and company-level settings."),
            new AccessPermissionOption(UserActionPermissions.SetupAudit, "View audit log", "Open app audit logs."),
            new AccessPermissionOption(UserActionPermissions.SetupAccess, "Manage access setup", "Change roles, areas, and granular permissions for users below own access level.")
        })
    };

    public static readonly IReadOnlyDictionary<string, string> PermissionNames = Groups
        .SelectMany(group => group.Options)
        .ToDictionary(option => option.Key, option => option.Name, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlySet<string> ValidKeys = Groups
        .SelectMany(group => group.Options)
        .Select(option => option.Key)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> NormalizePermissionKeys(IEnumerable<string>? permissionKeys)
    {
        return (permissionKeys ?? Enumerable.Empty<string>())
            .Where(key => ValidKeys.Contains(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key)
            .ToList();
    }

    public static string SerializePermissionKeys(IEnumerable<string>? permissionKeys)
    {
        return string.Join(",", NormalizePermissionKeys(permissionKeys));
    }

    public static IReadOnlyList<string> ParsePermissionKeys(string? serializedPermissionKeys)
    {
        return NormalizePermissionKeys((serializedPermissionKeys ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public static string DescribePermissionSummary(IReadOnlyCollection<string> permissionKeys)
    {
        if (permissionKeys.Count == 0)
        {
            return "No action permissions";
        }

        var permissionNames = permissionKeys
            .Select(key => PermissionNames.TryGetValue(key, out var name) ? name : key)
            .OrderBy(name => name)
            .ToList();

        var visibleNames = permissionNames.Take(4);
        var suffix = permissionNames.Count > 4
            ? $" + {permissionNames.Count - 4} more"
            : string.Empty;

        return string.Join(", ", visibleNames) + suffix;
    }
}

public sealed record AccessPermissionGroup(string Name, IReadOnlyList<AccessPermissionOption> Options);

public sealed record AccessPermissionOption(string Key, string Name, string Description);
