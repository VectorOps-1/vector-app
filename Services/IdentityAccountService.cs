using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public sealed record IdentityAccountChange(
    bool LoginEnabled,
    string? TemporaryPassword,
    string? ConfirmTemporaryPassword);

public sealed record IdentityAccountChangeResult(
    bool Succeeded,
    ApplicationIdentityUser? Identity,
    IReadOnlyList<string> Errors,
    bool IdentityCreated = false,
    bool PasswordReset = false);

public sealed class IdentityAccountService
{
    private readonly VectorDbContext _db;
    private readonly UserManager<ApplicationIdentityUser> _userManager;

    public IdentityAccountService(
        VectorDbContext db,
        UserManager<ApplicationIdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IdentityAccountChangeResult> ConfigureAsync(
        AppUser actor,
        AppUser targetProfile,
        IdentityAccountChange change,
        CancellationToken cancellationToken = default)
    {
        if (actor.CompanyId != targetProfile.CompanyId)
        {
            return Failure("The selected staff profile does not belong to the current company.");
        }

        var identity = await _db.LoginIdentities
            .FirstOrDefaultAsync(item =>
                item.CompanyId == actor.CompanyId &&
                item.AppUserId == targetProfile.Id,
                cancellationToken);

        if (!change.LoginEnabled)
        {
            if (identity is null)
            {
                return new IdentityAccountChangeResult(true, null, Array.Empty<string>());
            }

            identity.IsLoginEnabled = false;
            identity.UpdatedAtUtc = DateTime.UtcNow;
            var disableResult = await _userManager.UpdateAsync(identity);
            if (!disableResult.Succeeded)
            {
                return Failure(disableResult.Errors.Select(error => error.Description));
            }

            var disabledStampResult = await _userManager.UpdateSecurityStampAsync(identity);
            if (!disabledStampResult.Succeeded)
            {
                return Failure(disabledStampResult.Errors.Select(error => error.Description));
            }

            return new IdentityAccountChangeResult(true, identity, Array.Empty<string>());
        }

        if (identity is null)
        {
            var passwordError = ValidateTemporaryPassword(change, required: true);
            if (passwordError is not null)
            {
                return Failure(passwordError);
            }

            identity = new ApplicationIdentityUser
            {
                Id = Guid.NewGuid().ToString("N"),
                CompanyId = targetProfile.CompanyId,
                AppUserId = targetProfile.Id,
                UserName = $"c{targetProfile.CompanyId}.profile{targetProfile.Id}",
                Email = targetProfile.Email.Trim(),
                EmailConfirmed = false,
                IsLoginEnabled = true,
                MustChangePassword = true,
                LockoutEnabled = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(identity, change.TemporaryPassword!);
            return createResult.Succeeded
                ? new IdentityAccountChangeResult(true, identity, Array.Empty<string>(), IdentityCreated: true)
                : Failure(createResult.Errors.Select(error => error.Description));
        }

        var passwordReset = !string.IsNullOrWhiteSpace(change.TemporaryPassword);
        if (passwordReset)
        {
            var passwordError = ValidateTemporaryPassword(change, required: false);
            if (passwordError is not null)
            {
                return Failure(passwordError);
            }
        }

        identity.Email = targetProfile.Email.Trim();
        identity.IsLoginEnabled = true;
        identity.LockoutEnabled = true;
        identity.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(identity);
        if (!updateResult.Succeeded)
        {
            return Failure(updateResult.Errors.Select(error => error.Description));
        }

        if (passwordReset)
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(identity);
            var resetResult = await _userManager.ResetPasswordAsync(identity, resetToken, change.TemporaryPassword!);
            if (!resetResult.Succeeded)
            {
                return Failure(resetResult.Errors.Select(error => error.Description));
            }

            identity.MustChangePassword = true;
            identity.UpdatedAtUtc = DateTime.UtcNow;
            var forceChangeResult = await _userManager.UpdateAsync(identity);
            if (!forceChangeResult.Succeeded)
            {
                return Failure(forceChangeResult.Errors.Select(error => error.Description));
            }
        }

        var stampResult = await _userManager.UpdateSecurityStampAsync(identity);
        if (!stampResult.Succeeded)
        {
            return Failure(stampResult.Errors.Select(error => error.Description));
        }

        return new IdentityAccountChangeResult(true, identity, Array.Empty<string>(), PasswordReset: passwordReset);
    }

    private static string? ValidateTemporaryPassword(IdentityAccountChange change, bool required)
    {
        if (string.IsNullOrWhiteSpace(change.TemporaryPassword))
        {
            return required ? "Enter a temporary password to activate login access." : null;
        }

        if (!string.Equals(change.TemporaryPassword, change.ConfirmTemporaryPassword, StringComparison.Ordinal))
        {
            return "The temporary password confirmation does not match.";
        }

        return null;
    }

    private static IdentityAccountChangeResult Failure(string error) =>
        new(false, null, new[] { error });

    private static IdentityAccountChangeResult Failure(IEnumerable<string> errors) =>
        new(false, null, errors.Where(error => !string.IsNullOrWhiteSpace(error)).ToList());
}
