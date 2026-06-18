using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class MedicationRegisterModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public MedicationRegisterModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true, Name = "view")] public string? RequestedViewMode { get; set; }
    [BindProperty(SupportsGet = true)] public string GroupBy { get; set; } = "name";

    public List<MedicationRegisterItem> MedicationItems { get; private set; } = new();
    public List<MedicationRegisterGroup> MedicationGroups { get; private set; } = new();
    public string ViewMode { get; private set; } = "register";
    public bool IsRegisterView { get; private set; }
    public bool HasSearchTerm => !string.IsNullOrWhiteSpace(SearchTerm);
    public bool ShouldShowResults => IsRegisterView || HasSearchTerm;
    public string? StatusMessage { get; private set; }
    public string PageHeading => "Medication Register";
    public string PageSubtitle => "Open the full medication register grouped by medication name or current location. Use the search field to narrow results, then expand a group and select a row to view details.";
    public string CurrentReturnUrl => "/MedicationRegister?view=register&GroupBy=" + Uri.EscapeDataString(NormalizeGroupBy(GroupBy)) + (HasSearchTerm ? "&SearchTerm=" + Uri.EscapeDataString(SearchTerm!.Trim()) : string.Empty);

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        ViewMode = NormalizeViewMode(RequestedViewMode);
        IsRegisterView = ViewMode == "register";
        GroupBy = NormalizeGroupBy(GroupBy);
        StatusMessage = TempData["SuccessMessage"] as string;

        if (!ShouldShowResults)
        {
            return Page();
        }

        var query = _db.MedicationItems
            .Include(item => item.CreatedByUser)
            .Include(item => item.LastAllocatedByUser)
            .Where(item => item.CompanyId == currentUser.CompanyId && item.Status != "Deleted");

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(item =>
                item.Name.Contains(search)
                || (item.MedicationCode != null && item.MedicationCode.Contains(search))
                || (item.MedicationType != null && item.MedicationType.Contains(search))
                || (item.Schedule != null && item.Schedule.Contains(search))
                || (item.BatchNumber != null && item.BatchNumber.Contains(search))
                || (item.StorageLocation != null && item.StorageLocation.Contains(search))
                || (item.LastAllocationLocation != null && item.LastAllocationLocation.Contains(search))
                || (item.LastAllocatedByUser != null && item.LastAllocatedByUser.FullName.Contains(search)));
        }

        MedicationItems = await query
            .OrderBy(item => item.Name)
            .ThenBy(item => item.ExpiryDate)
            .Select(item => new MedicationRegisterItem
            {
                Id = item.Id,
                Name = item.Name,
                MedicationCode = item.MedicationCode,
                MedicationType = item.MedicationType,
                Schedule = item.Schedule,
                BatchNumber = item.BatchNumber,
                StorageLocation = item.StorageLocation,
                Status = item.Status,
                Quantity = item.Quantity,
                ExpiryDate = item.ExpiryDate,
                LastAllocationLocation = item.LastAllocationLocation,
                LastAllocatedAtUtc = item.LastAllocatedAtUtc,
                LastAllocatedByName = item.LastAllocatedByUser == null ? null : item.LastAllocatedByUser.FullName,
                CreatedByName = item.CreatedByUser == null ? "Manager" : item.CreatedByUser.FullName,
                CreatedAtUtc = item.CreatedAtUtc
            })
            .ToListAsync();

        MedicationGroups = MedicationItems
            .GroupBy(BuildGroupLabel)
            .OrderBy(group => group.Key)
            .Select(group => new MedicationRegisterGroup
            {
                Label = group.Key,
                Items = group
                    .OrderBy(item => item.Name)
                    .ThenBy(item => item.BatchNumber)
                    .ThenBy(item => item.ExpiryDate)
                    .ToList()
            })
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int medicationItemId, string? returnUrl)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var medicationItem = await _db.MedicationItems
            .FirstOrDefaultAsync(item =>
                item.Id == medicationItemId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (medicationItem is null)
        {
            TempData["SuccessMessage"] = "Medication item was not found.";
            return RedirectBack(returnUrl);
        }

        var now = DateTime.UtcNow;
        medicationItem.Status = "Deleted";
        medicationItem.UpdatedAtUtc = now;

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Medication item deleted",
            EntityType = "MedicationItem",
            EntityId = medicationItem.Id,
            Details = $"Medication item deleted from register: {medicationItem.Name}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Medication item deleted.";
        return RedirectBack(returnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/MedicationRegister", new { view = "register" });
    }

    private static string NormalizeViewMode(string? viewMode)
    {
        return "register";
    }

    private string BuildGroupLabel(MedicationRegisterItem item)
    {
        if (string.Equals(GroupBy, "location", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(item.StorageLocation)
                ? "No location assigned"
                : item.StorageLocation.Trim();
        }

        return string.IsNullOrWhiteSpace(item.Name)
            ? "Unnamed medication"
            : item.Name.Trim();
    }

    private static string NormalizeGroupBy(string? groupBy)
    {
        return string.Equals(groupBy, "location", StringComparison.OrdinalIgnoreCase)
            ? "location"
            : "name";
    }

    public class MedicationRegisterGroup
    {
        public string Label { get; set; } = string.Empty;
        public List<MedicationRegisterItem> Items { get; set; } = new();
    }

    public class MedicationRegisterItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? MedicationCode { get; set; }
        public string? MedicationType { get; set; }
        public string? Schedule { get; set; }
        public string? BatchNumber { get; set; }
        public string? StorageLocation { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? Quantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? LastAllocationLocation { get; set; }
        public DateTime? LastAllocatedAtUtc { get; set; }
        public string? LastAllocatedByName { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
