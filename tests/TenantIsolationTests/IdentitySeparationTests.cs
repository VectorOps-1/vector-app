using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

internal static class IdentitySeparationTests
{
    private const string InitialPassword = "Initial!Pass123";
    private const string ResetPassword = "Replacement!Pass456";

    public static async Task RunAllAsync()
    {
        await using var fixture = await IdentityFixture.CreateAsync();
        var authentication = fixture.Services.GetRequiredService<IdentityAuthenticationService>();
        var accounts = fixture.Services.GetRequiredService<IdentityAccountService>();
        var userManager = fixture.Services.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var noIdentityResult = await authentication.AuthenticateAsync(
            fixture.CompanyA.Id,
            fixture.StaffA.Email,
            InitialPassword,
            CurrentUserService.StaffAccess);
        Ensure(noIdentityResult.Status == IdentityAuthenticationStatus.LoginNotConfigured,
            "An active staff profile without an Identity record was allowed to authenticate.");
        Ensure(!await fixture.Db.LoginIdentities.AnyAsync(identity => identity.AppUserId == fixture.ImportedProfile.Id),
            "An imported staff profile received login identity by default.");

        var crossTenantResult = await accounts.ConfigureAsync(
            fixture.SeniorB,
            fixture.StaffA,
            new IdentityAccountChange(true, InitialPassword, InitialPassword));
        Ensure(!crossTenantResult.Succeeded,
            "A manager activated login access for a profile owned by another tenant.");

        var activation = await accounts.ConfigureAsync(
            fixture.SeniorA,
            fixture.StaffA,
            new IdentityAccountChange(true, InitialPassword, InitialPassword));
        Ensure(activation.Succeeded && activation.IdentityCreated && activation.Identity is not null,
            "Explicit Access Setup activation did not create the login identity.");
        var activatedIdentity = activation.Identity!;
        Ensure(activatedIdentity.MustChangePassword,
            "A newly activated login was not forced to change its temporary password.");
        Ensure(!string.Equals(activatedIdentity.PasswordHash, InitialPassword, StringComparison.Ordinal),
            "The password was stored as plaintext.");
        Ensure(await userManager.CheckPasswordAsync(activatedIdentity, InitialPassword),
            "The Identity password hash could not verify the configured password.");
        var initialSecurityStamp = activatedIdentity.SecurityStamp;

        var authenticated = await authentication.AuthenticateAsync(
            fixture.CompanyA.Id,
            fixture.StaffA.Email,
            InitialPassword,
            CurrentUserService.StaffAccess);
        Ensure(authenticated.Status == IdentityAuthenticationStatus.Succeeded &&
               authenticated.Profile?.Id == fixture.StaffA.Id,
            "Valid tenant-scoped credentials did not authenticate the linked profile.");

        var wrongRole = await authentication.AuthenticateAsync(
            fixture.CompanyA.Id,
            fixture.StaffA.Email,
            InitialPassword,
            CurrentUserService.SeniorManagementAccess);
        Ensure(wrongRole.Status == IdentityAuthenticationStatus.RoleNotAllowed,
            "A valid credential bypassed the selected access-level boundary.");

        var reset = await accounts.ConfigureAsync(
            fixture.SeniorA,
            fixture.StaffA,
            new IdentityAccountChange(true, ResetPassword, ResetPassword));
        Ensure(reset.Succeeded && reset.PasswordReset,
            "Access Setup could not reset an existing login password.");
        Ensure(!string.Equals(initialSecurityStamp, reset.Identity?.SecurityStamp, StringComparison.Ordinal),
            "Resetting a password did not rotate the Identity security stamp.");
        Ensure((await authentication.AuthenticateAsync(
            fixture.CompanyA.Id,
            fixture.StaffA.Email,
            InitialPassword,
            CurrentUserService.StaffAccess)).Status == IdentityAuthenticationStatus.InvalidCredentials,
            "The old password remained valid after a password reset.");
        Ensure((await authentication.AuthenticateAsync(
            fixture.CompanyA.Id,
            fixture.StaffA.Email,
            ResetPassword,
            CurrentUserService.StaffAccess)).Status == IdentityAuthenticationStatus.Succeeded,
            "The reset password did not authenticate.");

        var tenantBActivation = await accounts.ConfigureAsync(
            fixture.SeniorB,
            fixture.StaffB,
            new IdentityAccountChange(true, InitialPassword, InitialPassword));
        Ensure(tenantBActivation.Succeeded,
            "A second tenant could not configure its own profile with the same email address.");
        var tenantBAuthenticated = await authentication.AuthenticateAsync(
            fixture.CompanyB.Id,
            fixture.StaffB.Email,
            InitialPassword,
            CurrentUserService.StaffAccess);
        Ensure(tenantBAuthenticated.Status == IdentityAuthenticationStatus.Succeeded &&
               tenantBAuthenticated.Profile?.CompanyId == fixture.CompanyB.Id,
            "Shared email identifiers crossed tenant boundaries.");

        var disabled = await accounts.ConfigureAsync(
            fixture.SeniorB,
            fixture.StaffB,
            new IdentityAccountChange(false, null, null));
        Ensure(disabled.Succeeded, "Access Setup could not disable login access.");
        Ensure(!string.IsNullOrWhiteSpace(disabled.Identity?.SecurityStamp),
            "Disabling login access did not retain a revocation security stamp.");
        Ensure((await authentication.AuthenticateAsync(
            fixture.CompanyB.Id,
            fixture.StaffB.Email,
            InitialPassword,
            CurrentUserService.StaffAccess)).Status == IdentityAuthenticationStatus.LoginDisabled,
            "A disabled login identity remained usable.");

        IdentityAuthenticationResult lockoutResult = new(IdentityAuthenticationStatus.InvalidCredentials);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            lockoutResult = await authentication.AuthenticateAsync(
                fixture.CompanyA.Id,
                fixture.StaffA.Email,
                "Wrong!Password999",
                CurrentUserService.StaffAccess);
        }

        Ensure(lockoutResult.Status == IdentityAuthenticationStatus.LockedOut,
            "Repeated failed passwords did not lock the login identity.");
        Ensure((await authentication.AuthenticateAsync(
            fixture.CompanyA.Id,
            fixture.StaffA.Email,
            ResetPassword,
            CurrentUserService.StaffAccess)).Status == IdentityAuthenticationStatus.LockedOut,
            "A correct password bypassed an active lockout.");

        await VerifyAdditiveMigrationAsync();
    }

    private static async Task VerifyAdditiveMigrationAsync()
    {
        const string migrationId = "20260713213000_AddStaffLoginIdentity";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await ExecuteAsync(connection, "CREATE TABLE Companies (Id INTEGER NOT NULL PRIMARY KEY);");
        await ExecuteAsync(connection, "CREATE TABLE AppUsers (Id INTEGER NOT NULL PRIMARY KEY);");
        await ExecuteAsync(connection,
            "CREATE TABLE __EFMigrationsHistory (MigrationId TEXT NOT NULL PRIMARY KEY, ProductVersion TEXT NOT NULL);");

        var options = new DbContextOptionsBuilder<VectorDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new VectorDbContext(options);
        var migrationsAssembly = db.GetService<IMigrationsAssembly>();
        var migrationIds = migrationsAssembly.Migrations.Keys.OrderBy(id => id).ToList();
        Ensure(migrationIds.Contains(migrationId), "The additive Identity migration is not registered.");

        foreach (var previousMigration in migrationIds.Where(id => string.CompareOrdinal(id, migrationId) < 0))
        {
            await using var historyCommand = connection.CreateCommand();
            historyCommand.CommandText =
                "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ($id, '8.0.5');";
            historyCommand.Parameters.AddWithValue("$id", previousMigration);
            await historyCommand.ExecuteNonQueryAsync();
        }

        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(migrationId);

        foreach (var tableName in new[] { "AspNetUsers", "AspNetUserClaims", "AspNetUserLogins", "AspNetUserTokens" })
        {
            Ensure(await ObjectExistsAsync(connection, "table", tableName),
                $"The disposable migration did not create {tableName}.");
        }

        Ensure(await ObjectExistsAsync(connection, "index", "IX_AspNetUsers_AppUserId"),
            "The one-to-one AppUser identity index is missing.");
        Ensure(await ObjectExistsAsync(connection, "index", "IX_AspNetUsers_CompanyId_NormalizedEmail"),
            "The tenant/email lookup index is missing.");

        var foreignTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var foreignKeyCommand = connection.CreateCommand())
        {
            foreignKeyCommand.CommandText = "PRAGMA foreign_key_list('AspNetUsers');";
            await using var reader = await foreignKeyCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                foreignTables.Add(reader.GetString(2));
            }
        }

        Ensure(foreignTables.SetEquals(new[] { "Companies", "AppUsers" }),
            "The Identity table foreign keys do not preserve the company/profile boundary.");

        var previousTarget = migrationIds.Last(id => string.CompareOrdinal(id, migrationId) < 0);
        await migrator.MigrateAsync(previousTarget);
        Ensure(!await ObjectExistsAsync(connection, "table", "AspNetUsers"),
            "Rolling back the additive migration left the Identity table behind.");
        Ensure(await ObjectExistsAsync(connection, "table", "AppUsers"),
            "Rolling back the additive migration altered the existing staff profile table.");
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> ObjectExistsAsync(SqliteConnection connection, string type, string name)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = $type AND name = $name;";
        command.Parameters.AddWithValue("$type", type);
        command.Parameters.AddWithValue("$name", name);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class IdentityFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;
        private readonly AsyncServiceScope _scope;

        private IdentityFixture(
            SqliteConnection connection,
            ServiceProvider provider,
            AsyncServiceScope scope,
            VectorDbContext db,
            Company companyA,
            Company companyB,
            AppUser seniorA,
            AppUser seniorB,
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
            SeniorA = seniorA;
            SeniorB = seniorB;
            StaffA = staffA;
            StaffB = staffB;
            ImportedProfile = importedProfile;
        }

        public IServiceProvider Services => _scope.ServiceProvider;
        public VectorDbContext Db { get; }
        public Company CompanyA { get; }
        public Company CompanyB { get; }
        public AppUser SeniorA { get; }
        public AppUser SeniorB { get; }
        public AppUser StaffA { get; }
        public AppUser StaffB { get; }
        public AppUser ImportedProfile { get; }

        public static async Task<IdentityFixture> CreateAsync()
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
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.User.RequireUniqueEmail = false;
                })
                .AddSignInManager()
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<VectorDbContext>();
            services.AddScoped<IdentityAuthenticationService>();
            services.AddScoped<IdentityAccountService>();

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
            await db.Database.EnsureCreatedAsync();

            var seniorRole = new AppRole { Name = "Senior Management" };
            var staffRole = new AppRole { Name = "Staff" };
            var companyA = new Company { Name = "Identity A", WorkspaceSlug = "identity-a", Status = "Active" };
            var companyB = new Company { Name = "Identity B", WorkspaceSlug = "identity-b", Status = "Active" };
            db.AddRange(seniorRole, staffRole, companyA, companyB);
            await db.SaveChangesAsync();

            var seniorA = Profile(companyA.Id, seniorRole.Id, "Senior A", "senior-a@example.test");
            var seniorB = Profile(companyB.Id, seniorRole.Id, "Senior B", "senior-b@example.test");
            var staffA = Profile(companyA.Id, staffRole.Id, "Staff A", "shared@example.test");
            var staffB = Profile(companyB.Id, staffRole.Id, "Staff B", "shared@example.test");
            var imported = Profile(companyA.Id, staffRole.Id, "Imported Profile", "imported@example.test");
            db.AddRange(seniorA, seniorB, staffA, staffB, imported);
            await db.SaveChangesAsync();

            return new IdentityFixture(
                connection, provider, scope, db,
                companyA, companyB, seniorA, seniorB, staffA, staffB, imported);
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
