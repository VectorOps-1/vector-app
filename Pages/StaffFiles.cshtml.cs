using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class StaffFilesModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public StaffFilesModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public int? StaffUserId { get; set; }

    public List<StaffFileMember> StaffMembers { get; private set; } = new();
    public StaffFileMember? SelectedStaff { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        StaffMembers = await _db.AppUsers
            .Include(user => user.AppRole)
            .Where(user => user.CompanyId == currentUser.CompanyId)
            .OrderBy(user => user.FullName)
            .Select(user => new StaffFileMember
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleName = user.AppRole == null ? "Unassigned" : user.AppRole.Name,
                Status = user.Status
            })
            .ToListAsync();

        SelectedStaff = StaffUserId.HasValue
            ? StaffMembers.FirstOrDefault(staff => staff.Id == StaffUserId.Value)
            : null;

        return Page();
    }

    public class StaffFileMember
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
