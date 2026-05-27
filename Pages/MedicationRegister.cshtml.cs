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
            .Where(item => item.CompanyId == currentUser.CompanyId);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(item =>
                item.Name.Contains(search)
                || (item.MedicationCode != null && item.MedicationCode.Contains(search))
                || (item.MedicationType != null && item.MedicationType.Contains(search))
                || (item.BatchNumber != null && item.BatchNumber.Contains(search))
                || (item.StorageLocation != null && item.StorageLocation.Contains(search)));
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
                BatchNumber = item.BatchNumber,
                StorageLocation = item.StorageLocation,
                Status = item.Status,
                Quantity = item.Quantity,
                ExpiryDate = item.ExpiryDate,
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
        public string? BatchNumber { get; set; }
        public string? StorageLocation { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? Quantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
