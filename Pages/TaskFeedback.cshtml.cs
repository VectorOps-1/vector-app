using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class TaskFeedbackModel : PageModel
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp",
        ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt"
    };

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public TaskFeedbackModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public int? TaskId { get; set; }
    [BindProperty] public string? Outcome { get; set; }
    [BindProperty] public string? RelatedItem { get; set; }
    [BindProperty] public string? FeedbackMessage { get; set; }
    [BindProperty] public List<IFormFile> EvidenceFiles { get; set; } = new();

    public TaskFeedbackDetails? TaskDetails { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadTaskAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        await LoadTaskAsync();

        if (!TaskId.HasValue || TaskDetails is null)
        {
            StatusMessage = "No open task was found for this feedback submission.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Outcome))
        {
            StatusMessage = "Select a task outcome before submitting feedback.";
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

        var task = await _db.TaskItems
            .FirstAsync(t => t.Id == TaskId.Value && t.AssignedToUserId == currentUser.Id && t.Status == "Open");
        var now = DateTime.UtcNow;

        task.Status = "Completed";
        task.CompletedAtUtc = now;

        var evidenceSummary = EvidenceFiles.Any()
            ? $" Evidence files attached: {string.Join(", ", EvidenceFiles.Select(f => f.FileName))}."
            : string.Empty;

        _db.TaskEvents.Add(new TaskEvent
        {
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

    private async Task LoadTaskAsync()
    {
        var currentUserId = _currentUser.CurrentUserId;
        if (!TaskId.HasValue || !currentUserId.HasValue)
        {
            return;
        }

        TaskDetails = await _db.TaskItems
            .Where(t => t.Id == TaskId.Value && t.AssignedToUserId == currentUserId.Value && t.Status == "Open")
            .Select(t => new TaskFeedbackDetails
            {
                Id = t.Id,
                ActionType = t.ActionType,
                InstructionMessage = t.InstructionMessage,
                ExpiresAtUtc = t.ExpiresAtUtc
            })
            .FirstOrDefaultAsync();
    }

    public class TaskFeedbackDetails
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? InstructionMessage { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
    }
}
