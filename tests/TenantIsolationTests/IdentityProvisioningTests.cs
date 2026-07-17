using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

internal static class IdentityProvisioningTests
{
    private const string PasswordEnvironmentVariable = "ACUITYOPS_TEST_TEMP_PASSWORD";
    private const string TemporaryPassword = "Provision!Pass123";

    public static async Task RunAllAsync()
    {
        await using var fixture = await ProvisioningFixture.CreateAsync();
        var service = fixture.Services.GetRequiredService<IdentityProvisioningService>();
        var command = fixture.Services.GetRequiredService<IdentityProvisioningCommand>();
        var userManager = fixture.Services.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var inventory = await service.GetInventoryAsync();
        var staffInventory = inventory.Single(row => row.AppUserId == fixture.StaffA.Id);
        Ensure(staffInventory.CompanyId == fixture.CompanyA.Id && staffInventory.SuccessfulLoginAuditCount == 1,
            "Read-only inventory did not retain tenant and historical login evidence.");
        Ensure(staffInventory.SavedPermissionCount == 1,
            "Read-only inventory did not retain the saved-permission indicator.");
        Ensure(!await fixture.Db.LoginIdentities.AnyAsync(),
            "Inventory created login identities.");

        var manifest = Manifest(fixture.CompanyA, fixture.StaffA, "Staff");
        var secretResolver = new Func<string, string?>(name =>
            name == PasswordEnvironmentVariable ? TemporaryPassword : null);
        var auditCountBefore = await fixture.Db.AuditLogs.CountAsync();
        var dryRun = await service.ProvisionAsync(manifest, secretResolver, execute: false);
        Ensure(dryRun.Succeeded && !dryRun.Executed,
            "A valid provisioning dry-run did not pass.");
        Ensure(!await fixture.Db.LoginIdentities.AnyAsync(),
            "Dry-run created a login identity.");
        Ensure(await fixture.Db.AuditLogs.CountAsync() == auditCountBefore,
            "Dry-run created an audit record.");

        var wrongRoleManifest = Manifest(fixture.CompanyA, fixture.StaffA, "Senior Management");
        var wrongRole = await service.ProvisionAsync(wrongRoleManifest, secretResolver, execute: false);
        Ensure(!wrongRole.Succeeded && wrongRole.Errors.Any(error => error.Contains("role", StringComparison.OrdinalIgnoreCase)),
            "Provisioning accepted an unexpected role.");

        var provisioned = await service.ProvisionAsync(manifest, secretResolver, execute: true);
        Ensure(provisioned.Succeeded && provisioned.Executed,
            "Approved provisioning did not execute.");
        var identity = await fixture.Db.LoginIdentities.SingleAsync(item => item.AppUserId == fixture.StaffA.Id);
        Ensure(identity.CompanyId == fixture.CompanyA.Id && identity.IsLoginEnabled && identity.MustChangePassword,
            "Provisioned identity does not enforce tenant, login, and first-password-change state.");
        Ensure(!string.Equals(identity.PasswordHash, TemporaryPassword, StringComparison.Ordinal) &&
               await userManager.CheckPasswordAsync(identity, TemporaryPassword),
            "Provisioned password was not stored and verified through ASP.NET Core Identity.");
        Ensure(!await fixture.Db.LoginIdentities.AnyAsync(item => item.AppUserId == fixture.ImportedProfile.Id),
            "An unlisted imported profile received a login identity.");
        var provisioningAudit = await fixture.Db.AuditLogs.SingleAsync(log => log.Action == "Login identity provisioned");
        Ensure(provisioningAudit.CompanyId == fixture.CompanyA.Id &&
               !provisioningAudit.Details!.Contains(TemporaryPassword, StringComparison.Ordinal),
            "Provisioning audit evidence leaked the temporary password.");

        var passwordHash = identity.PasswordHash;
        var replay = await service.ProvisionAsync(manifest, secretResolver, execute: true);
        Ensure(replay.Succeeded && await fixture.Db.LoginIdentities.CountAsync() == 1,
            "Replay-safe provisioning created a duplicate identity.");
        Ensure((await fixture.Db.LoginIdentities.SingleAsync()).PasswordHash == passwordHash,
            "Replay-safe provisioning reset an existing password.");

        await VerifyCommandSafeguardsAsync(fixture, command);
    }

    private static async Task VerifyCommandSafeguardsAsync(
        ProvisioningFixture fixture,
        IdentityProvisioningCommand command)
    {
        var manifest = Manifest(fixture.CompanyB, fixture.StaffB, "Staff");
        var path = Path.Combine(Path.GetTempPath(), $"acuityops-identity-manifest-{Guid.NewGuid():N}.json");
        var previousSecret = Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await File.WriteAllBytesAsync(path, bytes);
            Environment.SetEnvironmentVariable(PasswordEnvironmentVariable, TemporaryPassword);

            var blockedOutput = new StringWriter();
            var blockedError = new StringWriter();
            var blockedExit = await command.RunAsync(
                new[] { IdentityProvisioningCommand.CommandName, "--manifest", path, "--execute" },
                blockedOutput,
                blockedError);
            Ensure(blockedExit == 2 && !await fixture.Db.LoginIdentities.AnyAsync(item => item.AppUserId == fixture.StaffB.Id),
                "Execute mode bypassed manifest hash confirmation.");

            var dryRunOutput = new StringWriter();
            var dryRunError = new StringWriter();
            var dryRunExit = await command.RunAsync(
                new[] { IdentityProvisioningCommand.CommandName, "--manifest", path },
                dryRunOutput,
                dryRunError);
            Ensure(dryRunExit == 0 && !await fixture.Db.LoginIdentities.AnyAsync(item => item.AppUserId == fixture.StaffB.Id),
                "Command dry-run wrote identity data.");
            Ensure(!dryRunOutput.ToString().Contains(TemporaryPassword, StringComparison.Ordinal) &&
                   !dryRunError.ToString().Contains(TemporaryPassword, StringComparison.Ordinal),
                "Command output leaked the temporary password.");

            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var executeOutput = new StringWriter();
            var executeError = new StringWriter();
            var executeExit = await command.RunAsync(
                new[]
                {
                    IdentityProvisioningCommand.CommandName,
                    "--manifest", path,
                    "--execute",
                    "--confirm-sha256", hash
                },
                executeOutput,
                executeError);
            Ensure(executeExit == 0 && await fixture.Db.LoginIdentities.AnyAsync(item => item.AppUserId == fixture.StaffB.Id),
                "Hash-confirmed command execution did not provision the approved identity.");
            Ensure(!executeOutput.ToString().Contains(TemporaryPassword, StringComparison.Ordinal) &&
                   !executeError.ToString().Contains(TemporaryPassword, StringComparison.Ordinal),
                "Execute output leaked the temporary password.");
        }
        finally
        {
            Environment.SetEnvironmentVariable(PasswordEnvironmentVariable, previousSecret);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static IdentityProvisioningManifest Manifest(Company company, AppUser profile, string role) => new()
    {
        Version = 1,
        Accounts = new List<IdentityProvisioningManifestEntry>
        {
            new()
            {
                CompanyId = company.Id,
                WorkspaceSlug = company.WorkspaceSlug!,
                AppUserId = profile.Id,
                Email = profile.Email,
                ExpectedRole = role,
                TemporaryPasswordEnvironmentVariable = PasswordEnvironmentVariable
            }
        }
    };

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class ProvisioningFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;
        private readonly AsyncServiceScope _scope;

        private ProvisioningFixture(
            SqliteConnection connection,
            ServiceProvider provider,
            AsyncServiceScope scope,
            VectorDbContext db,
            Company companyA,
            Company companyB,
            AppUser staffA,
            AppUser staffB,
            AppUser importedProfile)
        {
            _connection = connection;
            _provider = provider;
            _scope = scope;
            Db = db;
            CompanyA = companyA;
            CompanyB = companyB;
            StaffA = staffA;
            StaffB = staffB;
            ImportedProfile = importedProfile;
        }

        public IServiceProvider Services => _scope.ServiceProvider;
        public VectorDbContext Db { get; }
        public Company CompanyA { get; }
        public Company CompanyB { get; }
        public AppUser StaffA { get; }
        public AppUser StaffB { get; }
        public AppUser ImportedProfile { get; }

        public static async Task<ProvisioningFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            services.AddDbContext<VectorDbContext>(options => options.UseSqlite(connection));
            services
                .AddIdentityCore<ApplicationIdentityUser>(options =>
                {
                    options.Password.RequiredLength = 12;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireDigit = true;
                    options.Password.RequireNonAlphanumeric = true;
                })
                .AddSignInManager()
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<VectorDbContext>();
            services.AddScoped<IdentityProvisioningService>();
            services.AddScoped<IdentityProvisioningCommand>();

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
            await db.Database.EnsureCreatedAsync();

            var seniorRole = new AppRole { Name = "Senior Management" };
            var staffRole = new AppRole { Name = "Staff" };
            var companyA = new Company { Name = "Provision A", WorkspaceSlug = "provision-a", Status = "Active" };
            var companyB = new Company { Name = "Provision B", WorkspaceSlug = "provision-b", Status = "Active" };
            db.AddRange(seniorRole, staffRole, companyA, companyB);
            await db.SaveChangesAsync();

            var seniorA = Profile(companyA.Id, seniorRole.Id, "Senior A", "senior-a@example.test");
            var staffA = Profile(companyA.Id, staffRole.Id, "Staff A", "staff-a@example.test");
            var staffB = Profile(companyB.Id, staffRole.Id, "Staff B", "staff-b@example.test");
            var imported = Profile(companyA.Id, staffRole.Id, "Imported Profile", "imported@example.test");
            db.AddRange(seniorA, staffA, staffB, imported);
            await db.SaveChangesAsync();

            db.AuditLogs.Add(new AuditLog
            {
                CompanyId = companyA.Id,
                AppUserId = staffA.Id,
                Action = "User signed in",
                EntityType = "AppUser",
                EntityId = staffA.Id,
                Details = "Historic login evidence",
                CreatedAtUtc = DateTime.UtcNow
            });
            db.AppUserAccessPermissions.Add(new AppUserAccessPermission
            {
                CompanyId = companyA.Id,
                AppUserId = staffA.Id,
                PermissionKey = "DailyChecks.Complete",
                Status = "Allowed",
                UpdatedByUserId = seniorA.Id,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            return new ProvisioningFixture(
                connection,
                provider,
                scope,
                db,
                companyA,
                companyB,
                staffA,
                staffB,
                imported);
        }

        public async ValueTask DisposeAsync()
        {
            await _scope.DisposeAsync();
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private static AppUser Profile(int companyId, int roleId, string name, string email) => new()
        {
            CompanyId = companyId,
            AppRoleId = roleId,
            FullName = name,
            Email = email,
            Status = "Active"
        };
    }
}
