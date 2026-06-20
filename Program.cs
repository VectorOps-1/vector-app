using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using vector_app_local.Data;
using vector_app_local.Services;

var builder = WebApplication.CreateBuilder(args);
const string DevelopmentDatabaseRepairCommand = "--dev-db-repair";
var runDevelopmentDatabaseRepair = args.Any(arg =>
    string.Equals(arg, DevelopmentDatabaseRepairCommand, StringComparison.OrdinalIgnoreCase));

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
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<LocationOptionService>();
builder.Services.AddScoped<SetupUploadService>();
builder.Services.AddScoped<ChecklistVarianceService>();
builder.Services.AddScoped<ReadinessAlertService>();
builder.Services.AddScoped<AuditTrailService>();
builder.Services.AddScoped<ChecklistReportPdfService>();
builder.Services.AddScoped<ReadinessEngineService>();
builder.Services.AddScoped<ReadinessEngineScoringService>();
builder.Services.AddScoped<ChecklistPublishingService>();
builder.Services.AddScoped<VehicleSchematicAssignmentService>();
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

app.MapMethods("/DailyChecklist", ["GET", "POST"], (HttpRequest request) =>
    Results.Redirect(BuildDailyChecklistRedirect(request)));
app.MapMethods("/DailyEquipmentChecklist", ["GET", "POST"], (HttpRequest request) =>
    Results.Redirect(BuildDailyChecklistRedirect(request)));
app.MapMethods("/EditEquipmentChecklist", ["GET", "POST"], () =>
    Results.Redirect("/EditChecklist"));
app.MapMethods("/ManagerAreas", ["GET", "POST"], () =>
    Results.Redirect("/AreaManagerControl"));

app.MapRazorPages();

app.Run();

static string BuildDailyChecklistRedirect(HttpRequest request)
{
    var query = new List<string>();
    AddQueryValue(query, "frequency", request.Query["frequency"].FirstOrDefault() ?? "daily");
    AddQueryValue(query, "registration", request.Query["registration"].FirstOrDefault());
    AddQueryValue(query, "callsign", request.Query["callsign"].FirstOrDefault());

    return query.Count == 0
        ? "/DailyVehicleChecklist"
        : $"/DailyVehicleChecklist?{string.Join("&", query)}";
}

static void AddQueryValue(List<string> query, string key, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return;
    }

    query.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value.Trim())}");
}
