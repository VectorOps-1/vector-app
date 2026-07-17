using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public sealed class IdentityProvisioningManifest
{
    public int Version { get; init; } = 1;
    public List<IdentityProvisioningManifestEntry> Accounts { get; init; } = new();
}

public sealed class IdentityProvisioningManifestEntry
{
    public int CompanyId { get; init; }
    public string WorkspaceSlug { get; init; } = string.Empty;
    public int AppUserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string ExpectedRole { get; init; } = string.Empty;
    public string TemporaryPasswordEnvironmentVariable { get; init; } = string.Empty;
}

public sealed record IdentityProvisioningInventoryRow(
    int CompanyId,
    string WorkspaceSlug,
    int AppUserId,
    string FullName,
    string Email,
    string Role,
    string ProfileStatus,
    DateTime? LastLoginAtUtc,
    int SuccessfulLoginAuditCount,
    int SavedPermissionCount);

public sealed record IdentityProvisioningPlanRow(
    int CompanyId,
    string WorkspaceSlug,
    int AppUserId,
    string FullName,
    string Email,
    string Role,
    string Status);

public sealed record IdentityProvisioningResult(
    bool Succeeded,
    bool Executed,
    IReadOnlyList<IdentityProvisioningPlanRow> Accounts,
    IReadOnlyList<string> Errors);

public sealed class IdentityProvisioningService
{
    private const int ManifestVersion = 1;
    private const int MaximumAccountsPerRun = 100;
    private static readonly Regex EnvironmentVariablePattern = new(
        "^[A-Z][A-Z0-9_]{2,127}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly VectorDbContext _db;
    private readonly UserManager<ApplicationIdentityUser> _userManager;

    public IdentityProvisioningService(
        VectorDbContext db,
        UserManager<ApplicationIdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<IdentityProvisioningInventoryRow>> GetInventoryAsync(
        CancellationToken cancellationToken = default)
    {
        var profiles = await _db.AppUsers
            .AsNoTracking()
            .Include(user => user.Company)
            .Include(user => user.AppRole)
            .Where(user => user.Status != "Deleted")
            .OrderBy(user => user.CompanyId)
            .ThenBy(user => user.AppRole == null ? string.Empty : user.AppRole.Name)
            .ThenBy(user => user.FullName)
            .Select(user => new
            {
                user.CompanyId,
                WorkspaceSlug = user.Company == null ? string.Empty : user.Company.WorkspaceSlug,
                AppUserId = user.Id,
                user.FullName,
                user.Email,
                Role = user.AppRole == null ? "Unassigned" : user.AppRole.Name,
                ProfileStatus = user.Status,
                user.LastLoginAtUtc
            })
            .ToListAsync(cancellationToken);

        var userIds = profiles.Select(profile => profile.AppUserId).ToList();
        var loginCounts = await _db.AuditLogs
            .AsNoTracking()
            .Where(log =>
                log.AppUserId.HasValue &&
                userIds.Contains(log.AppUserId.Value) &&
                log.Action == "User signed in")
            .GroupBy(log => new { log.CompanyId, AppUserId = log.AppUserId!.Value })
            .Select(group => new { group.Key.CompanyId, group.Key.AppUserId, Count = group.Count() })
            .ToDictionaryAsync(
                row => (row.CompanyId, row.AppUserId),
                row => row.Count,
                cancellationToken);
        var permissionCounts = await _db.AppUserAccessPermissions
            .AsNoTracking()
            .Where(permission => userIds.Contains(permission.AppUserId))
            .GroupBy(permission => new { permission.CompanyId, permission.AppUserId })
            .Select(group => new { group.Key.CompanyId, group.Key.AppUserId, Count = group.Count() })
            .ToDictionaryAsync(
                row => (row.CompanyId, row.AppUserId),
                row => row.Count,
                cancellationToken);

        return profiles.Select(profile => new IdentityProvisioningInventoryRow(
            profile.CompanyId,
            profile.WorkspaceSlug ?? string.Empty,
            profile.AppUserId,
            profile.FullName,
            profile.Email,
            profile.Role,
            profile.ProfileStatus,
            profile.LastLoginAtUtc,
            loginCounts.GetValueOrDefault((profile.CompanyId, profile.AppUserId)),
            permissionCounts.GetValueOrDefault((profile.CompanyId, profile.AppUserId))))
            .ToList();
    }

    public async Task<IdentityProvisioningResult> ProvisionAsync(
        IdentityProvisioningManifest manifest,
        Func<string, string?> secretResolver,
        bool execute,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(manifest, secretResolver, cancellationToken);
        if (validation.Errors.Count > 0)
        {
            return new IdentityProvisioningResult(false, false, validation.Rows, validation.Errors);
        }

        if (!execute)
        {
            return new IdentityProvisioningResult(true, false, validation.Rows, Array.Empty<string>());
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var createdRows = new List<IdentityProvisioningPlanRow>();
            foreach (var item in validation.ValidatedAccounts)
            {
                if (item.ExistingIdentity is not null)
                {
                    createdRows.Add(item.Row with { Status = "Already provisioned; unchanged" });
                    continue;
                }

                var now = DateTime.UtcNow;
                var identity = item.ProspectiveIdentity;
                var createResult = await _userManager.CreateAsync(identity, item.TemporaryPassword);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _db.ChangeTracker.Clear();
                    return new IdentityProvisioningResult(
                        false,
                        false,
                        validation.Rows,
                        createResult.Errors.Select(error => error.Description).ToList());
                }

                _db.AuditLogs.Add(new AuditLog
                {
                    CompanyId = item.Profile.CompanyId,
                    AppUserId = item.Profile.Id,
                    Action = "Login identity provisioned",
                    EntityType = "ApplicationIdentityUser",
                    EntityId = item.Profile.Id,
                    Details = $"Login identity provisioned for {item.Profile.FullName}; temporary password replacement required at first sign-in.",
                    CreatedAtUtc = now
                });
                createdRows.Add(item.Row with { Status = "Provisioned" });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityProvisioningResult(true, true, createdRows, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            return new IdentityProvisioningResult(
                false,
                false,
                validation.Rows,
                new[] { $"Provisioning failed and was rolled back: {ex.GetType().Name}." });
        }
    }

    private async Task<ValidationState> ValidateAsync(
        IdentityProvisioningManifest manifest,
        Func<string, string?> secretResolver,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var rows = new List<IdentityProvisioningPlanRow>();
        var validatedAccounts = new List<ValidatedAccount>();

        if (manifest.Version != ManifestVersion)
        {
            errors.Add($"Manifest version {manifest.Version} is not supported. Expected version {ManifestVersion}.");
        }

        if (manifest.Accounts.Count == 0 || manifest.Accounts.Count > MaximumAccountsPerRun)
        {
            errors.Add($"Manifest must contain between 1 and {MaximumAccountsPerRun} accounts.");
        }

        var duplicateProfile = manifest.Accounts
            .GroupBy(entry => (entry.CompanyId, entry.AppUserId))
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateProfile is not null)
        {
            errors.Add($"Manifest repeats company {duplicateProfile.Key.CompanyId}, profile {duplicateProfile.Key.AppUserId}.");
        }

        foreach (var entry in manifest.Accounts)
        {
            var entryErrors = new List<string>();
            var passwordEnvironmentVariable = entry.TemporaryPasswordEnvironmentVariable ?? string.Empty;
            if (entry.CompanyId <= 0 || entry.AppUserId <= 0)
            {
                entryErrors.Add("CompanyId and AppUserId must be positive.");
            }

            if (string.IsNullOrWhiteSpace(entry.WorkspaceSlug) ||
                string.IsNullOrWhiteSpace(entry.Email) ||
                string.IsNullOrWhiteSpace(entry.ExpectedRole))
            {
                entryErrors.Add("WorkspaceSlug, Email, and ExpectedRole are required.");
            }

            if (!EnvironmentVariablePattern.IsMatch(passwordEnvironmentVariable))
            {
                entryErrors.Add("TemporaryPasswordEnvironmentVariable must be an uppercase environment-variable name.");
            }

            var profile = entryErrors.Count == 0
                ? await _db.AppUsers
                    .Include(user => user.Company)
                    .Include(user => user.AppRole)
                    .FirstOrDefaultAsync(user =>
                        user.CompanyId == entry.CompanyId &&
                        user.Id == entry.AppUserId,
                        cancellationToken)
                : null;

            if (profile is null)
            {
                entryErrors.Add("The exact tenant-owned staff profile was not found.");
            }
            else
            {
                if (!string.Equals(profile.Company?.WorkspaceSlug, entry.WorkspaceSlug, StringComparison.OrdinalIgnoreCase))
                {
                    entryErrors.Add("Workspace slug does not match the profile company.");
                }

                if (!string.Equals(profile.Email, entry.Email, StringComparison.OrdinalIgnoreCase))
                {
                    entryErrors.Add("Email does not match the staff profile.");
                }

                if (!string.Equals(profile.AppRole?.Name, entry.ExpectedRole, StringComparison.OrdinalIgnoreCase))
                {
                    entryErrors.Add("Expected role does not match the staff profile role.");
                }

                if (!string.Equals(profile.Status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    entryErrors.Add("Only an active staff profile may be provisioned.");
                }
            }

            var temporaryPassword = secretResolver(passwordEnvironmentVariable) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(temporaryPassword))
            {
                entryErrors.Add($"The required password environment variable {passwordEnvironmentVariable} is not set.");
            }

            ApplicationIdentityUser? existingIdentity = null;
            ApplicationIdentityUser? prospectiveIdentity = null;
            if (profile is not null && entryErrors.Count == 0)
            {
                existingIdentity = await _db.LoginIdentities
                    .FirstOrDefaultAsync(identity =>
                        identity.CompanyId == entry.CompanyId &&
                        identity.AppUserId == entry.AppUserId,
                        cancellationToken);

                if (existingIdentity is not null)
                {
                    if (!existingIdentity.IsLoginEnabled || existingIdentity.MustChangePassword == false)
                    {
                        entryErrors.Add("An existing identity has a different state and will not be changed by bootstrap provisioning.");
                    }
                }
                else
                {
                    prospectiveIdentity = CreateProspectiveIdentity(profile);
                    foreach (var validator in _userManager.PasswordValidators)
                    {
                        var passwordResult = await validator.ValidateAsync(_userManager, prospectiveIdentity, temporaryPassword);
                        if (!passwordResult.Succeeded)
                        {
                            entryErrors.AddRange(passwordResult.Errors.Select(error => error.Description));
                        }
                    }
                }
            }

            var displayName = profile?.FullName ?? "Unresolved profile";
            var role = profile?.AppRole?.Name ?? entry.ExpectedRole;
            var row = new IdentityProvisioningPlanRow(
                entry.CompanyId,
                entry.WorkspaceSlug,
                entry.AppUserId,
                displayName,
                entry.Email,
                role,
                existingIdentity is null ? "Planned" : "Already provisioned; unchanged");
            rows.Add(row);

            if (entryErrors.Count > 0)
            {
                errors.AddRange(entryErrors.Select(error =>
                    $"Company {entry.CompanyId}, profile {entry.AppUserId}: {error}"));
                continue;
            }

            validatedAccounts.Add(new ValidatedAccount(
                profile!,
                existingIdentity,
                prospectiveIdentity ?? existingIdentity!,
                temporaryPassword,
                row));
        }

        return new ValidationState(rows, validatedAccounts, errors.Distinct(StringComparer.Ordinal).ToList());
    }

    private static ApplicationIdentityUser CreateProspectiveIdentity(AppUser profile)
    {
        var now = DateTime.UtcNow;
        return new ApplicationIdentityUser
        {
            Id = Guid.NewGuid().ToString("N"),
            CompanyId = profile.CompanyId,
            AppUserId = profile.Id,
            UserName = $"c{profile.CompanyId}.profile{profile.Id}",
            Email = profile.Email.Trim(),
            EmailConfirmed = false,
            IsLoginEnabled = true,
            MustChangePassword = true,
            LockoutEnabled = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private sealed record ValidatedAccount(
        AppUser Profile,
        ApplicationIdentityUser? ExistingIdentity,
        ApplicationIdentityUser ProspectiveIdentity,
        string TemporaryPassword,
        IdentityProvisioningPlanRow Row);

    private sealed record ValidationState(
        IReadOnlyList<IdentityProvisioningPlanRow> Rows,
        IReadOnlyList<ValidatedAccount> ValidatedAccounts,
        IReadOnlyList<string> Errors);
}
