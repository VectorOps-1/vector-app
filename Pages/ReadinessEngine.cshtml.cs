using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ReadinessEngineModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly ReadinessEngineService _engineService;
    private readonly AuditTrailService _auditTrail;

    public ReadinessEngineModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        ReadinessEngineService engineService,
        AuditTrailService auditTrail)
    {
        _db = db;
        _currentUser = currentUser;
        _engineService = engineService;
        _auditTrail = auditTrail;
    }

    [BindProperty(SupportsGet = true)] public string? Confirmation { get; set; }
    [BindProperty] public int VersionId { get; set; }
    [BindProperty] public List<RuleInput> Rules { get; set; } = [];
    [BindProperty] public RuleInput NewRule { get; set; } = new();
    [BindProperty] public int RuleId { get; set; }
    [BindProperty] public int RequestId { get; set; }
    [BindProperty] public string? ProposedSeverity { get; set; }
    [BindProperty] public int? ProposedImpactPercent { get; set; }
    [BindProperty] public bool ProposedActive { get; set; }
    [BindProperty] public string? RequestReason { get; set; }
    [BindProperty] public string? DecisionNote { get; set; }

    public string? StatusMessage { get; private set; }
    public bool IsSeniorManager { get; private set; }
    public bool IsOperationalManager { get; private set; }
    public string CurrentUserLabel { get; private set; } = string.Empty;
    public ReadinessEngineVersion? ActiveVersion { get; private set; }
    public ReadinessEngineVersion? WorkingVersion { get; private set; }
    public List<ReadinessRuleRow> RuleRows { get; private set; } = [];
    public List<ScoringRequestRow> PendingRequests { get; private set; } = [];
    public List<ScoringRequestRow> OwnRequests { get; private set; } = [];
    public string[] SeverityOptions { get; } = ReadinessRuleSeverity.Options;
    public string[] ReadinessScopeOptions { get; } = ReadinessRuleScope.Options;
    public string[] AssetTypeOptions { get; } =
    [
        "Checklist Completion",
        "Vehicle",
        "Equipment",
        "Stock",
        "Medication",
        "Issue Report",
        "Custom"
    ];

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!CanUseReadinessEngine(currentUser))
        {
            return RedirectToPage("/Home");
        }

        await LoadPageDataAsync(currentUser);
        ApplyConfirmationMessage();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null || !CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        var draft = await _db.ReadinessEngineVersions
            .Include(version => version.Rules)
            .FirstOrDefaultAsync(version =>
                version.Id == VersionId &&
                version.CompanyId == currentUser.CompanyId &&
                version.Status == ReadinessEngineStatuses.Draft);

        if (draft is null)
        {
            return RedirectToPage("/ReadinessEngine", new { confirmation = "missing-draft" });
        }

        var inputsById = Rules
            .Where(input => input.Id > 0)
            .ToDictionary(input => input.Id);
        var now = DateTime.UtcNow;

        foreach (var rule in draft.Rules)
        {
            if (!inputsById.TryGetValue(rule.Id, out var input))
            {
                continue;
            }

            ApplyRuleInput(rule, input, now);
        }

        draft.UpdatedAtUtc = now;
        _auditTrail.Record(
            currentUser,
            "Readiness engine rules saved",
            "ReadinessEngineVersion",
            draft.Id,
            $"{Rules.Count} readiness scoring rule(s) reviewed and saved in {draft.Name} {draft.VersionNumber}.",
            now);
        await _db.SaveChangesAsync();
        return RedirectToPage("/ReadinessEngine", new { confirmation = "rules-saved" });
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null || !CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        var draft = await _db.ReadinessEngineVersions
            .Include(version => version.Rules)
            .FirstOrDefaultAsync(version =>
                version.Id == VersionId &&
                version.CompanyId == currentUser.CompanyId &&
                version.Status == ReadinessEngineStatuses.Draft);

        if (draft is null)
        {
            return RedirectToPage("/ReadinessEngine", new { confirmation = "missing-draft" });
        }

        var rule = new ReadinessEngineRule
        {
            CompanyId = currentUser.CompanyId,
            ReadinessEngineVersionId = draft.Id,
            SortOrder = draft.Rules.Count == 0 ? 10 : draft.Rules.Max(item => item.SortOrder) + 10,
            SourceType = "Custom",
            CreatedAtUtc = DateTime.UtcNow
        };

        ApplyRuleInput(rule, NewRule, DateTime.UtcNow);
        draft.Rules.Add(rule);
        draft.UpdatedAtUtc = DateTime.UtcNow;
        _auditTrail.Record(
            currentUser,
            "Readiness engine rule added",
            "ReadinessEngineVersion",
            draft.Id,
            $"Added scoring rule: {rule.AssetType} / {rule.ItemName} / {rule.TriggerValue}.");
        await _db.SaveChangesAsync();
        return RedirectToPage("/ReadinessEngine", new { confirmation = "rule-added" });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null || !CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        var rule = await _db.ReadinessEngineRules
            .Include(item => item.ReadinessEngineVersion)
            .FirstOrDefaultAsync(item =>
                item.Id == RuleId &&
                item.CompanyId == currentUser.CompanyId &&
                item.ReadinessEngineVersion != null &&
                item.ReadinessEngineVersion.Status == ReadinessEngineStatuses.Draft);

        if (rule is not null)
        {
            _auditTrail.Record(
                currentUser,
                "Readiness engine rule deleted",
                "ReadinessEngineRule",
                rule.Id,
                $"Deleted scoring rule: {rule.AssetType} / {rule.ItemName} / {rule.TriggerValue}.");
            _db.ReadinessEngineRules.Remove(rule);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/ReadinessEngine", new { confirmation = "rule-deleted" });
    }

    public async Task<IActionResult> OnPostDuplicateAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null || !CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        var source = await _db.ReadinessEngineRules
            .AsNoTracking()
            .Include(item => item.ReadinessEngineVersion)
            .FirstOrDefaultAsync(item =>
                item.Id == RuleId &&
                item.CompanyId == currentUser.CompanyId &&
                item.ReadinessEngineVersion != null &&
                item.ReadinessEngineVersion.Status == ReadinessEngineStatuses.Draft);

        if (source is not null)
        {
            var nextOrder = await _db.ReadinessEngineRules
                .Where(item => item.ReadinessEngineVersionId == source.ReadinessEngineVersionId)
                .MaxAsync(item => (int?)item.SortOrder) ?? 0;

            _db.ReadinessEngineRules.Add(new ReadinessEngineRule
            {
                CompanyId = currentUser.CompanyId,
                ReadinessEngineVersionId = source.ReadinessEngineVersionId,
                AssetType = source.AssetType,
                Section = source.Section,
                ItemName = $"{source.ItemName} copy",
                FieldKey = source.FieldKey,
                TriggerValue = source.TriggerValue,
                AppliesTo = source.AppliesTo,
                ReadinessScope = source.ReadinessScope,
                TargetVehicleType = source.TargetVehicleType,
                OperationalAreaId = source.OperationalAreaId,
                ChecklistTemplateId = source.ChecklistTemplateId,
                Severity = source.Severity,
                DefaultImpactPercent = source.DefaultImpactPercent,
                ManualImpactPercent = source.ManualImpactPercent,
                IsHardBlocker = source.IsHardBlocker,
                ManagerAlert = source.ManagerAlert,
                IsActive = source.IsActive,
                IsAutoPopulated = false,
                SourceType = "Custom",
                SourceEntityType = source.SourceEntityType,
                SourceEntityId = source.SourceEntityId,
                Notes = source.Notes,
                SortOrder = nextOrder + 10,
                CreatedAtUtc = DateTime.UtcNow
            });

            _auditTrail.Record(
                currentUser,
                "Readiness engine rule duplicated",
                "ReadinessEngineRule",
                source.Id,
                $"Duplicated scoring rule: {source.AssetType} / {source.ItemName} / {source.TriggerValue}.");
            await _db.SaveChangesAsync();
        }

        return RedirectToPage("/ReadinessEngine", new { confirmation = "rule-duplicated" });
    }

    public async Task<IActionResult> OnPostAutopopulateAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null || !CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        var added = await _engineService.AutoPopulateSuggestedRulesAsync(currentUser, VersionId);
        return RedirectToPage("/ReadinessEngine", new { confirmation = added == 0 ? "no-new-rules" : "rules-autopopulated" });
    }

    public async Task<IActionResult> OnPostPublishAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null || !CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        await _engineService.PublishDraftAsync(currentUser, VersionId);
        return RedirectToPage("/ReadinessEngine", new { confirmation = "engine-published" });
    }

    public async Task<IActionResult> OnPostRequestChangeAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null || !IsOpsManager(currentUser))
        {
            return RedirectToPage("/Home");
        }

        if (string.IsNullOrWhiteSpace(RequestReason))
        {
            return RedirectToPage("/ReadinessEngine", new { confirmation = "request-needs-reason" });
        }

        await _engineService.CreateChangeRequestAsync(
            currentUser,
            RuleId,
            ProposedSeverity ?? ReadinessRuleSeverity.Moderate,
            ProposedImpactPercent,
            ProposedActive,
            RequestReason);

        return RedirectToPage("/ReadinessEngine", new { confirmation = "request-sent" });
    }

    public async Task<IActionResult> OnPostReviewRequestAsync(bool approve)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null || !CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        await _engineService.ReviewChangeRequestAsync(currentUser, RequestId, approve, DecisionNote);
        return RedirectToPage("/ReadinessEngine", new { confirmation = approve ? "request-approved" : "request-rejected" });
    }

    public async Task<IActionResult> OnPostDeleteRequestAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null || !CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return RedirectToPage("/Home");
        }

        await _engineService.DeleteChangeRequestAsync(currentUser, RequestId, DecisionNote);
        return RedirectToPage("/ReadinessEngine", null, new { confirmation = "request-deleted" }, "scoringRequests");
    }

    private async Task LoadPageDataAsync(AppUser currentUser)
    {
        IsSeniorManager = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        IsOperationalManager = IsOpsManager(currentUser);
        CurrentUserLabel = $"{currentUser.FullName} ({currentUser.AppRole?.Name})";

        ActiveVersion = await _engineService.LoadPublishedVersionAsync(currentUser.CompanyId);
        if (ActiveVersion is null)
        {
            await ReadinessEngineService.EnsureDefaultPublishedEngineAsync(_db, currentUser.CompanyId, currentUser.Id);
            ActiveVersion = await _engineService.LoadPublishedVersionAsync(currentUser.CompanyId);
        }

        WorkingVersion = IsSeniorManager
            ? await _engineService.EnsureDraftVersionAsync(currentUser)
            : ActiveVersion;

        RuleRows = (WorkingVersion?.Rules ?? [])
            .OrderBy(rule => rule.SortOrder)
            .ThenBy(rule => rule.AssetType)
            .ThenBy(rule => rule.ItemName)
            .Select(ReadinessRuleRow.FromRule)
            .ToList();

        Rules = RuleRows.Select(RuleInput.FromRow).ToList();
        VersionId = WorkingVersion?.Id ?? 0;

        if (IsSeniorManager)
        {
            PendingRequests = await _db.ReadinessScoringChangeRequests
                .AsNoTracking()
                .Include(request => request.RequestedByUser)
                .Where(request => request.CompanyId == currentUser.CompanyId && request.Status == "Pending")
                .OrderBy(request => request.CreatedAtUtc)
                .Select(request => ScoringRequestRow.FromRequest(request))
                .ToListAsync();
        }

        if (IsOperationalManager)
        {
            OwnRequests = await _db.ReadinessScoringChangeRequests
                .AsNoTracking()
                .Where(request => request.CompanyId == currentUser.CompanyId && request.RequestedByUserId == currentUser.Id)
                .OrderByDescending(request => request.CreatedAtUtc)
                .Take(12)
                .Select(request => ScoringRequestRow.FromRequest(request))
                .ToListAsync();
        }
    }

    private void ApplyConfirmationMessage()
    {
        StatusMessage = Confirmation switch
        {
            "rules-saved" => "Readiness scoring rules saved.",
            "rule-added" => "Readiness rule added.",
            "rule-deleted" => "Readiness rule deleted.",
            "rule-duplicated" => "Readiness rule duplicated.",
            "rules-autopopulated" => "Suggested rules added from registers.",
            "no-new-rules" => "No new register-based rules were found.",
            "engine-published" => "Readiness engine published for live use.",
            "request-sent" => "Scoring change request sent to senior management.",
            "request-approved" => "Scoring change request approved.",
            "request-rejected" => "Scoring change request rejected.",
            "request-deleted" => "Scoring change request deleted.",
            "request-needs-reason" => "Add a reason before sending the request.",
            "missing-draft" => "No editable draft engine was available.",
            _ => null
        };
    }

    private static void ApplyRuleInput(ReadinessEngineRule rule, RuleInput input, DateTime now)
    {
        rule.AssetType = Clean(input.AssetType, "Custom");
        rule.Section = Clean(input.Section, "General");
        rule.ItemName = Clean(input.ItemName, "Unnamed rule");
        rule.FieldKey = string.IsNullOrWhiteSpace(input.FieldKey) ? null : input.FieldKey.Trim();
        rule.TriggerValue = Clean(input.TriggerValue, "Any issue");
        rule.AppliesTo = Clean(input.AppliesTo, "All");
        rule.ReadinessScope = ReadinessRuleScope.Options.Contains(input.ReadinessScope)
            ? input.ReadinessScope
            : ReadinessRuleScope.ActiveShift;
        rule.TargetVehicleType = string.IsNullOrWhiteSpace(input.TargetVehicleType) ? null : input.TargetVehicleType.Trim();
        rule.DefaultImpactPercent = ClampPercent(input.DefaultImpactPercent);
        rule.Severity = ReadinessRuleSeverity.FromImpactPercent(rule.DefaultImpactPercent);
        rule.ManualImpactPercent = input.ManualImpactPercent.HasValue ? ClampPercent(input.ManualImpactPercent.Value) : null;
        rule.IsHardBlocker = input.IsHardBlocker || string.Equals(rule.Severity, ReadinessRuleSeverity.HardBlocker, StringComparison.OrdinalIgnoreCase);
        rule.ManagerAlert = input.ManagerAlert;
        rule.IsActive = input.IsActive;
        rule.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        rule.SortOrder = input.SortOrder <= 0 ? rule.SortOrder : input.SortOrder;
        rule.UpdatedAtUtc = now;
    }

    private static int ClampPercent(int value)
    {
        return Math.Max(0, Math.Min(100, value));
    }

    private static string Clean(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool CanUseReadinessEngine(AppUser user)
    {
        return IsOpsManager(user) || CurrentUserService.IsSeniorAccessRole(user.AppRole?.Name);
    }

    private static bool IsOpsManager(AppUser user)
    {
        return string.Equals(user.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class RuleInput
    {
        public int Id { get; set; }
        public string AssetType { get; set; } = "Vehicle";
        public string Section { get; set; } = "General";
        public string ItemName { get; set; } = string.Empty;
        public string? FieldKey { get; set; }
        public string TriggerValue { get; set; } = string.Empty;
        public string AppliesTo { get; set; } = "All";
        public string ReadinessScope { get; set; } = ReadinessRuleScope.ActiveShift;
        public string? TargetVehicleType { get; set; }
        public string Severity { get; set; } = ReadinessRuleSeverity.Moderate;
        public int DefaultImpactPercent { get; set; } = 10;
        public int? ManualImpactPercent { get; set; }
        public bool IsHardBlocker { get; set; }
        public bool ManagerAlert { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
        public int SortOrder { get; set; }

        public static RuleInput FromRow(ReadinessRuleRow row)
        {
            return new RuleInput
            {
                Id = row.Id,
                AssetType = row.AssetType,
                Section = row.Section,
                ItemName = row.ItemName,
                FieldKey = row.FieldKey,
                TriggerValue = row.TriggerValue,
                AppliesTo = row.AppliesTo,
                ReadinessScope = row.ReadinessScope,
                TargetVehicleType = row.TargetVehicleType,
                Severity = row.Severity,
                DefaultImpactPercent = row.DefaultImpactPercent,
                ManualImpactPercent = row.ManualImpactPercent,
                IsHardBlocker = row.IsHardBlocker,
                ManagerAlert = row.ManagerAlert,
                IsActive = row.IsActive,
                Notes = row.Notes,
                SortOrder = row.SortOrder
            };
        }
    }

    public sealed class ReadinessRuleRow
    {
        public int Id { get; set; }
        public string AssetType { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? FieldKey { get; set; }
        public string TriggerValue { get; set; } = string.Empty;
        public string AppliesTo { get; set; } = string.Empty;
        public string ReadinessScope { get; set; } = ReadinessRuleScope.ActiveShift;
        public string? TargetVehicleType { get; set; }
        public string Severity { get; set; } = string.Empty;
        public int DefaultImpactPercent { get; set; }
        public int? ManualImpactPercent { get; set; }
        public bool IsHardBlocker { get; set; }
        public bool ManagerAlert { get; set; }
        public bool IsActive { get; set; }
        public bool IsAutoPopulated { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int SortOrder { get; set; }
        public int EffectiveImpactPercent => ManualImpactPercent ?? DefaultImpactPercent;

        public static ReadinessRuleRow FromRule(ReadinessEngineRule rule)
        {
            return new ReadinessRuleRow
            {
                Id = rule.Id,
                AssetType = rule.AssetType,
                Section = rule.Section,
                ItemName = rule.ItemName,
                FieldKey = rule.FieldKey,
                TriggerValue = rule.TriggerValue,
                AppliesTo = rule.AppliesTo,
                ReadinessScope = string.IsNullOrWhiteSpace(rule.ReadinessScope) ? ReadinessRuleScope.ActiveShift : rule.ReadinessScope,
                TargetVehicleType = rule.TargetVehicleType,
                Severity = rule.Severity,
                DefaultImpactPercent = rule.DefaultImpactPercent,
                ManualImpactPercent = rule.ManualImpactPercent,
                IsHardBlocker = rule.IsHardBlocker,
                ManagerAlert = rule.ManagerAlert,
                IsActive = rule.IsActive,
                IsAutoPopulated = rule.IsAutoPopulated,
                SourceType = rule.SourceType,
                Notes = rule.Notes,
                SortOrder = rule.SortOrder
            };
        }
    }

    public sealed class ScoringRequestRow
    {
        public int Id { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RuleLabel { get; set; } = string.Empty;
        public string CurrentSetting { get; set; } = string.Empty;
        public string ProposedSetting { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? SeniorDecisionNote { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        public static ScoringRequestRow FromRequest(ReadinessScoringChangeRequest request)
        {
            return new ScoringRequestRow
            {
                Id = request.Id,
                RequestedBy = request.RequestedByUser?.FullName ?? "Operational manager",
                Status = request.Status,
                RuleLabel = $"{request.AssetType} - {request.ItemName} - {request.TriggerValue}",
                CurrentSetting = $"{request.CurrentSeverity ?? "Current"} / -{request.CurrentImpactPercent ?? 0}%",
                ProposedSetting = $"{request.ProposedSeverity} / -{request.ProposedImpactPercent ?? 0}% / {(request.ProposedActive == false ? "inactive" : "active")}",
                Reason = request.Reason,
                SeniorDecisionNote = request.SeniorDecisionNote,
                CreatedAtUtc = request.CreatedAtUtc
            };
        }
    }
}
