using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class DailyVehicleChecklistModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly VehicleSchematicAssignmentService _schematicAssignments;

    public DailyVehicleChecklistModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        VehicleSchematicAssignmentService schematicAssignments)
    {
        _db = db;
        _currentUser = currentUser;
        _schematicAssignments = schematicAssignments;
    }

    [BindProperty(SupportsGet = true)] public string Frequency { get; set; } = "daily";
    [BindProperty(SupportsGet = true)] public string? Callsign { get; set; }
    [BindProperty(SupportsGet = true)] public string? Registration { get; set; }
    [BindProperty] public string? VehicleType { get; set; }
    [BindProperty] public string? LicenseNumber { get; set; }
    [BindProperty] public DateTime? NextServiceDate { get; set; }
    [BindProperty] public string? SchematicMarkData { get; set; }
    [BindProperty] public string? ChecklistNotes { get; set; }
    [BindProperty] public Dictionary<string, string?> ChecklistResponses { get; set; } = new();
    [BindProperty] public bool SameAsPreviousShift { get; set; }
    [BindProperty] public int? PreviousVehicleReportId { get; set; }
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public bool AllowSameAsPreviousVehicleInspection { get; private set; } = true;
    public bool HasSelectedVehicle => SelectedVehicleOption is not null;
    public string DraftStorageKey { get; private set; } = "daily-vehicle-readiness:anonymous";
    public string FreshChecklistUrl => $"/DailyVehicleChecklist?frequency={Uri.EscapeDataString(NormalizeFrequency(Frequency))}";
    public string FrequencyLabel => NormalizeFrequency(Frequency) == "full-audit" ? "Full Audit" : "Daily Check";
    public string InspectionTitle => NormalizeFrequency(Frequency) == "full-audit" ? "Full Audit" : "Daily Vehicle & Equipment Check";
    public bool HasAssignedChecklist { get; private set; }
    public bool AssignedChecklistHasConfiguredContent { get; private set; }
    public string AssignedChecklistName { get; private set; } = string.Empty;
    public VehicleRegisterOption? SelectedVehicleOption { get; private set; }
    private const string NoUnitSchematicConfigured = "No unit schematic configured";

    public List<VehicleRegisterOption> VehicleRegisterOptions { get; private set; } = new();
    public List<LiveChecklistSection> AssignedChecklistSections { get; private set; } = new();
    private const string SectionNoteItemKind = "SectionNote";

    public async Task<IActionResult> OnGetAsync()
    {
        Frequency = NormalizeFrequency(Frequency);
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        ApplyUserDraftContext(currentUser);
        await LoadSameAsPreviousSettingAsync(currentUser.CompanyId);
        await LoadVehicleRegisterOptionsAsync(currentUser.CompanyId, currentUser.Id);
        ApplySelectedVehicleValues();
        await LoadAssignedChecklistSectionsAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Frequency = NormalizeFrequency(Frequency);
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        ApplyUserDraftContext(currentUser);
        await LoadSameAsPreviousSettingAsync(currentUser.CompanyId);
        await LoadVehicleRegisterOptionsAsync(currentUser.CompanyId, currentUser.Id);
        ApplySelectedVehicleValues();
        await LoadAssignedChecklistSectionsAsync(currentUser.CompanyId);

        if (string.IsNullOrWhiteSpace(Callsign) && string.IsNullOrWhiteSpace(Registration))
        {
            StatusMessage = "Enter a callsign or registration before saving.";
            return Page();
        }

        if (!HasAssignedChecklist)
        {
            StatusMessage = "No assigned checklist available for this vehicle. Ask management to publish a checklist for this vehicle type, area, or callsign.";
            return Page();
        }

        if (!AssignedChecklistHasConfiguredContent)
        {
            StatusMessage = "The assigned checklist has no configured fields. Build and publish checklist rows before this vehicle can be checked.";
            return Page();
        }

        if (FindMissingRequiredChecklistField() is { } missingField)
        {
            StatusMessage = $"Complete the required checklist field: {missingField}.";
            return Page();
        }

        if (SameAsPreviousShift && PreviousVehicleReportId is null)
        {
            StatusMessage = "No previous checklist from another profile is available for this vehicle.";
            return Page();
        }

        var vehicle = await FindSelectedRegisteredVehicleAsync(currentUser.CompanyId);
        if (vehicle is null)
        {
            StatusMessage = "This vehicle is not active in the vehicle register. Select a registered active vehicle before saving.";
            return Page();
        }

        var checklistTemplate = await ResolvePublishedTemplateForVehicleAsync(currentUser.CompanyId, vehicle);
        if (checklistTemplate is null)
        {
            StatusMessage = "No assigned checklist is published for this vehicle. Ask management to publish one from Checklist Management.";
            return Page();
        }

        var report = await SaveVehicleReadinessAsync(currentUser, vehicle, checklistTemplate);
        StatusMessage = $"{InspectionTitle} saved for {report.VehicleRegistrationNumber}. A fresh check is ready.";
        ActionSaved = true;
        return Page();
    }

    private void ApplyUserDraftContext(AppUser currentUser)
    {
        var accessView = CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView);
        DraftStorageKey = $"daily-vehicle-readiness:user-{currentUser.Id}:access-{accessView}:frequency-{Frequency}";
    }

    private async Task LoadSameAsPreviousSettingAsync(int companyId)
    {
        var setting = await _db.Companies
            .AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => company.AllowSameAsPreviousVehicleInspection)
            .FirstOrDefaultAsync();

        AllowSameAsPreviousVehicleInspection = setting;
    }

    private async Task LoadVehicleRegisterOptionsAsync(int companyId, int currentUserId)
    {
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle =>
                vehicle.CompanyId == companyId &&
                vehicle.Status != "Deleted")
            .ToListAsync();
        var vehiclesByRegistration = vehicles
            .GroupBy(vehicle => vehicle.RegistrationNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var vehicleIds = vehicles.Select(vehicle => vehicle.Id).ToList();
        var latestReports = vehicleIds.Count == 0
            ? new List<DailyVehicleReadinessReport>()
            : await _db.DailyVehicleReadinessReports
                .AsNoTracking()
                .Include(report => report.PerformedByUser)
                .Where(report =>
                    report.CompanyId == companyId &&
                    vehicleIds.Contains(report.VehicleId) &&
                    report.PerformedByUserId != currentUserId &&
                    report.WorkflowStatus != "Deleted")
                .OrderByDescending(report => report.InspectionDateUtc)
                .ToListAsync();
        var latestReportByVehicleId = latestReports
            .GroupBy(report => report.VehicleId)
            .ToDictionary(group => group.Key, group => group.First());
        var publishedTemplates = (await _db.ChecklistTemplates
            .AsNoTracking()
            .Include(template => template.PublishScopes)
            .Where(template =>
                template.CompanyId == companyId &&
                template.ChecklistType == "Vehicle" &&
                template.Status == "Published" &&
                template.IsPublished &&
                template.PublishScopes.Any(scope => scope.IsActive && scope.RetiredAtUtc == null))
            .ToListAsync())
            .Where(IsRegisterOwnedTemplate)
            .ToList();
        var publishedTemplateIds = publishedTemplates.Select(template => template.Id).ToList();
        var configuredTemplateIds = publishedTemplateIds.Count == 0
            ? new HashSet<int>()
            : (await (
                from item in _db.ChecklistItems.AsNoTracking()
                join section in _db.ChecklistSections.AsNoTracking()
                    on item.ChecklistSectionId equals section.Id
                join column in _db.ChecklistColumnDefinitions.AsNoTracking()
                    on item.Id equals column.ChecklistItemId into itemColumns
                from column in itemColumns.DefaultIfEmpty()
                where publishedTemplateIds.Contains(section.ChecklistTemplateId) &&
                      (column != null || item.ItemKind == SectionNoteItemKind)
                select section.ChecklistTemplateId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

        var baseOptions = new List<VehicleRegisterOption>();
        foreach (var vehicle in vehicles
            .OrderBy(vehicle => vehicle.RegistrationNumber)
            .ThenBy(vehicle => vehicle.Callsign))
        {
            var schematic = await _schematicAssignments.ResolveForVehicleAsync(companyId, vehicle);
            baseOptions.Add(new VehicleRegisterOption(
                vehicle.RegistrationNumber,
                vehicle.Callsign,
                VehicleTaxonomyService.DisplayClassification(vehicle),
                schematic?.DisplayName ?? NoUnitSchematicConfigured,
                schematic?.Key ?? "",
                vehicle.NextServiceDate?.ToString("yyyy-MM-dd") ?? "",
                vehicle.LicenseNumber ?? "",
                null,
                ""));
        }

        VehicleRegisterOptions = baseOptions
            .Select(option =>
            {
                vehiclesByRegistration.TryGetValue(option.Registration, out var registeredVehicle);
                var publishedTemplate = registeredVehicle is null
                    ? null
                    : SelectPublishedTemplateForVehicle(publishedTemplates, registeredVehicle);

                if (!vehiclesByRegistration.TryGetValue(option.Registration, out var vehicle) ||
                    !latestReportByVehicleId.TryGetValue(vehicle.Id, out var report))
                {
                    return option with
                    {
                        PublishedChecklistTemplateId = publishedTemplate?.Id,
                        PublishedChecklistTemplateName = ChecklistDisplayService.TemplateName(publishedTemplate?.Name),
                        PublishedChecklistHasConfiguredContent = publishedTemplate is not null &&
                            configuredTemplateIds.Contains(publishedTemplate.Id)
                    };
                }

                return option with
                {
                    Callsign = vehicle.Callsign,
                    VehicleType = VehicleTaxonomyService.DisplayClassification(vehicle),
                    LicenseNumber = vehicle.LicenseNumber ?? "",
                    NextServiceDate = (vehicle.NextServiceDate ?? option.NextServiceDateAsDate)?.ToString("yyyy-MM-dd") ?? option.NextServiceDate,
                    PreviousSourceReportId = report.Id,
                    PreviousSourcePerformer = report.PerformedByUser?.FullName ?? "another profile",
                    PublishedChecklistTemplateId = publishedTemplate?.Id,
                    PublishedChecklistTemplateName = ChecklistDisplayService.TemplateName(publishedTemplate?.Name),
                    PublishedChecklistHasConfiguredContent = publishedTemplate is not null &&
                        configuredTemplateIds.Contains(publishedTemplate.Id)
                };
            })
            .ToList();
    }

    private void ApplySelectedVehicleValues()
    {
        HasAssignedChecklist = false;
        AssignedChecklistHasConfiguredContent = false;
        AssignedChecklistName = string.Empty;

        if (string.IsNullOrWhiteSpace(Registration))
        {
            return;
        }

        var selectedVehicle = VehicleRegisterOptions.FirstOrDefault(option =>
            string.Equals(option.Registration, Registration, StringComparison.OrdinalIgnoreCase));
        if (selectedVehicle is null)
        {
            StatusMessage = "This vehicle is not active in the vehicle register. Select a registered active vehicle before a checklist can load.";
            return;
        }

        SelectedVehicleOption = selectedVehicle;
        Callsign = NormalizeOptional(Callsign) ?? selectedVehicle.Callsign;
        VehicleType = selectedVehicle.VehicleType;
        LicenseNumber = selectedVehicle.LicenseNumber;
        NextServiceDate = selectedVehicle.NextServiceDateAsDate;
        HasAssignedChecklist = selectedVehicle.PublishedChecklistTemplateId.HasValue;
        AssignedChecklistHasConfiguredContent = HasAssignedChecklist && selectedVehicle.PublishedChecklistHasConfiguredContent;
        AssignedChecklistName = selectedVehicle.PublishedChecklistTemplateName;
    }

    private async Task<DailyVehicleReadinessReport> SaveVehicleReadinessAsync(
        AppUser currentUser,
        Vehicle vehicle,
        ChecklistTemplate checklistTemplate)
    {
        var now = DateTime.UtcNow;
        var selectedVehicle = VehicleRegisterOptions.FirstOrDefault(option =>
            string.Equals(option.Registration, Registration, StringComparison.OrdinalIgnoreCase));

        var report = new DailyVehicleReadinessReport
        {
            CompanyId = currentUser.CompanyId,
            VehicleId = vehicle.Id,
            PerformedByUserId = currentUser.Id,
            InspectionDateUtc = now,
            ShiftStartedAtUtc = now,
            ShiftEndsAtUtc = now.AddHours(12),
            LastSavedAtUtc = now,
            WorkflowStatus = "Saved",
            LastSavedSection = "Vehicle",
            VehicleRegistrationNumber = vehicle.RegistrationNumber,
            CallsignAtCheck = NormalizeOptional(Callsign) ?? vehicle.Callsign,
            VehicleTypeAtCheck = NormalizeOptional(VehicleType) ?? VehicleTaxonomyService.DisplayClassification(vehicle),
            ChecklistTemplateId = checklistTemplate.Id,
            ChecklistTemplateVersion = checklistTemplate.Version,
            SchematicTypeAtCheck = selectedVehicle?.SchematicKey,
            VehicleNextServiceDateAtCheck = NextServiceDate ?? vehicle.NextServiceDate,
            SameAsPreviousShiftUsed = SameAsPreviousShift,
            VehicleSameAsPreviousShiftUsed = SameAsPreviousShift,
            VehicleSameAsPreviousSourceReportId = SameAsPreviousShift ? PreviousVehicleReportId : null,
            VehicleSameAsPreviousAppliedAtUtc = SameAsPreviousShift ? now : null,
            VehicleSameAsPreviousCopiedSummary = SameAsPreviousShift && PreviousVehicleReportId.HasValue
                ? $"Vehicle inspection values copied from readiness report #{PreviousVehicleReportId.Value} completed by another profile."
                : null,
            OperationalNotes = BuildChecklistResponseSummary(),
            SchematicNotes = BuildSchematicMarkSummary(),
            SchematicMarkData = NormalizeSchematicMarkData(SchematicMarkData),
            GeneralNotes = NormalizeOptional(ChecklistNotes),
            ReadinessStatus = ResolveDynamicReadinessStatus(),
            CriticalIssueCount = CountDynamicCriticalIssues(),
            WarningIssueCount = CountDynamicWarningIssues(),
            CreatedAtUtc = now,
            SubmittedAtUtc = now
        };

        _db.DailyVehicleReadinessReports.Add(report);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Vehicle readiness saved",
            EntityType = "DailyVehicleReadinessReport",
            EntityId = report.Id,
            Details = $"{currentUser.FullName} saved {InspectionTitle} for {report.VehicleRegistrationNumber} from {CurrentUserService.NormalizeAccessView(_currentUser.CurrentAccessView)} access.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
        return report;
    }

    private async Task<Vehicle?> FindSelectedRegisteredVehicleAsync(int companyId)
    {
        var registration = NormalizeOptional(Registration);
        if (registration is null)
        {
            return null;
        }

        return await _db.Vehicles.FirstOrDefaultAsync(item =>
            item.CompanyId == companyId &&
            item.Status != "Deleted" &&
            item.RegistrationNumber == registration);
    }

    private async Task LoadAssignedChecklistSectionsAsync(int companyId)
    {
        AssignedChecklistSections = new List<LiveChecklistSection>();
        if (string.IsNullOrWhiteSpace(Registration))
        {
            HasAssignedChecklist = false;
            AssignedChecklistHasConfiguredContent = false;
            AssignedChecklistName = string.Empty;
            return;
        }

        var selectedVehicle = VehicleRegisterOptions.FirstOrDefault(option =>
            string.Equals(option.Registration, Registration, StringComparison.OrdinalIgnoreCase));
        if (selectedVehicle?.PublishedChecklistTemplateId is not { } selectedTemplateId)
        {
            HasAssignedChecklist = false;
            AssignedChecklistHasConfiguredContent = false;
            AssignedChecklistName = string.Empty;
            return;
        }

        var vehicle = await FindSelectedRegisteredVehicleAsync(companyId);
        var liveTemplate = vehicle is null
            ? null
            : await ResolvePublishedTemplateForVehicleAsync(companyId, vehicle);

        if (liveTemplate is null || liveTemplate.Id != selectedTemplateId)
        {
            HasAssignedChecklist = false;
            AssignedChecklistHasConfiguredContent = false;
            AssignedChecklistName = string.Empty;
            return;
        }

        HasAssignedChecklist = true;
        AssignedChecklistName = ChecklistDisplayService.TemplateName(liveTemplate.Name);
        var templateId = liveTemplate.Id;

        var sections = await _db.ChecklistSections
            .AsNoTracking()
            .Where(section => section.ChecklistTemplateId == templateId)
            .OrderBy(section => section.DisplayOrder)
            .ThenBy(section => section.Id)
            .ToListAsync();
        var sectionIds = sections.Select(section => section.Id).ToList();
        if (sectionIds.Count == 0)
        {
            AssignedChecklistHasConfiguredContent = false;
            return;
        }

        var items = await _db.ChecklistItems
            .AsNoTracking()
            .Where(item => sectionIds.Contains(item.ChecklistSectionId))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Id)
            .ToListAsync();
        var itemIds = items.Select(item => item.Id).ToList();
        var columns = itemIds.Count == 0
            ? new List<ChecklistColumnDefinition>()
            : await _db.ChecklistColumnDefinitions
                .AsNoTracking()
                .Where(column => itemIds.Contains(column.ChecklistItemId))
                .OrderBy(column => column.SortOrder)
                .ThenBy(column => column.Id)
                .ToListAsync();

        AssignedChecklistSections = sections
            .Select(section => new LiveChecklistSection
            {
                Id = section.Id,
                Name = section.Name,
                Items = items
                    .Where(item => item.ChecklistSectionId == section.Id)
                    .Select(item => new LiveChecklistItem
                    {
                        Id = item.Id,
                        Prompt = item.Prompt,
                        ItemKind = item.ItemKind,
                        ResponseType = item.ResponseType,
                        ParentChecklistItemId = item.ParentChecklistItemId,
                        SchematicKey = item.Model,
                        IsSubItem = item.ParentChecklistItemId.HasValue,
                        Fields = BuildLiveChecklistFields(item, columns.Where(column => column.ChecklistItemId == item.Id).ToList())
                    })
                    .ToList()
            })
            .ToList();

        AssignedChecklistHasConfiguredContent = AssignedChecklistSections
            .Any(section => section.Items.Any(item => item.Fields.Count > 0));
    }

    private static List<LiveChecklistField> BuildLiveChecklistFields(
        ChecklistItem item,
        IReadOnlyList<ChecklistColumnDefinition> columns)
    {
        if (columns.Count > 0)
        {
            return columns.Select(column => new LiveChecklistField
            {
                ResponseKey = $"column-{column.Id}",
                Heading = string.IsNullOrWhiteSpace(column.Heading) ? item.Prompt : column.Heading,
                ResponseType = string.IsNullOrWhiteSpace(column.ResponseType) ? item.ResponseType : column.ResponseType,
                IsRequired = column.IsRequired,
                IsReadinessCritical = column.AffectsReadiness,
                DropdownOptionsJson = column.DropdownOptionsJson
            }).ToList();
        }

        if (!item.ItemKind.Equals(SectionNoteItemKind, StringComparison.OrdinalIgnoreCase))
        {
            return new List<LiveChecklistField>();
        }

        return new List<LiveChecklistField>
        {
            new()
            {
                ResponseKey = $"item-{item.Id}",
                Heading = item.Prompt,
                ResponseType = item.ResponseType,
                IsRequired = item.IsRequired,
                IsReadinessCritical = item.IsReadinessCritical
            }
        };
    }

    public IReadOnlyList<string> GetChecklistResponseOptions(LiveChecklistField field)
    {
        var options = ParseDropdownOptions(field.DropdownOptionsJson);
        if (options.Count > 0)
        {
            return EnsureNotApplicableFirst(options);
        }

        var responseType = field.ResponseType.Trim();
        if (responseType.Contains("yes", StringComparison.OrdinalIgnoreCase))
        {
            return ["N/A", "Yes", "No"];
        }

        if (responseType.Contains("pass", StringComparison.OrdinalIgnoreCase) ||
            responseType.Contains("operational", StringComparison.OrdinalIgnoreCase))
        {
            return ["N/A", "Pass", "Issue", "Fail"];
        }

        if (responseType.Contains("battery", StringComparison.OrdinalIgnoreCase))
        {
            return ["N/A", "Full", "3/4", "1/2", "1/4", "Empty", "Charging"];
        }

        return ["N/A", "Complete", "Issue", "Not complete"];
    }

    public VehicleSchematicDefinition? ResolveSchematicForItem(LiveChecklistItem item)
    {
        return VehicleSchematicLibrary.Find(SelectedVehicleOption?.SchematicKey ?? string.Empty);
    }

    private static List<string> ParseDropdownOptions(string? dropdownOptionsJson)
    {
        if (string.IsNullOrWhiteSpace(dropdownOptionsJson))
        {
            return new List<string>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(dropdownOptionsJson);
            return parsed?
                .Select(NormalizeOptional)
                .Where(option => option is not null)
                .Select(option => option!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }
        catch
        {
            return dropdownOptionsJson
                .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    private static IReadOnlyList<string> EnsureNotApplicableFirst(IReadOnlyList<string> options)
    {
        var withoutNa = options
            .Where(option => !string.Equals(option, "N/A", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new[] { "N/A" }.Concat(withoutNa).ToList();
    }

    private string? FindMissingRequiredChecklistField()
    {
        foreach (var field in AssignedChecklistSections
            .SelectMany(section => section.Items)
            .SelectMany(item => item.Fields))
        {
            if (!field.IsRequired)
            {
                continue;
            }

            if (!ChecklistResponses.TryGetValue(field.ResponseKey, out var response) ||
                string.IsNullOrWhiteSpace(response))
            {
                return field.Heading;
            }
        }

        return null;
    }

    private string? BuildChecklistResponseSummary()
    {
        var lines = new List<string>();
        foreach (var section in AssignedChecklistSections)
        {
            lines.Add($"[{section.Name}]");
            foreach (var item in section.Items)
            {
                foreach (var field in item.Fields)
                {
                    ChecklistResponses.TryGetValue(field.ResponseKey, out var response);
                    var label = string.Equals(field.Heading, item.Prompt, StringComparison.OrdinalIgnoreCase)
                        ? item.Prompt
                        : $"{item.Prompt} - {field.Heading}";
                    lines.Add($"{label}: {NormalizeOptional(response) ?? "N/A"}");
                }
            }
        }

        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private string ResolveDynamicReadinessStatus()
    {
        var responses = ChecklistResponses.Values
            .Select(NormalizeOptional)
            .Where(response => response is not null)
            .Select(response => response!)
            .ToList();

        if (responses.Any(IsCriticalResponse))
        {
            return "Not ready";
        }

        if (responses.Any(IsWarningResponse))
        {
            return "Operational with notes";
        }

        return "Operational";
    }

    private int CountDynamicCriticalIssues()
    {
        return ChecklistResponses.Values
            .Select(NormalizeOptional)
            .Count(response => response is not null && IsCriticalResponse(response));
    }

    private int CountDynamicWarningIssues()
    {
        return ChecklistResponses.Values
            .Select(NormalizeOptional)
            .Count(response => response is not null && IsWarningResponse(response));
    }

    private static bool IsCriticalResponse(string value)
    {
        return string.Equals(value, "Fail", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "No", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("not operational", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("not complete", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("out of service", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWarningResponse(string value)
    {
        return string.Equals(value, "Issue", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("notes", StringComparison.OrdinalIgnoreCase);
    }

    private string? BuildSchematicMarkSummary()
    {
        var values = new List<string>();
        if (CountSchematicMarks(SchematicMarkData) is var markCount && markCount > 0)
        {
            values.Add($"Schematic marks: {markCount} captured");
        }

        return values.Count == 0 ? null : string.Join("; ", values);
    }

    private static string? NormalizeSchematicMarkData(string? value)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null || normalized == "{}")
        {
            return null;
        }

        return normalized.Length <= 4000 ? normalized : normalized[..4000];
    }

    private static int CountSchematicMarks(string? markData)
    {
        if (string.IsNullOrWhiteSpace(markData))
        {
            return 0;
        }

        return markData.Split("\"points\"", StringSplitOptions.None).Length - 1;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ChecklistTemplate? SelectPublishedTemplateForVehicle(
        IReadOnlyList<ChecklistTemplate> templates,
        Vehicle vehicle)
    {
        if (templates.Count == 0)
        {
            return null;
        }

        var scopedTemplates = templates
            .Where(IsLivePublishedTemplate)
            .Select(template => new
            {
                Template = template,
                Rank = template.PublishScopes
                    .Where(scope => scope.IsActive && scope.RetiredAtUtc == null)
                    .Where(scope => ScopeAppliesToVehicle(scope, template, vehicle))
                    .Select(scope => ScopeRank(scope.ScopeType))
                    .DefaultIfEmpty(0)
                    .Max()
            })
            .Where(item => item.Rank > 0)
            .OrderByDescending(item => item.Rank)
            .ThenByDescending(item => item.Template.PublishedAtUtc)
            .Select(item => item.Template)
            .FirstOrDefault();

        return scopedTemplates;
    }

    private static bool IsLivePublishedTemplate(ChecklistTemplate template)
    {
        return IsRegisterOwnedTemplate(template) &&
            template.IsPublished &&
            string.Equals(template.Status, "Published", StringComparison.OrdinalIgnoreCase) &&
            template.PublishScopes.Any(scope => scope.IsActive && scope.RetiredAtUtc is null);
    }

    private static bool IsRegisterOwnedTemplate(ChecklistTemplate template)
    {
        var sourceType = NormalizeOptional(template.SourceType);
        return !string.Equals(sourceType, "Seed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(sourceType, "Default", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(sourceType, "Sample", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ScopeAppliesToVehicle(ChecklistPublishScope scope, ChecklistTemplate template, Vehicle vehicle)
    {
        return scope.ScopeType switch
        {
            "Vehicle" => scope.VehicleId == vehicle.Id && VehicleMatchesTemplateTarget(vehicle, template.TargetVehicleType),
            "OperationalArea" => scope.OperationalAreaId == vehicle.CurrentOperationalAreaId && VehicleMatchesTemplateTarget(vehicle, template.TargetVehicleType),
            "VehicleFunction" => VehicleFunctionMatchesTemplateTarget(vehicle, template.TargetVehicleType),
            "VehicleSubtype" => VehicleSubtypeMatchesTemplateTarget(vehicle, template.TargetVehicleType),
            "VehicleCategory" => VehicleMatchesTemplateTarget(vehicle, template.TargetVehicleType),
            "AllAreas" => VehicleMatchesTemplateTarget(vehicle, template.TargetVehicleType),
            _ => false
        };
    }

    private static int ScopeRank(string scopeType) => scopeType switch
    {
        "Vehicle" => 5,
        "OperationalArea" => 4,
        "VehicleSubtype" => 3,
        "VehicleFunction" => 2,
        "VehicleCategory" => 2,
        "AllAreas" => 1,
        _ => 0
    };

    private static bool VehicleMatchesTemplateTarget(Vehicle vehicle, string targetVehicleType)
    {
        if (string.Equals(targetVehicleType, "All Vehicles", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var function = NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        var subtype = NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);

        return string.Equals(function, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(subtype, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vehicle.VehicleType, targetVehicleType, StringComparison.OrdinalIgnoreCase);
    }

    private static bool VehicleFunctionMatchesTemplateTarget(Vehicle vehicle, string targetVehicleType)
    {
        var function = NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        return string.Equals(function, targetVehicleType, StringComparison.OrdinalIgnoreCase);
    }

    private static bool VehicleSubtypeMatchesTemplateTarget(Vehicle vehicle, string targetVehicleType)
    {
        var subtype = NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);
        return string.Equals(subtype, targetVehicleType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(vehicle.VehicleType, targetVehicleType, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ChecklistTemplate?> ResolvePublishedTemplateForVehicleAsync(int companyId, Vehicle vehicle)
    {
        var publishedTemplates = (await _db.ChecklistTemplates
            .AsNoTracking()
            .Include(template => template.PublishScopes)
            .Where(template =>
                template.CompanyId == companyId &&
                template.ChecklistType == "Vehicle" &&
                template.Status == "Published" &&
                template.IsPublished &&
                template.PublishScopes.Any(scope => scope.IsActive && scope.RetiredAtUtc == null))
            .ToListAsync())
            .Where(IsRegisterOwnedTemplate)
            .ToList();

        return SelectPublishedTemplateForVehicle(publishedTemplates, vehicle);
    }

    private static string NormalizeFrequency(string? frequency)
    {
        if (string.Equals(frequency, "full-audit", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(frequency, "full audit", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(frequency, "audit", StringComparison.OrdinalIgnoreCase))
        {
            return "full-audit";
        }

        return "daily";
    }

    private static string SchematicName(string key)
    {
        return VehicleSchematicLibrary.Find(key)?.DisplayName ?? NoUnitSchematicConfigured;
    }

    public sealed class LiveChecklistSection
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<LiveChecklistItem> Items { get; set; } = new();
    }

    public sealed class LiveChecklistItem
    {
        public int Id { get; set; }
        public int? ParentChecklistItemId { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string ItemKind { get; set; } = "Field";
        public string ResponseType { get; set; } = "Text";
        public string? SchematicKey { get; set; }
        public bool IsSubItem { get; set; }
        public bool IsSchematicBlock =>
            ItemKind.Equals("SchematicBlock", StringComparison.OrdinalIgnoreCase) ||
            ResponseType.Contains("Schematic Markup", StringComparison.OrdinalIgnoreCase);
        public bool IsSchematicView =>
            ItemKind.Equals("SchematicView", StringComparison.OrdinalIgnoreCase) ||
            ResponseType.Contains("Schematic View", StringComparison.OrdinalIgnoreCase);
        public List<LiveChecklistField> Fields { get; set; } = new();
    }

    public sealed class LiveChecklistField
    {
        public string ResponseKey { get; set; } = string.Empty;
        public string Heading { get; set; } = string.Empty;
        public string ResponseType { get; set; } = "Text";
        public bool IsRequired { get; set; }
        public bool IsReadinessCritical { get; set; }
        public string? DropdownOptionsJson { get; set; }
        public bool IsSchematicMarkup =>
            ResponseType.Contains("Schematic", StringComparison.OrdinalIgnoreCase);
    }

    public sealed record VehicleRegisterOption(
        string Registration,
        string Callsign,
        string VehicleType,
        string SchematicName,
        string SchematicKey,
        string NextServiceDate,
        string LicenseNumber,
        int? PreviousSourceReportId,
        string PreviousSourcePerformer,
        int? PublishedChecklistTemplateId = null,
        string PublishedChecklistTemplateName = "",
        bool PublishedChecklistHasConfiguredContent = false)
    {
        public DateTime? NextServiceDateAsDate =>
            DateTime.TryParse(NextServiceDate, out var date) ? date : null;
    }
}
