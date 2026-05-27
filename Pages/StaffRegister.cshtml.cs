using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class StaffRegisterModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public StaffRegisterModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }

    public List<StaffRegisterItem> StaffMembers { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var query = _db.AppUsers
            .Include(user => user.AppRole)
            .Where(user => user.CompanyId == currentUser.CompanyId);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(user =>
                user.FullName.Contains(search)
                || user.Email.Contains(search)
                || (user.AppRole != null && user.AppRole.Name.Contains(search)));
        }

        StaffMembers = await query
            .OrderBy(user => user.AppRole == null ? string.Empty : user.AppRole.Name)
            .ThenBy(user => user.FullName)
            .Select(user => new StaffRegisterItem
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = user.AppRole == null ? "Unassigned" : user.AppRole.Name,
                Status = user.Status,
                CreatedAtUtc = user.CreatedAtUtc,
                LastLoginAtUtc = user.LastLoginAtUtc
            })
            .ToListAsync();

        return Page();
    }

    public class StaffRegisterItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastLoginAtUtc { get; set; }
    }
}
