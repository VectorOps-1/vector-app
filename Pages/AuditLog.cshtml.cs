using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AuditLogModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public AuditLogModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Module { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ActivityDate { get; set; }

    public IReadOnlyList<SelectListItem> ModuleOptions { get; private set; } = [];
    public IReadOnlyList<AuditLogRow> Logs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        ModuleOptions = BuildModuleOptions();

        var query = _db.AuditLogs
            .AsNoTracking()
            .Include(log => log.AppUser)
            .Where(log => log.CompanyId == currentUser.CompanyId);

        if (!string.IsNullOrWhiteSpace(Module))
        {
            var module = Module.Trim();
            query = query.Where(log =>
                log.EntityType.Contains(module) ||
                log.Action.Contains(module) ||
                (log.Details != null && log.Details.Contains(module)));
        }

        if (ActivityDate.HasValue)
        {
            var dayStart = ActivityDate.Value.Date;
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(log => log.CreatedAtUtc >= dayStart && log.CreatedAtUtc < dayEnd);
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(log =>
                log.Action.Contains(search) ||
                log.EntityType.Contains(search) ||
                (log.Details != null && log.Details.Contains(search)) ||
                (log.AppUser != null && log.AppUser.FullName.Contains(search)) ||
                (log.EntityId.HasValue && log.EntityId.Value.ToString().Contains(search)));
        }

        Logs = await query
            .OrderByDescending(log => log.CreatedAtUtc)
            .Take(150)
            .Select(log => new AuditLogRow
            {
                Id = log.Id,
                CreatedAtUtc = log.CreatedAtUtc,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Details = log.Details,
                UserName = log.AppUser == null ? "System / unknown" : log.AppUser.FullName
            })
            .ToListAsync();

        return Page();
    }

    private static IReadOnlyList<SelectListItem> BuildModuleOptions()
    {
        return new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "All modules" },
            new() { Value = "Task", Text = "Tasks and feedback" },
            new() { Value = "Issue", Text = "Issue reports" },
            new() { Value = "Readiness", Text = "Readiness checks" },
            new() { Value = "ReadinessEngine", Text = "Readiness engine" },
            new() { Value = "ReadinessAlert", Text = "Readiness alerts" },
            new() { Value = "ReadinessScoring", Text = "Readiness scoring requests" },
            new() { Value = "Checklist", Text = "Checklists" },
            new() { Value = "ChecklistVariance", Text = "Checklist variance alerts" },
            new() { Value = "Vehicle", Text = "Vehicles" },
            new() { Value = "Equipment", Text = "Equipment" },
            new() { Value = "AssetMovement", Text = "Asset movements" },
            new() { Value = "Stock", Text = "Stock" },
            new() { Value = "StockOrder", Text = "Stock orders" },
            new() { Value = "Medication", Text = "Medication" },
            new() { Value = "Staff", Text = "Staff files" },
            new() { Value = "Document", Text = "Documents" },
            new() { Value = "Company", Text = "Company workspace" },
            new() { Value = "OperationalArea", Text = "Operational areas" },
            new() { Value = "ManagerArea", Text = "Manager areas" },
            new() { Value = "Access", Text = "Access control" },
            new() { Value = "Upload", Text = "Uploads" }
        };
    }

    public sealed class AuditLogRow
    {
        public int Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? Details { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}
