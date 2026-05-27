using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
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

    public List<MedicationRegisterItem> MedicationItems { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var query = _db.MedicationItems
            .Include(item => item.CreatedByUser)
            .Include(item => item.LastAllocatedByUser)
            .Where(item => item.CompanyId == currentUser.CompanyId);

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

        return Page();
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
