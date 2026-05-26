using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Pages;

public class TaskFeedbackModel : PageModel
{
    private const int PrototypeCurrentUserId = 1;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp",
        ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt"
    };

    private readonly VectorDbContext _db;

    public TaskFeedbackModel(VectorDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)] public int? TaskId { get; set; }
    [BindProperty] public string? Outcome { get; set; }
    [BindProperty] public string? RelatedItem { get; set; }
    [BindProperty] public string? FeedbackMessage { get; set; }
    [BindProperty] public List<IFormFile> EvidenceFiles { get; set; } = new();

    public TaskFeedbackDetails? TaskDetails { get; private set; }
    public string? StatusMessage { get; private set; }
    public bool FeedbackSaved { get; private set; }

    public async Task OnGetAsync()
    {
        StatusMessage = TempData["StatusMessage"] as string;
        FeedbackSaved = TempData["FeedbackSaved"] as bool? == true;
        await LoadTaskAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
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

        var task = await _db.TaskItems.FirstAsync(t => t.Id == TaskId.Value);
        var now = DateTime.UtcNow;

        task.Status = "Completed";
        task.CompletedAtUtc = now;

        var evidenceSummary = EvidenceFiles.Any()
            ? $" Evidence files attached: {string.Join(", ", EvidenceFiles.Select(f => f.FileName))}."
            : string.Empty;

        _db.TaskEvents.Add(new TaskEvent
        {
            TaskItemId = task.Id,
            PerformedByUserId = PrototypeCurrentUserId,
            EventType = "Completed",
            Notes = $"Outcome: {Outcome}. Related item: {RelatedItem ?? "None"}. Feedback: {FeedbackMessage}.{evidenceSummary}",
            CreatedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = task.CompanyId,
            AppUserId = PrototypeCurrentUserId,
            Action = "Task completed",
            EntityType = "TaskItem",
            EntityId = task.Id,
            Details = $"Task #{task.Id} completed with outcome: {Outcome}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Feedback submitted and task marked complete.";
        TempData["FeedbackSaved"] = true;
        return RedirectToPage(new { taskId = TaskId.Value });
    }

    private async Task LoadTaskAsync()
    {
        if (!TaskId.HasValue)
        {
            return;
        }

        TaskDetails = await _db.TaskItems
            .Where(t => t.Id == TaskId.Value && t.AssignedToUserId == PrototypeCurrentUserId && t.Status == "Open")
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
