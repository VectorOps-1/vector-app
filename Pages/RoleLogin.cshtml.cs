using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class RoleLoginModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly IWebHostEnvironment _environment;

    public RoleLoginModel(VectorDbContext db, CurrentUserService currentUser, IWebHostEnvironment environment)
    {
        _db = db;
        _currentUser = currentUser;
        _environment = environment;
    }

    public string AccessView { get; private set; } = "operational-management";
    public string RoleTitle { get; private set; } = "Operational Management Login";
    public string? LoginError { get; private set; }

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public void OnGet(string? access)
    {
        ViewData["ClientName"] = CompanyBranding.GetCompanyName(_environment);
        SetAccessView(access);
    }

    public async Task<IActionResult> OnPostAsync(string? access)
    {
        ViewData["ClientName"] = CompanyBranding.GetCompanyName(_environment);
        SetAccessView(access);

        if (string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError(nameof(Email), "Enter the user email for this access level.");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ModelState.AddModelError(nameof(Password), "Enter the prototype password.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var email = Email!.Trim();
        var user = await _db.AppUsers
            .Include(appUser => appUser.AppRole)
            .Include(appUser => appUser.Company)
            .FirstOrDefaultAsync(appUser => appUser.Email == email && appUser.Status == "Active");

        if (user is null || !CurrentUserService.AccessAllowsRole(AccessView, user.AppRole?.Name))
        {
            LoginError = "No active user with that email is authorised for this access level.";
            return Page();
        }

        var now = DateTime.UtcNow;
        user.LastLoginAtUtc = now;
        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = user.CompanyId,
            AppUserId = user.Id,
            Action = "User signed in",
            EntityType = "AppUser",
            EntityId = user.Id,
            Details = $"{user.FullName} signed in to {RoleTitle}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        _currentUser.SignIn(user, AccessView);

        return RedirectToPage("/Home");
    }

    private void SetAccessView(string? access)
    {
        AccessView = CurrentUserService.NormalizeAccessView(access);
        RoleTitle = AccessView switch
        {
            CurrentUserService.StaffAccess => "Staff Login",
            CurrentUserService.SeniorManagementAccess => "Senior Management Login",
            _ => "Operational Management Login"
        };
    }
}
