using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class TaskFeedbackModel : PageModel
{
    public const string TaskFeedbackMode = "task";
    public const string GeneralFeedbackMode = "general";

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp",
        ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt"
    };

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public TaskFeedbackModel(VectorDbContext db, CurrentUserService currentUser, IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    [BindProperty(SupportsGet = true)] public int? TaskId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Mode { get; set; }
    [BindProperty] public string? FeedbackMode { get; set; }
    [BindProperty] public string? Outcome { get; set; }
    [BindProperty] public string? RelatedItem { get; set; }
    [BindProperty] public string? FeedbackMessage { get; set; }
    [BindProperty] public List<IFormFile> EvidenceFiles { get; set; } = new();

    public TaskFeedbackDetails? TaskDetails { get; private set; }
    public List<OpenTaskFeedbackOption> OpenTaskOptions { get; private set; } = new();
    public List<SelectListItem> TaskSelectOptions { get; private set; } = new();
    public string SelectedMode { get; private set; } = string.Empty;
    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? confirmation)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        SelectedMode = NormalizeMode(Mode, TaskId);
        await LoadOpenTasksAsync(currentUser.Id);
        await LoadTaskAsync(currentUser.Id);

        if (TaskId.HasValue && TaskDetails is null)
        {
            StatusMessage = "That task is no longer open for your signed-in profile.";
        }

        if (confirmation == "general-feedback-submitted")
        {
            StatusMessage = "General feedback submitted and recorded.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        SelectedMode = NormalizeMode(FeedbackMode ?? Mode, TaskId);
        await LoadOpenTasksAsync(currentUser.Id);
        await LoadTaskAsync(currentUser.Id);

        if (string.IsNullOrWhiteSpace(Outcome))
        {
            StatusMessage = SelectedMode == TaskFeedbackMode
                ? "Select a task outcome before submitting feedback."
                : "Select a feedback type before submitting.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(FeedbackMessage))
        {
            StatusMessage = "Enter feedback before submitting.";
            return Page();
        }

        var unsupportedFile = EvidenceFiles.FirstOrDefault(file => !AllowedExtensions.Contains(Path.GetExtension(file.FileName)));
        if (unsupportedFile is not null)
        {
            StatusMessage = $"Unsupported file type: {unsupportedFile.FileName}.";
            return Page();
        }

        if (SelectedMode == GeneralFeedbackMode)
        {
            return await SubmitGeneralFeedbackAsync(currentUser);
        }

        if (SelectedMode != TaskFeedbackMode || !TaskId.HasValue || TaskDetails is null)
        {
            StatusMessage = "Select a specific open task before submitting task feedback.";
            return Page();
        }

        return await SubmitTaskFeedbackAsync(currentUser);
    }

    private async Task<IActionResult> SubmitTaskFeedbackAsync(AppUser currentUser)
    {
        var task = await _db.TaskItems
            .FirstOrDefaultAsync(t => t.Id == TaskId!.Value && t.AssignedToUserId == currentUser.Id && t.Status == "Open");

        if (task is null)
        {
            return RedirectToPage("/TaskInbox", new { confirmation = "task-not-found" });
        }

        var now = DateTime.UtcNow;

        task.Status = "Completed";
        task.CompletedAtUtc = now;

        var savedEvidence = await SaveEvidenceFilesAsync(
            currentUser,
            linkedEntityType: "TaskItem",
            linkedEntityId: task.Id,
            category: "Task Feedback Evidence",
            notes: $"Outcome: {Outcome}. Related item: {RelatedItem ?? "None"}.",
            now);

        var evidenceSummary = savedEvidence.Any()
            ? $" Evidence files attached: {string.Join(", ", savedEvidence)}."
            : string.Empty;

        _db.TaskEvents.Add(new TaskEvent
        {
            CompanyId = currentUser.CompanyId,
            TaskItemId = task.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Completed",
            Notes = $"Outcome: {Outcome}. Related item: {RelatedItem ?? "None"}. Feedback: {FeedbackMessage}.{evidenceSummary}",
            CreatedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = task.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Task completed",
            EntityType = "TaskItem",
            EntityId = task.Id,
            Details = $"Task #{task.Id} completed with outcome: {Outcome}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        return RedirectToPage("/TaskInbox", new { confirmation = "feedback-submitted" });
    }

    private async Task<IActionResult> SubmitGeneralFeedbackAsync(AppUser currentUser)
    {
        var now = DateTime.UtcNow;
        var savedEvidence = await SaveEvidenceFilesAsync(
            currentUser,
            linkedEntityType: "AppUser",
            linkedEntityId: currentUser.Id,
            category: "General Feedback Evidence",
            notes: $"Feedback type: {Outcome}. Related item: {RelatedItem ?? "None"}.",
            now);

        var evidenceSummary = savedEvidence.Any()
            ? $" Evidence files attached: {string.Join(", ", savedEvidence)}."
            : string.Empty;

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "General feedback submitted",
            EntityType = "AppUser",
            EntityId = currentUser.Id,
            Details = $"Feedback type: {Outcome}. Related item: {RelatedItem ?? "None"}. Feedback: {FeedbackMessage}.{evidenceSummary}",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        return RedirectToPage(new { mode = GeneralFeedbackMode, confirmation = "general-feedback-submitted" });
    }

    private async Task<List<string>> SaveEvidenceFilesAsync(
        AppUser currentUser,
        string linkedEntityType,
        int linkedEntityId,
        string category,
        string notes,
        DateTime now)
    {
        var savedEvidence = new List<string>();

        foreach (var file in EvidenceFiles.Where(file => file.Length > 0))
        {
            var storedFile = await _fileStorage.SaveAsync(file, $"{linkedEntityType}-{linkedEntityId}");
            _db.AssetFiles.Add(new AssetFile
            {
                CompanyId = currentUser.CompanyId,
                UploadedByUserId = currentUser.Id,
                LinkedEntityType = linkedEntityType,
                LinkedEntityId = linkedEntityId,
                Category = category,
                OriginalFileName = storedFile.OriginalFileName,
                ContentType = storedFile.ContentType,
                StorageProvider = storedFile.ProviderName,
                StoragePath = storedFile.StoragePath,
                SizeBytes = storedFile.SizeBytes,
                Notes = notes,
                UploadedAtUtc = now
            });

            savedEvidence.Add(storedFile.OriginalFileName);
        }

        return savedEvidence;
    }

    private async Task LoadOpenTasksAsync(int currentUserId)
    {
        OpenTaskOptions = await _db.TaskItems
            .AsNoTracking()
            .Where(t => t.AssignedToUserId == currentUserId && t.Status == "Open")
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new OpenTaskFeedbackOption
            {
                Id = t.Id,
                Label = "#" + t.Id + " - " + t.ActionType,
                InstructionMessage = t.InstructionMessage,
                ExpiresAtUtc = t.ExpiresAtUtc
            })
            .ToListAsync();

        TaskSelectOptions =
        [
            new SelectListItem("Select an open task", string.Empty)
        ];

        TaskSelectOptions.AddRange(OpenTaskOptions.Select(task => new SelectListItem(
            task.SummaryLabel,
            task.Id.ToString(),
            TaskId == task.Id)));
    }

    private async Task LoadTaskAsync(int currentUserId)
    {
        if (!TaskId.HasValue)
        {
            return;
        }

        TaskDetails = await _db.TaskItems
            .Include(t => t.AssignedByUser)
            .Where(t => t.Id == TaskId.Value && t.AssignedToUserId == currentUserId && t.Status == "Open")
            .Select(t => new TaskFeedbackDetails
            {
                Id = t.Id,
                ActionType = t.ActionType,
                InstructionMessage = t.InstructionMessage,
                AssignedByName = t.AssignedByUser == null ? "Manager" : t.AssignedByUser.FullName,
                ExpiresAtUtc = t.ExpiresAtUtc
            })
            .FirstOrDefaultAsync();
    }

    private static string NormalizeMode(string? mode, int? taskId)
    {
        if (taskId.HasValue || string.Equals(mode, TaskFeedbackMode, StringComparison.OrdinalIgnoreCase))
        {
            return TaskFeedbackMode;
        }

        if (string.Equals(mode, GeneralFeedbackMode, StringComparison.OrdinalIgnoreCase))
        {
            return GeneralFeedbackMode;
        }

        return string.Empty;
    }

    public class OpenTaskFeedbackOption
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? InstructionMessage { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }

        public string SummaryLabel
        {
            get
            {
                var expiry = ExpiresAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "No expiry";
                return $"{Label} ({expiry})";
            }
        }
    }

    public class TaskFeedbackDetails
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? InstructionMessage { get; set; }
        public string AssignedByName { get; set; } = string.Empty;
        public DateTime? ExpiresAtUtc { get; set; }
    }
}
