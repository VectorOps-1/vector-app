using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;
using System.Text;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

var tests = new (string Name, Func<Task> Run)[]
{
    ("tenant workflow snapshots do not leak records", TenantWorkflowSnapshotsDoNotLeakRecordsAsync),
    ("workspace login recovery accepts issued links without a default tenant", WorkspaceLoginRecoveryAcceptsIssuedLinksAsync),
    ("current user service rejects company mismatch", CurrentUserServiceRejectsCompanyMismatchAsync),
    ("checklist publishing rejects foreign tenant inputs", ChecklistPublishingRejectsForeignTenantInputsAsync),
    ("schematic resolution is company bounded", SchematicResolutionIsCompanyBoundedAsync),
    ("tenant file storage requires company scoped paths", TenantFileStorageRequiresCompanyScopedPathsAsync),
    ("submitted report identity remains immutable after register changes", SubmittedReportIdentityRemainsImmutableAsync),
    ("professional evidence PDF contains the immutable evidence contract", ProfessionalEvidencePdfContainsImmutableContractAsync)
    ,("import field registry exposes required canonical contracts", ImportFieldRegistryExposesRequiredContractsAsync)
    ,("import source inspector handles quoted CSV within limits", ImportSourceInspectorHandlesQuotedCsvAsync)
    ,("import foundation is tenant scoped and creates no domain records", ImportFoundationIsTenantScopedAsync)
    ,("staff profile and login identity remain separated and tenant scoped", IdentitySeparationTests.RunAllAsync)
    ,("login identity provisioning is explicit, dry-run first, and replay safe", IdentityProvisioningTests.RunAllAsync)
    ,("deterministic register imports are tenant scoped, transactional, and non-login", ImportRegisterWorkflowTests.RunAllAsync)
    ,("deterministic checklist imports preserve four layouts and create draft-only templates", ChecklistImportConversionTests.RunAllAsync)
    ,("import governance keeps mappings tenant scoped and rollback conservative", ImportGovernanceTests.RunAllAsync)
};

foreach (var test in tests)
{
    await test.Run();
    Console.WriteLine($"PASS: {test.Name}");
}

Console.WriteLine("Tenant isolation tests passed.");

static Task WorkspaceLoginRecoveryAcceptsIssuedLinksAsync()
{
    Ensure(CompanyWorkspaceAccess.NormalizeWorkspaceSlug("acuityops-workspace-42-demo") == "acuityops-workspace-42-demo",
        "A workspace slug was not preserved.");
    Ensure(CompanyWorkspaceAccess.NormalizeWorkspaceSlug("https://app.example/CompanyLogin/acuityops-workspace-42-demo") == "acuityops-workspace-42-demo",
        "An issued workspace URL did not resolve to its workspace slug.");
    Ensure(CompanyWorkspaceAccess.NormalizeWorkspaceSlug(null) is null,
        "An empty workspace unexpectedly resolved to a default tenant.");
    return Task.CompletedTask;
}

static Task ImportFieldRegistryExposesRequiredContractsAsync()
{
    var registry = new ImportFieldRegistry();
    Ensure(registry.ContractVersion == 1, "Unexpected import field registry version.");
    Ensure(registry.FindField("vehicle.registration_number")?.IsRequired == true, "Vehicle registration is not a required canonical field.");
    Ensure(registry.FindField("stock.quantity")?.IsRequired == true, "Stock quantity is not a required canonical field.");
    Ensure(registry.FindField("medication.name")?.IsRequired == true, "Medication name is not a required canonical field.");
    Ensure(registry.FindField("staff.email")?.HelpText.Contains("never grants login", StringComparison.OrdinalIgnoreCase) == true,
        "Staff import contract does not state the no-login boundary.");
    Ensure(registry.Targets.Select(target => target.TargetType).ToHashSet(StringComparer.OrdinalIgnoreCase)
        .SetEquals(ImportTargetTypes.All), "Canonical field registry target types are incomplete.");
    return Task.CompletedTask;
}

static async Task ImportSourceInspectorHandlesQuotedCsvAsync()
{
    var inspector = new ImportSourceInspector();
    var csv = "Registration,Notes\r\nDEM-101,\"Front, left panel\"\r\n";
    var bytes = Encoding.UTF8.GetBytes(csv);
    await using var stream = new MemoryStream(bytes);
    var file = new FormFile(stream, 0, bytes.Length, "file", "vehicles.csv")
    {
        Headers = new HeaderDictionary(),
        ContentType = "text/csv"
    };

    var profile = await inspector.InspectAsync(file);
    Ensure(profile.FileType == "CSV", "CSV source profile did not retain the file type.");
    Ensure(profile.WorksheetCount == 1, "CSV source profile did not expose one logical worksheet.");
    Ensure(profile.TotalRows == 2, "CSV source profile row count is incorrect.");
    Ensure(profile.Worksheets[0].ColumnCount == 2, "Quoted CSV field was parsed as an extra column.");
    Ensure(profile.TotalNonEmptyCells == 4, "CSV source profile non-empty-cell count is incorrect.");

    var unsafeBytes = Encoding.UTF8.GetBytes("not a supported workbook");
    await using var unsafeStream = new MemoryStream(unsafeBytes);
    var unsafeFile = new FormFile(unsafeStream, 0, unsafeBytes.Length, "file", "macro.xlsm");
    await EnsureThrowsAsync<ImportSourceValidationException>(
        () => inspector.InspectAsync(unsafeFile),
        "Import inspector accepted a macro-enabled workbook extension.");
}

static async Task ImportFoundationIsTenantScopedAsync()
{
    await using var fixture = await TenantFixture.CreateAsync();
    var companyA = await fixture.Db.Companies.SingleAsync(company => company.Id == fixture.TenantA.CompanyId);
    companyA.SubscriptionTier = SubscriptionTiers.Pro;
    await fixture.Db.SaveChangesAsync();

    var actorA = await fixture.Db.AppUsers.Include(user => user.AppRole)
        .SingleAsync(user => user.Id == fixture.TenantA.SeniorUserId);
    var actorB = await fixture.Db.AppUsers.Include(user => user.AppRole)
        .SingleAsync(user => user.Id == fixture.TenantB.SeniorUserId);
    var staffA = await fixture.Db.AppUsers.Include(user => user.AppRole)
        .SingleAsync(user => user.Id == fixture.TenantA.StaffUserId);

    var domainCountsBefore = new
    {
        Vehicles = await fixture.Db.Vehicles.CountAsync(),
        Staff = await fixture.Db.AppUsers.CountAsync(),
        Equipment = await fixture.Db.EquipmentItems.CountAsync(),
        Stock = await fixture.Db.StockItems.CountAsync(),
        Medication = await fixture.Db.MedicationItems.CountAsync(),
        Checklists = await fixture.Db.ChecklistTemplates.CountAsync()
    };

    var sourceFile = new AssetFile
    {
        CompanyId = actorA.CompanyId,
        UploadedByUserId = actorA.Id,
        LinkedEntityType = SetupUploadService.RegisterUploadEntityType,
        Category = "Vehicle Register",
        OriginalFileName = "vehicles.csv",
        ContentType = "text/csv",
        StorageProvider = LocalFileStorageService.Provider,
        StoragePath = $"local/company-{actorA.CompanyId}/registerupload-vehicle/2026/07/vehicles.csv",
        SizeBytes = 50
    };
    fixture.Db.AssetFiles.Add(sourceFile);

    var service = new ImportBatchService(fixture.Db, new UserActionPermissionService(fixture.Db));
    Ensure((await service.CanPrepareAsync(actorA)).Allowed,
        "Pro senior user was not allowed to prepare a guided import.");
    var profile = new ImportSourceProfile(1, new string('A', 64), "CSV", 1, 2, 4,
        [new ImportWorksheetProfile("vehicles", 2, 2, 4)]);
    var batch = await service.CreateUploadedBatchAsync(actorA, sourceFile, ImportTargetTypes.Vehicle, profile, DateTime.UtcNow);
    await fixture.Db.SaveChangesAsync();

    Ensure(await service.GetForCurrentTenantAsync(actorA, batch.Id) is not null,
        "Import batch was not visible to its owning tenant.");
    Ensure(await service.GetForCurrentTenantAsync(actorB, batch.Id) is null,
        "Import batch leaked to another tenant.");
    Ensure(!(await service.CanPrepareAsync(actorB)).Allowed,
        "Base tenant was allowed to prepare a guided import.");
    Ensure(!(await service.CanPrepareAsync(staffA)).Allowed,
        "Staff user without import permission was allowed to prepare a guided import.");
    Ensure(!(await service.CanCommitAsync(staffA)).Allowed,
        "Staff user without import permission was allowed to commit a guided import.");

    Ensure(domainCountsBefore.Vehicles == await fixture.Db.Vehicles.CountAsync(), "Import upload created a vehicle record.");
    Ensure(domainCountsBefore.Staff == await fixture.Db.AppUsers.CountAsync(), "Import upload created a staff/login record.");
    Ensure(domainCountsBefore.Equipment == await fixture.Db.EquipmentItems.CountAsync(), "Import upload created an equipment record.");
    Ensure(domainCountsBefore.Stock == await fixture.Db.StockItems.CountAsync(), "Import upload created a stock record.");
    Ensure(domainCountsBefore.Medication == await fixture.Db.MedicationItems.CountAsync(), "Import upload created a medication record.");
    Ensure(domainCountsBefore.Checklists == await fixture.Db.ChecklistTemplates.CountAsync(), "Import upload created a checklist template.");
}

static async Task TenantWorkflowSnapshotsDoNotLeakRecordsAsync()
{
    await using var fixture = await TenantFixture.CreateAsync();

    var tenantA = await TenantSnapshot.LoadAsync(fixture.Db, fixture.TenantA.CompanyId);
    var tenantB = await TenantSnapshot.LoadAsync(fixture.Db, fixture.TenantB.CompanyId);

    tenantA.AssertContainsOnly("tenant-a-private", "tenant-b-private");
    tenantB.AssertContainsOnly("tenant-b-private", "tenant-a-private");
}

static async Task CurrentUserServiceRejectsCompanyMismatchAsync()
{
    await using var fixture = await TenantFixture.CreateAsync();

    var identity = new ApplicationIdentityUser
    {
        Id = "tenant-a-senior-identity",
        CompanyId = fixture.TenantA.CompanyId,
        AppUserId = fixture.TenantA.SeniorUserId,
        UserName = "tenant-a-senior",
        NormalizedUserName = "TENANT-A-SENIOR",
        Email = "alex.manager@example.test",
        NormalizedEmail = "ALEX.MANAGER@EXAMPLE.TEST",
        IsLoginEnabled = true,
        MustChangePassword = false,
        LockoutEnabled = true
    };
    fixture.Db.LoginIdentities.Add(identity);
    await fixture.Db.SaveChangesAsync();

    var session = new TestSession();
    var httpContext = new DefaultHttpContext
    {
        Session = session,
        User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, identity.Id) },
            "IdentityTest"))
    };
    var service = new CurrentUserService(
        new HttpContextAccessor { HttpContext = httpContext },
        fixture.Db);

    session.SetInt32(CurrentUserService.UserIdSessionKey, fixture.TenantA.SeniorUserId);
    session.SetInt32(CurrentUserService.CompanyIdSessionKey, fixture.TenantA.CompanyId);
    var validUser = await service.GetCurrentUserAsync();
    Ensure(validUser?.CompanyId == fixture.TenantA.CompanyId, "Valid tenant session did not resolve the tenant A user.");

    session.SetInt32(CurrentUserService.UserIdSessionKey, fixture.TenantA.SeniorUserId);
    session.SetInt32(CurrentUserService.CompanyIdSessionKey, fixture.TenantB.CompanyId);
    var mismatchedUser = await service.GetCurrentUserAsync();
    Ensure(mismatchedUser is null, "User lookup accepted a user id from a different company session.");
}

static async Task ChecklistPublishingRejectsForeignTenantInputsAsync()
{
    await using var fixture = await TenantFixture.CreateAsync();

    var service = new ChecklistPublishingService(fixture.Db);
    var actor = await fixture.Db.AppUsers
        .Include(user => user.AppRole)
        .SingleAsync(user => user.Id == fixture.TenantA.SeniorUserId);

    var foreignTemplateResult = await service.PublishAsync(actor, new ChecklistPublishRequest(
        fixture.TenantB.ChecklistTemplateId,
        ChecklistPublishingService.ScopeAllAreas,
        null,
        null,
        null,
        "Attempted cross-tenant template publish"));
    Ensure(!foreignTemplateResult.Success, "Checklist publishing accepted a template owned by another company.");

    var foreignVehicleResult = await service.PublishAsync(actor, new ChecklistPublishRequest(
        fixture.TenantA.ChecklistTemplateId,
        ChecklistPublishingService.ScopeVehicle,
        null,
        fixture.TenantB.VehicleId,
        null,
        "Attempted cross-tenant vehicle publish"));
    Ensure(!foreignVehicleResult.Success, "Checklist publishing accepted a vehicle owned by another company.");
}

static async Task SchematicResolutionIsCompanyBoundedAsync()
{
    await using var fixture = await TenantFixture.CreateAsync();

    var service = new VehicleSchematicAssignmentService(fixture.Db);
    var vehicleA = await fixture.Db.Vehicles.AsNoTracking().SingleAsync(vehicle => vehicle.Id == fixture.TenantA.VehicleId);
    var vehicleB = await fixture.Db.Vehicles.AsNoTracking().SingleAsync(vehicle => vehicle.Id == fixture.TenantB.VehicleId);

    var schematicA = await service.ResolveForVehicleAsync(fixture.TenantA.CompanyId, vehicleA);
    Ensure(schematicA?.Key == "pickup-rv", "Tenant A vehicle did not resolve its own schematic assignment.");

    var schematicB = await service.ResolveForVehicleAsync(fixture.TenantB.CompanyId, vehicleB);
    Ensure(schematicB?.Key == "toyota-quantum-hiace-high-roof", "Tenant B vehicle did not resolve its own schematic assignment.");

    var crossTenantResolution = await service.ResolveForVehicleAsync(fixture.TenantA.CompanyId, vehicleB);
    Ensure(crossTenantResolution is null, "Schematic resolution accepted a vehicle object from another company.");
}

static async Task TenantFileStorageRequiresCompanyScopedPathsAsync()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), "acuityops-tenant-isolation-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempRoot);

    try
    {
        var environment = new TestWebHostEnvironment(tempRoot);
        var storage = new LocalFileStorageService(environment, new NoOpFileSecurityScanner());

        await EnsureThrowsAsync<InvalidOperationException>(
            () => storage.SaveAsync(CreatePngFormFile(), 0, "staff", FileStorageValidationOptions.StaffDocument),
            "File storage accepted a missing company id.");

        var tenantAFile = await storage.SaveAsync(CreatePngFormFile(), 101, "staff", FileStorageValidationOptions.StaffDocument);
        var tenantBFile = await storage.SaveAsync(CreatePngFormFile(), 202, "staff", FileStorageValidationOptions.StaffDocument);

        Ensure(tenantAFile.StoragePath.StartsWith("local/company-101/staff/", StringComparison.OrdinalIgnoreCase),
            $"Tenant A file path was not company scoped: {tenantAFile.StoragePath}");
        Ensure(tenantBFile.StoragePath.StartsWith("local/company-202/staff/", StringComparison.OrdinalIgnoreCase),
            $"Tenant B file path was not company scoped: {tenantBFile.StoragePath}");
        Ensure(!string.Equals(tenantAFile.StoragePath, tenantBFile.StoragePath, StringComparison.OrdinalIgnoreCase),
            "Tenant file storage reused the same path for two companies.");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static Task ProfessionalEvidencePdfContainsImmutableContractAsync()
{
    var evidence = new ChecklistEvidenceSnapshot
    {
        CapturedAtUtc = DateTime.UtcNow,
        Tenant = new EvidenceTenantSnapshot { CompanyId = 44, DisplayName = "Evidence Test EMS" },
        Submission = new EvidenceSubmissionMetadata
        {
            ReportId = 817,
            WorkflowStatus = "Submitted",
            ReadinessStatus = "Operational",
            SubmittedAtUtc = DateTime.UtcNow
        },
        Submitter = new EvidenceSubmitterSnapshot
        {
            FullName = "Casey Clinician",
            Role = "Staff",
            StaffIdentifier = "STAFF-817"
        },
        Vehicle = new EvidenceVehicleSnapshot
        {
            RegistrationNumber = "EV-817",
            Callsign = "A817",
            VehicleFunction = "Ambulance",
            VehicleSubtype = "Operational Ambulance"
        },
        Template = new EvidenceTemplateSnapshot
        {
            Name = "Evidence Contract Test",
            Version = "4.2",
            PublishScopeSummary = "Callsign A817"
        },
        Sections =
        [
            new EvidenceSectionSnapshot
            {
                Name = "Vehicle",
                Items =
                [
                    new EvidenceItemSnapshot
                    {
                        Prompt = "Warning lights",
                        Fields = [new EvidenceFieldSnapshot { Heading = "Status", Value = "Pass" }]
                    }
                ]
            }
        ],
        Notes = new EvidenceNotesSnapshot { GeneralNotes = "Immutable evidence marker 817" }
    };

    var pdf = new ChecklistReportPdfService().BuildDailyReadinessPdf(new DailyVehicleReadinessReport(), evidence);
    var raw = Encoding.ASCII.GetString(pdf);
    var previewPath = Environment.GetEnvironmentVariable("ACUITYOPS_PDF_TEST_OUTPUT");
    if (!string.IsNullOrWhiteSpace(previewPath))
    {
        File.WriteAllBytes(previewPath, pdf);
    }

    Ensure(raw.StartsWith("%PDF-1.4", StringComparison.Ordinal), "Evidence PDF does not have a valid PDF header.");
    Ensure(raw.Contains("/BaseFont /Helvetica-Bold", StringComparison.Ordinal), "Evidence PDF is missing the professional heading font.");
    Ensure(raw.Contains("Evidence Test EMS", StringComparison.Ordinal), "Evidence PDF omitted the tenant identity.");
    Ensure(raw.Contains("Evidence Contract Test", StringComparison.Ordinal), "Evidence PDF omitted the template identity.");
    Ensure(raw.Contains("Warning lights - Status", StringComparison.Ordinal), "Evidence PDF omitted a dynamic checklist answer.");
    Ensure(raw.Contains("Immutable evidence marker 817", StringComparison.Ordinal), "Evidence PDF omitted submission notes.");
    Ensure(raw.Contains("Page 1 of", StringComparison.Ordinal), "Evidence PDF omitted print-safe pagination.");
    Ensure(!raw.Contains("/BaseFont /Courier", StringComparison.Ordinal), "Evidence PDF still uses the prototype Courier renderer.");

    return Task.CompletedTask;
}

static Task SubmittedReportIdentityRemainsImmutableAsync()
{
    var captured = new ChecklistEvidenceSnapshot
    {
        CapturedAtUtc = new DateTime(2026, 7, 13, 8, 30, 0, DateTimeKind.Utc),
        Vehicle = new EvidenceVehicleSnapshot
        {
            OperationalAreaId = 71,
            OperationalAreaName = "Captured Area",
            RegistrationNumber = "CAP-101",
            Callsign = "CAP1",
            VehicleFunction = "Ambulance",
            VehicleSubtype = "Primary Response"
        },
        Submitter = new EvidenceSubmitterSnapshot { FullName = "Captured Clinician", Role = "Staff" },
        Template = new EvidenceTemplateSnapshot { Name = "Captured Checklist", Version = "3.1" },
        Submission = new EvidenceSubmissionMetadata
        {
            ReportId = 901,
            WorkflowStatus = "Submitted",
            ReadinessStatus = "Operational",
            SubmittedAtUtc = new DateTime(2026, 7, 13, 8, 30, 0, DateTimeKind.Utc)
        }
    };
    var report = new DailyVehicleReadinessReport
    {
        Id = 901,
        CompanyId = 44,
        EvidenceSnapshotVersion = ChecklistEvidenceSnapshot.CurrentVersion,
        EvidenceSnapshotJson = ChecklistEvidenceSnapshotSerializer.Serialize(captured),
        VehicleRegistrationNumber = "MUTATED-REG",
        CallsignAtCheck = "MUTATED-CALLSIGN",
        WorkflowStatus = "Draft",
        ReadinessStatus = "Not ready",
        Vehicle = new Vehicle
        {
            RegistrationNumber = "CURRENT-REG",
            Callsign = "CURRENT-CALLSIGN",
            CurrentOperationalAreaId = 99,
            CurrentOperationalArea = new OperationalArea { Id = 99, Name = "Current Area" }
        },
        PerformedByUser = new AppUser { FullName = "Renamed User" },
        ChecklistTemplate = new ChecklistTemplate { Name = "Renamed Template", Version = "9.9" }
    };

    var resolved = ChecklistEvidenceSnapshotResolver.Resolve(report);

    Ensure(resolved.Vehicle.OperationalAreaId == 71, "Report scope followed the vehicle's current area instead of the submission snapshot.");
    Ensure(resolved.Vehicle.RegistrationNumber == "CAP-101", "Report identity followed a mutable vehicle record.");
    Ensure(resolved.Submitter.FullName == "Captured Clinician", "Report identity followed a mutable staff profile.");
    Ensure(resolved.Template.Name == "Captured Checklist", "Report identity followed a mutable checklist template.");
    Ensure(resolved.Submission.WorkflowStatus == "Submitted", "Report workflow status did not come from the immutable snapshot.");

    return Task.CompletedTask;
}

static IFormFile CreatePngFormFile()
{
    var bytes = new byte[]
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D
    };
    var stream = new MemoryStream(bytes);
    return new FormFile(stream, 0, bytes.Length, "file", "evidence.png")
    {
        Headers = new HeaderDictionary(),
        ContentType = "image/png"
    };
}

static async Task EnsureThrowsAsync<TException>(Func<Task> action, string failureMessage)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(failureMessage);
}

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class TenantFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TenantFixture(SqliteConnection connection, VectorDbContext db, TenantIds tenantA, TenantIds tenantB)
    {
        _connection = connection;
        Db = db;
        TenantA = tenantA;
        TenantB = tenantB;
    }

    public VectorDbContext Db { get; }
    public TenantIds TenantA { get; }
    public TenantIds TenantB { get; }

    public static async Task<TenantFixture> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<VectorDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new VectorDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var seniorRole = new AppRole { Name = "Senior Management" };
        var staffRole = new AppRole { Name = "Staff" };
        db.AppRoles.AddRange(seniorRole, staffRole);
        await db.SaveChangesAsync();

        var companyA = new Company
        {
            Name = "Mirror EMS",
            TradingName = "Mirror EMS tenant-a-private",
            WorkspaceSlug = "mirror-alpha",
            WorkspaceAccessCode = "tenant-a-code",
            BrandingStatus = CompanyBranding.BrandingStatusConfigured,
            Status = "Active"
        };
        var companyB = new Company
        {
            Name = "Mirror EMS",
            TradingName = "Mirror EMS tenant-b-private",
            WorkspaceSlug = "mirror-bravo",
            WorkspaceAccessCode = "tenant-b-code",
            BrandingStatus = CompanyBranding.BrandingStatusConfigured,
            Status = "Active"
        };
        db.Companies.AddRange(companyA, companyB);
        await db.SaveChangesAsync();

        companyA.LogoStoragePath = CompanyBranding.GetStoredLogoPath(companyA.Id);
        companyB.LogoStoragePath = CompanyBranding.GetStoredLogoPath(companyB.Id);

        var tenantA = await AddTenantRecordsAsync(db, companyA, seniorRole, staffRole, "tenant-a-private", "pickup-rv");
        var tenantB = await AddTenantRecordsAsync(db, companyB, seniorRole, staffRole, "tenant-b-private", "toyota-quantum-hiace-high-roof");
        await db.SaveChangesAsync();

        return new TenantFixture(connection, db, tenantA, tenantB);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static async Task<TenantIds> AddTenantRecordsAsync(
        VectorDbContext db,
        Company company,
        AppRole seniorRole,
        AppRole staffRole,
        string marker,
        string schematicKey)
    {
        var area = new OperationalArea
        {
            CompanyId = company.Id,
            Name = "Central Base",
            AreaType = "Base",
            Notes = marker
        };
        db.OperationalAreas.Add(area);
        await db.SaveChangesAsync();

        var senior = new AppUser
        {
            CompanyId = company.Id,
            AppRoleId = seniorRole.Id,
            FullName = "Alex Manager",
            Email = "alex.manager@example.test",
            StaffIdentifier = "STAFF-001",
            QualificationFunction = "Emergency Care Practitioner",
            AssignedOperationalAreaId = area.Id,
            Status = "Active"
        };
        var staff = new AppUser
        {
            CompanyId = company.Id,
            AppRoleId = staffRole.Id,
            FullName = "Casey Clinician",
            Email = "casey.clinician@example.test",
            StaffIdentifier = "STAFF-002",
            QualificationFunction = "Basic Life Support",
            AssignedOperationalAreaId = area.Id,
            Status = "Active"
        };
        db.AppUsers.AddRange(senior, staff);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle
        {
            CompanyId = company.Id,
            RegistrationNumber = "DUP-001",
            Callsign = "RV1",
            VehicleType = "Response Vehicle",
            VehicleFunction = "Response Vehicle",
            VehicleSubtype = "Pickup RV",
            CurrentOperationalAreaId = area.Id,
            Notes = marker,
            Status = "Active"
        };
        var equipment = new EquipmentItem
        {
            CompanyId = company.Id,
            Name = "Monitor Defibrillator",
            EquipmentType = "Monitor Defibrillator",
            Model = "Shared Model",
            SerialOrAssetId = "SHARED-SERIAL",
            CurrentOperationalAreaId = area.Id,
            Notes = marker,
            Status = "Active"
        };
        var stock = new StockItem
        {
            CompanyId = company.Id,
            CreatedByUserId = senior.Id,
            ItemName = "Gloves",
            ItemType = "Nitrile",
            StockCategory = "PPE",
            BatchNumber = "SHARED-BATCH",
            Quantity = 10,
            CurrentOperationalAreaId = area.Id,
            Notes = marker,
            Status = "Active"
        };
        var medication = new MedicationItem
        {
            CompanyId = company.Id,
            CreatedByUserId = senior.Id,
            Name = "Adrenaline",
            MedicationCode = "MED-001",
            MedicationType = "Ampoule",
            BatchNumber = "SHARED-BATCH",
            Quantity = 5,
            CurrentOperationalAreaId = area.Id,
            Notes = marker,
            Status = "Active"
        };
        var checklist = new ChecklistTemplate
        {
            CompanyId = company.Id,
            ClientName = "Mirror EMS",
            Name = "Daily Vehicle & Equipment Check",
            ChecklistType = "Vehicle",
            TargetVehicleType = "Pickup RV",
            Status = "Published",
            SourceType = "Built",
            IsPublished = true,
            PublishedByUserId = senior.Id,
            PublishedAtUtc = DateTime.UtcNow,
            PublishScopeSummary = marker
        };
        db.Vehicles.Add(vehicle);
        db.EquipmentItems.Add(equipment);
        db.StockItems.Add(stock);
        db.MedicationItems.Add(medication);
        db.ChecklistTemplates.Add(checklist);
        await db.SaveChangesAsync();

        var report = new DailyVehicleReadinessReport
        {
            CompanyId = company.Id,
            VehicleId = vehicle.Id,
            PerformedByUserId = staff.Id,
            ChecklistTemplateId = checklist.Id,
            ChecklistTemplateVersion = checklist.Version,
            VehicleRegistrationNumber = vehicle.RegistrationNumber,
            CallsignAtCheck = vehicle.Callsign,
            VehicleTypeAtCheck = vehicle.VehicleType,
            WorkflowStatus = "Submitted",
            ReadinessStatus = "Operational",
            GeneralNotes = marker,
            SubmittedAtUtc = DateTime.UtcNow
        };
        var task = new TaskItem
        {
            CompanyId = company.Id,
            AssignedToUserId = staff.Id,
            AssignedByUserId = senior.Id,
            ActionType = "Inspect",
            RelatedItemReference = "DUP-001",
            InstructionMessage = marker,
            Status = "Open"
        };
        var issue = new IssueReport
        {
            CompanyId = company.Id,
            ReportedByUserId = staff.Id,
            AssignedToUserId = senior.Id,
            ManagerLevel = "Operational Management",
            Module = "Vehicle",
            IssueType = "Damage",
            RelatedItem = "DUP-001",
            Description = marker,
            Status = "Open"
        };
        var scope = new ChecklistPublishScope
        {
            CompanyId = company.Id,
            ChecklistTemplateId = checklist.Id,
            ScopeType = ChecklistPublishingService.ScopeVehicleSubtype,
            PublishedByUserId = senior.Id,
            PublishNote = marker,
            IsActive = true
        };
        var assetFile = new AssetFile
        {
            CompanyId = company.Id,
            UploadedByUserId = staff.Id,
            LinkedEntityType = "staff",
            LinkedEntityId = staff.Id,
            Category = "License",
            OriginalFileName = "shared-license.pdf",
            ContentType = "application/pdf",
            StorageProvider = LocalFileStorageService.Provider,
            StoragePath = $"local/company-{company.Id}/staff/2026/06/{marker}.pdf",
            SizeBytes = 512,
            Notes = marker
        };
        var uploadedFile = new UploadedFile
        {
            CompanyId = company.Id,
            ChecklistTemplateId = checklist.Id,
            OriginalFileName = "shared-checklist.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            StoragePath = $"local/company-{company.Id}/setup/2026/06/{marker}.xlsx",
            SizeBytes = 1024
        };
        var schematicAssignment = new VehicleSchematicAssignment
        {
            CompanyId = company.Id,
            SchematicKey = schematicKey,
            ScopeType = VehicleSchematicAssignmentService.SubtypeScope,
            VehicleFunction = vehicle.VehicleFunction,
            VehicleSubtype = vehicle.VehicleSubtype,
            CreatedByUserId = senior.Id
        };
        var auditLog = new AuditLog
        {
            CompanyId = company.Id,
            AppUserId = senior.Id,
            Action = "Tenant test action",
            EntityType = "TenantIsolation",
            EntityId = company.Id,
            Details = marker
        };
        var readinessVersion = new ReadinessEngineVersion
        {
            CompanyId = company.Id,
            Name = "Tenant readiness engine",
            Status = ReadinessEngineStatuses.Published,
            PublishedByUserId = senior.Id
        };

        db.DailyVehicleReadinessReports.Add(report);
        db.TaskItems.Add(task);
        db.IssueReports.Add(issue);
        db.ChecklistPublishScopes.Add(scope);
        db.AssetFiles.Add(assetFile);
        db.UploadedFiles.Add(uploadedFile);
        db.VehicleSchematicAssignments.Add(schematicAssignment);
        db.AuditLogs.Add(auditLog);
        db.ReadinessEngineVersions.Add(readinessVersion);
        await db.SaveChangesAsync();

        var readinessRule = new ReadinessEngineRule
        {
            CompanyId = company.Id,
            ReadinessEngineVersionId = readinessVersion.Id,
            AssetType = "Vehicle",
            Section = "Operational",
            ItemName = "Warning lights",
            TriggerValue = "Failed",
            Notes = marker,
            DefaultImpactPercent = 40,
            Severity = ReadinessRuleSeverity.Critical,
            IsActive = true
        };
        db.ReadinessEngineRules.Add(readinessRule);
        await db.SaveChangesAsync();

        var equipmentCheck = new DailyVehicleEquipmentCheck
        {
            CompanyId = company.Id,
            DailyVehicleReadinessReportId = report.Id,
            EquipmentItemId = equipment.Id,
            EquipmentName = "Monitor Defibrillator",
            SerialOrAssetId = "SHARED-SERIAL",
            Notes = marker,
            SortOrder = 1
        };
        db.DailyVehicleEquipmentChecks.Add(equipmentCheck);
        await db.SaveChangesAsync();

        db.ReadinessAlerts.Add(new ReadinessAlert
        {
            CompanyId = company.Id,
            ReadinessEngineRuleId = readinessRule.Id,
            DailyVehicleReadinessReportId = report.Id,
            DailyVehicleEquipmentCheckId = equipmentCheck.Id,
            VehicleId = vehicle.Id,
            TriggeredByUserId = staff.Id,
            AssignedToUserId = senior.Id,
            AssetType = "Equipment",
            SourceArea = "Central Base",
            ItemName = "Monitor Defibrillator",
            TriggerValue = "Failed",
            Severity = ReadinessRuleSeverity.Critical,
            ImpactPercent = 40,
            VehicleLabel = "RV1 / DUP-001",
            AlertSummary = marker
        });
        await db.SaveChangesAsync();

        return new TenantIds(
            company.Id,
            senior.Id,
            staff.Id,
            area.Id,
            vehicle.Id,
            checklist.Id);
    }
}

sealed record TenantIds(
    int CompanyId,
    int SeniorUserId,
    int StaffUserId,
    int OperationalAreaId,
    int VehicleId,
    int ChecklistTemplateId);

sealed record TenantSnapshot(IReadOnlyList<string> Markers)
{
    public static async Task<TenantSnapshot> LoadAsync(VectorDbContext db, int companyId)
    {
        var markers = new List<string>();

        markers.AddRange(await db.Companies
            .Where(item => item.Id == companyId)
            .Select(item => item.TradingName ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.AppUsers
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.QualificationFunction ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.OperationalAreas
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.Notes ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.Vehicles
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.Notes ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.EquipmentItems
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.Notes ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.StockItems
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.Notes ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.MedicationItems
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.Notes ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.ChecklistTemplates
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.PublishScopeSummary ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.ChecklistPublishScopes
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.PublishNote ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.DailyVehicleReadinessReports
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.GeneralNotes ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.DailyVehicleEquipmentChecks
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.Notes ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.AssetFiles
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.StoragePath + "|" + (item.Notes ?? string.Empty))
            .ToListAsync());
        markers.AddRange(await db.UploadedFiles
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.StoragePath)
            .ToListAsync());
        markers.AddRange(await db.VehicleSchematicAssignments
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.SchematicKey)
            .ToListAsync());
        markers.AddRange(await db.ReadinessEngineRules
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.Notes ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.ReadinessAlerts
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.AlertSummary)
            .ToListAsync());
        markers.AddRange(await db.IssueReports
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.Description)
            .ToListAsync());
        markers.AddRange(await db.TaskItems
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.InstructionMessage ?? string.Empty)
            .ToListAsync());
        markers.AddRange(await db.AuditLogs
            .Where(item => item.CompanyId == companyId)
            .Select(item => item.Details ?? string.Empty)
            .ToListAsync());

        return new TenantSnapshot(markers.Where(marker => !string.IsNullOrWhiteSpace(marker)).ToList());
    }

    public void AssertContainsOnly(string expectedMarker, string forbiddenMarker)
    {
        EnsureSnapshot(Markers.Any(marker => marker.Contains(expectedMarker, StringComparison.OrdinalIgnoreCase)),
            $"Tenant snapshot did not include expected marker {expectedMarker}.");
        EnsureSnapshot(!Markers.Any(marker => marker.Contains(forbiddenMarker, StringComparison.OrdinalIgnoreCase)),
            $"Tenant snapshot leaked marker {forbiddenMarker}.");
    }

    private static void EnsureSnapshot(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _values = new(StringComparer.Ordinal);

    public IEnumerable<string> Keys => _values.Keys;
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public bool IsAvailable => true;

    public void Clear() => _values.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _values.Remove(key);
    public void Set(string key, byte[] value) => _values[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _values.TryGetValue(key, out value!);
}

sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public TestWebHostEnvironment(string rootPath)
    {
        ContentRootPath = rootPath;
        WebRootPath = Path.Combine(rootPath, "wwwroot");
        Directory.CreateDirectory(WebRootPath);
        ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
        WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
    }

    public string ApplicationName { get; set; } = "TenantIsolationTests";
    public IFileProvider ContentRootFileProvider { get; set; }
    public string ContentRootPath { get; set; }
    public string EnvironmentName { get; set; } = "Test";
    public string WebRootPath { get; set; }
    public IFileProvider WebRootFileProvider { get; set; }
}
