using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;
using vector_app_local.Services;

var builder = WebApplication.CreateBuilder(args);
const string DevelopmentDatabaseRepairCommand = "--dev-db-repair";
const string SchemaBaselineCommand = "--schema-baseline";
var runDevelopmentDatabaseRepair = args.Any(arg =>
    string.Equals(arg, DevelopmentDatabaseRepairCommand, StringComparison.OrdinalIgnoreCase));
var runSchemaBaseline = args.Any(arg =>
    string.Equals(arg, SchemaBaselineCommand, StringComparison.OrdinalIgnoreCase));

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.ConfigureFilter(new SessionAccessPageFilter());
});
builder.Services.AddDistributedMemoryCache();
if (builder.Environment.IsDevelopment())
{
    var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, ".data-protection-keys");
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
        .SetApplicationName("AcuityOpsLocal");
}
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<IFeatureAccessService, FeatureAccessService>();
builder.Services.AddScoped<IUserActionPermissionService, UserActionPermissionService>();
builder.Services.AddScoped<IUserActionAuthorizationService, UserActionAuthorizationService>();
builder.Services.AddScoped<IFileSecurityScanner, NoOpFileSecurityScanner>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<LocationOptionService>();
builder.Services.AddScoped<SetupUploadService>();
builder.Services.AddScoped<IImportSourceInspector, ImportSourceInspector>();
builder.Services.AddScoped<IImportFieldRegistry, ImportFieldRegistry>();
builder.Services.AddScoped<ImportBatchService>();
builder.Services.AddScoped<AccessModelSetupService>();
builder.Services.AddScoped<AssetRegisterSetupService>();
builder.Services.AddScoped<ChecklistSetupService>();
builder.Services.AddScoped<ReadinessEngineSetupService>();
builder.Services.AddScoped<ChecklistVarianceService>();
builder.Services.AddScoped<ReadinessAlertService>();
builder.Services.AddScoped<AuditTrailService>();
builder.Services.AddScoped<ChecklistReportPdfService>();
builder.Services.AddScoped<ReadinessEngineService>();
builder.Services.AddScoped<ReadinessEngineScoringService>();
builder.Services.AddScoped<ChecklistPublishingService>();
builder.Services.AddScoped<VehicleSchematicAssignmentService>();
builder.Services.AddScoped<VehicleStructureSetupService>();
builder.Services.AddScoped<StaffStructureSetupService>();
builder.Services.AddScoped<CustomDropdownOptionService>();
builder.Services.AddScoped<ExpiryPressureService>();
builder.Services.AddDbContext<VectorDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("VectorDatabase"));
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("VectorDatabase"));
    }
});

var app = builder.Build();

if (runDevelopmentDatabaseRepair)
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Development database repair can only run in the Development environment.");
    }

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
    await DevelopmentDatabase.RepairSqliteDevelopmentSchemaAsync(db);
    app.Logger.LogInformation(
        "Development database repair completed by explicit command {Command}.",
        DevelopmentDatabaseRepairCommand);
    return;
}

if (runSchemaBaseline)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
    var migrationsAssembly = db.GetService<IMigrationsAssembly>();
    await SchemaBaselineInitializer.CreateCurrentSchemaBaselineAsync(db, migrationsAssembly);
    app.Logger.LogInformation(
        "Database schema baseline completed by explicit command {Command}.",
        SchemaBaselineCommand);
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
