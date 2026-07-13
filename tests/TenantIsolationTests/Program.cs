using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Text;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

var tests = new (string Name, Func<Task> Run)[]
{
    ("tenant workflow snapshots do not leak records", TenantWorkflowSnapshotsDoNotLeakRecordsAsync),
    ("current user service rejects company mismatch", CurrentUserServiceRejectsCompanyMismatchAsync),
    ("checklist publishing rejects foreign tenant inputs", ChecklistPublishingRejectsForeignTenantInputsAsync),
    ("schematic resolution is company bounded", SchematicResolutionIsCompanyBoundedAsync),
    ("tenant file storage requires company scoped paths", TenantFileStorageRequiresCompanyScopedPathsAsync),
    ("professional evidence PDF contains the immutable evidence contract", ProfessionalEvidencePdfContainsImmutableContractAsync)
};

foreach (var test in tests)
{
    await test.Run();
    Console.WriteLine($"PASS: {test.Name}");
}

Console.WriteLine("Tenant isolation tests passed.");

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

    var session = new TestSession();
    var httpContext = new DefaultHttpContext
    {
        Session = session
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
