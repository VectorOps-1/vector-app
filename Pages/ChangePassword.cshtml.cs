using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationIdentityUser> _userManager;
    private readonly SignInManager<ApplicationIdentityUser> _signInManager;
    private readonly CurrentUserService _currentUser;
    private readonly VectorDbContext _db;

    public ChangePasswordModel(
        UserManager<ApplicationIdentityUser> userManager,
        SignInManager<ApplicationIdentityUser> signInManager,
        CurrentUserService currentUser,
        VectorDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _currentUser = currentUser;
        _db = db;
    }

    [BindProperty]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmNewPassword { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        return await ResolveAccountAsync() is null
            ? RedirectToPage("/CompanyLogin")
            : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var account = await ResolveAccountAsync();
        if (account is null)
        {
            return RedirectToPage("/CompanyLogin");
        }

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            ModelState.AddModelError(nameof(CurrentPassword), "Enter your current password.");
        }

        if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(ConfirmNewPassword), "The new password confirmation does not match.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(account.Value.Identity, CurrentPassword, NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        account.Value.Identity.MustChangePassword = false;
        account.Value.Identity.UpdatedAtUtc = DateTime.UtcNow;
        var updateResult = await _userManager.UpdateAsync(account.Value.Identity);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = account.Value.Profile.CompanyId,
            AppUserId = account.Value.Profile.Id,
            Action = "Login password changed",
            EntityType = "ApplicationIdentityUser",
            EntityId = account.Value.Profile.Id,
            Details = $"{account.Value.Profile.FullName} completed the required password change.",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        await _signInManager.RefreshSignInAsync(account.Value.Identity);

        return RedirectToPage("/Home", new { confirmation = "password-changed" });
    }

    private async Task<(AppUser Profile, ApplicationIdentityUser Identity)?> ResolveAccountAsync()
    {
        var profile = await _currentUser.GetCurrentUserAsync();
        var identity = await _userManager.GetUserAsync(User);
        if (profile is null || identity is null ||
            identity.CompanyId != profile.CompanyId ||
            identity.AppUserId != profile.Id ||
            !identity.IsLoginEnabled)
        {
            return null;
        }

        return (profile, identity);
    }
}
