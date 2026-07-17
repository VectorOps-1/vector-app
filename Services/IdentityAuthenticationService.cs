using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public enum IdentityAuthenticationStatus
{
    Succeeded,
    InvalidCredentials,
    LoginNotConfigured,
    LoginDisabled,
    LockedOut,
    RoleNotAllowed
}

public sealed record IdentityAuthenticationResult(
    IdentityAuthenticationStatus Status,
    AppUser? Profile = null,
    ApplicationIdentityUser? Identity = null);

public sealed class IdentityAuthenticationService
{
    private readonly VectorDbContext _db;
    private readonly UserManager<ApplicationIdentityUser> _userManager;

    public IdentityAuthenticationService(
        VectorDbContext db,
        UserManager<ApplicationIdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IdentityAuthenticationResult> AuthenticateAsync(
        int companyId,
        string email,
        string password,
        string accessView,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var profile = await _db.AppUsers
            .Include(user => user.AppRole)
            .Include(user => user.Company)
            .Include(user => user.LoginIdentity)
            .FirstOrDefaultAsync(user =>
                user.CompanyId == companyId &&
                user.Status == "Active" &&
                user.Email.ToUpper() == normalizedEmail,
                cancellationToken);

        if (profile is null)
        {
            return new IdentityAuthenticationResult(IdentityAuthenticationStatus.InvalidCredentials);
        }

        if (!CurrentUserService.AccessAllowsRole(accessView, profile.AppRole?.Name))
        {
            return new IdentityAuthenticationResult(IdentityAuthenticationStatus.RoleNotAllowed);
        }

        var identity = profile.LoginIdentity;
        if (identity is null)
        {
            return new IdentityAuthenticationResult(IdentityAuthenticationStatus.LoginNotConfigured);
        }

        if (identity.CompanyId != companyId || identity.AppUserId != profile.Id || !identity.IsLoginEnabled)
        {
            return new IdentityAuthenticationResult(IdentityAuthenticationStatus.LoginDisabled);
        }

        if (await _userManager.IsLockedOutAsync(identity))
        {
            return new IdentityAuthenticationResult(IdentityAuthenticationStatus.LockedOut);
        }

        if (!await _userManager.CheckPasswordAsync(identity, password))
        {
            await _userManager.AccessFailedAsync(identity);
            var status = await _userManager.IsLockedOutAsync(identity)
                ? IdentityAuthenticationStatus.LockedOut
                : IdentityAuthenticationStatus.InvalidCredentials;
            return new IdentityAuthenticationResult(status);
        }

        await _userManager.ResetAccessFailedCountAsync(identity);
        return new IdentityAuthenticationResult(IdentityAuthenticationStatus.Succeeded, profile, identity);
    }
}
