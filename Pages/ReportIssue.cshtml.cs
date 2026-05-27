using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ReportIssueModel : PageModel
{
    private static readonly string[] ManagerLevels =
    {
        "Operational Management",
        "Senior Management",
        "Company Owner"
    };

    private static readonly string[] NotificationMethods =
    {
        "In-app notification",
        "Email alert",
        "Text/SMS alert"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp",
        ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt"
    };

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;

    public ReportIssueModel(VectorDbContext db, CurrentUserService currentUser, LocationOptionService locationOptions)
    {
        _db = db;
        _currentUser = currentUser;
        _locationOptions = locationOptions;
    }

    [BindProperty(SupportsGet = true)] public string Module { get; set; } = "General";
    [BindProperty] public string? ManagerLevel { get; set; }
    [BindProperty] public int? AssignedToUserId { get; set; }
    [BindProperty] public string? NotificationMethod { get; set; }
    [BindProperty] public string? IssueType { get; set; }
    [BindProperty] public string? RelatedItem { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string? Severity { get; set; }
    [BindProperty] public string? OperationalStatus { get; set; }
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public List<IFormFile> EvidenceFiles { get; set; } = new();

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public List<ManagerRecipientOption> ManagerRecipients { get; private set; } = new();
    public List<SelectListItem> LocationOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string? module)
    {
        Module = NormaliseModule(module ?? Module);
        NotificationMethod = "In-app notification";
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        await LoadFormOptionsAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Module = NormaliseModule(Module);
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        await LoadFormOptionsAsync(currentUser.CompanyId);

        if (string.IsNullOrWhiteSpace(ManagerLevel) || !ManagerLevels.Contains(ManagerLevel))
        {
            StatusMessage = "Select the manager level before submitting.";
            return Page();
        }

        if (!AssignedToUserId.HasValue)
        {
            StatusMessage = "Select the individual manager who should receive this issue.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NotificationMethod) || !NotificationMethods.Contains(NotificationMethod))
        {
            StatusMessage = "Select how this issue should notify the manager.";
            return Page();
        }

        var assignedTo = await _db.AppUsers
            .Include(user => user.AppRole)
            .FirstOrDefaultAsync(user =>
                user.Id == AssignedToUserId.Value
                && user.CompanyId == currentUser.CompanyId
                && user.Status == "Active");

        if (assignedTo is null || !string.Equals(assignedTo.AppRole?.Name, ManagerLevel, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "The selected manager does not match the selected manager level.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(IssueType))
        {
            StatusMessage = "Select an issue type before submitting.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            StatusMessage = "Enter an issue description before submitting.";
            return Page();
        }

        var unsupportedFile = EvidenceFiles.FirstOrDefault(file => !AllowedExtensions.Contains(Path.GetExtension(file.FileName)));
        if (unsupportedFile is not null)
        {
            StatusMessage = $"Unsupported file type: {unsupportedFile.FileName}.";
            return Page();
        }

        var evidenceSummary = EvidenceFiles.Any()
            ? string.Join(", ", EvidenceFiles.Select(file => file.FileName))
            : null;

        var now = DateTime.UtcNow;
        var issue = new IssueReport
        {
            CompanyId = currentUser.CompanyId,
            ReportedByUserId = currentUser.Id,
            AssignedToUserId = assignedTo.Id,
            ManagerLevel = ManagerLevel,
            Module = Module,
            IssueType = IssueType.Trim(),
            RelatedItem = string.IsNullOrWhiteSpace(RelatedItem) ? null : RelatedItem.Trim(),
            Location = LocationOptionService.NormalizeSelectedLocation(Location),
            Severity = string.IsNullOrWhiteSpace(Severity) ? null : Severity.Trim(),
            OperationalStatus = string.IsNullOrWhiteSpace(OperationalStatus) ? null : OperationalStatus.Trim(),
            Description = Description.Trim(),
            NotificationMethod = NotificationMethod,
            EvidenceFileNames = evidenceSummary,
            Status = "Open",
            CreatedAtUtc = now
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.IssueReports.Add(issue);
        await _db.SaveChangesAsync();

        _db.IssueReportEvents.Add(new IssueReportEvent
        {
            IssueReportId = issue.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Submitted",
            Notes = $"Issue sent to {assignedTo.FullName} by {NotificationMethod}.",
            CreatedAtUtc = now
        });
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Issue submitted",
            EntityType = "IssueReport",
            EntityId = issue.Id,
            Details = $"{Module} issue submitted to {assignedTo.FullName}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        ActionSaved = true;
        StatusMessage = $"{Module} issue submitted to {assignedTo.FullName}. It is now in their issue inbox and the management pool.";
        return Page();
    }

    private async Task LoadFormOptionsAsync(int companyId)
    {
        await LoadManagerRecipientsAsync(companyId);
        LocationOptions = await _locationOptions.GetAssetLocationOptionsAsync(companyId);
    }

    private async Task LoadManagerRecipientsAsync(int companyId)
    {
        ManagerRecipients = await _db.AppUsers
            .Include(user => user.AppRole)
            .Where(user =>
                user.CompanyId == companyId
                && user.Status == "Active"
                && user.AppRole != null
                && ManagerLevels.Contains(user.AppRole.Name))
            .OrderBy(user => user.AppRole!.Name)
            .ThenBy(user => user.FullName)
            .Select(user => new ManagerRecipientOption
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                ManagerLevel = user.AppRole!.Name
            })
            .ToListAsync();
    }

    private static string NormaliseModule(string module)
    {
        return module.Trim().ToLowerInvariant() switch
        {
            "vehicle" or "vehicles" => "Vehicle",
            "equipment" => "Equipment",
            "stock" => "Stock",
            "medication" => "Medication",
            "staff" => "Staff",
            "checklist" => "Checklist",
            _ => "General"
        };
    }

    public class ManagerRecipientOption
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ManagerLevel { get; set; } = string.Empty;
    }
}
