using Microsoft.EntityFrameworkCore;
using vector_app_local.Models;
using vector_app_local.Services;

internal static class ImportRegisterWorkflowTests
{
    public static async Task RunAllAsync()
    {
        await using var fixture = await TenantFixture.CreateAsync();
        var company = await fixture.Db.Companies.SingleAsync(item => item.Id == fixture.TenantA.CompanyId);
        company.SubscriptionTier = SubscriptionTiers.Pro;
        fixture.Db.VehicleFunctionSetups.Add(new VehicleFunctionSetup { CompanyId = company.Id, Name = "Ambulance", Status = "Active" });
        fixture.Db.StaffQualificationSetups.Add(new StaffQualificationSetup { CompanyId = company.Id, Name = "ILS", Status = "Active" });
        await fixture.Db.SaveChangesAsync();
        var actor = await fixture.Db.AppUsers.Include(user => user.AppRole).SingleAsync(user => user.Id == fixture.TenantA.SeniorUserId);

        await CommitCreateAsync(fixture, actor, ImportTargetTypes.OperationalArea,
            ("Name", "South Base"), ("Area Type", "Base"));
        var southBase = await fixture.Db.OperationalAreas.SingleAsync(item => item.CompanyId == company.Id && item.Name == "South Base");
        await CommitCreateAsync(fixture, actor, ImportTargetTypes.StorageLocation,
            ("Name", "South Store"), ("Operational Area", "South Base"), ("Storage Type", "General store"));
        await CommitCreateAsync(fixture, actor, ImportTargetTypes.Vehicle,
            ("Registration", "IMP-101"), ("Call sign", "I01"), ("Function", "Ambulance"), ("Area", "South Base"));
        await CommitCreateAsync(fixture, actor, ImportTargetTypes.Equipment,
            ("Equipment", "Imported Monitor"), ("Serial number", "IMP-EQ-1"), ("Area", "South Base"));
        await CommitCreateAsync(fixture, actor, ImportTargetTypes.Stock,
            ("Item", "Imported Gauze"), ("Quantity", "12"), ("Batch number", "IMP-S-1"), ("Area", "South Base"));
        await CommitCreateAsync(fixture, actor, ImportTargetTypes.Medication,
            ("Drug", "Imported Adrenaline"), ("Code", "IMP-M-1"), ("Quantity", "5"), ("Area", "South Base"));
        await CommitCreateAsync(fixture, actor, ImportTargetTypes.Staff,
            ("Staff name", "Imported Clinician"), ("Email address", "imported.clinician@example.test"), ("Staff number", "IMP-ST-1"), ("Qualification", "ILS"), ("Area", "South Base"));

        Assert(await fixture.Db.StorageLocations.AnyAsync(item => item.CompanyId == company.Id && item.OperationalAreaId == southBase.Id && item.Name == "South Store"), "Storage import failed.");
        Assert(await fixture.Db.Vehicles.AnyAsync(item => item.CompanyId == company.Id && item.RegistrationNumber == "IMP-101"), "Vehicle import failed.");
        Assert(await fixture.Db.EquipmentItems.AnyAsync(item => item.CompanyId == company.Id && item.SerialOrAssetId == "IMP-EQ-1"), "Equipment import failed.");
        Assert(await fixture.Db.StockItems.AnyAsync(item => item.CompanyId == company.Id && item.ItemName == "Imported Gauze" && item.Quantity == 12), "Stock import failed.");
        Assert(await fixture.Db.MedicationItems.AnyAsync(item => item.CompanyId == company.Id && item.MedicationCode == "IMP-M-1"), "Medication import failed.");
        var importedStaff = await fixture.Db.AppUsers.Include(item => item.LoginIdentity).SingleAsync(item => item.CompanyId == company.Id && item.Email == "imported.clinician@example.test");
        Assert(importedStaff.LoginIdentity is null, "Staff import granted login access.");

        var duplicatePrepared = await PrepareAsync(fixture, actor, ImportTargetTypes.Vehicle,
            ("Registration", "IMP-101"), ("Call sign", "I02"));
        var duplicate = duplicatePrepared.Batch;
        var duplicateRow = await fixture.Db.ImportRowResults.SingleAsync(item => item.ImportBatchId == duplicate.Id);
        Assert(duplicateRow.ValidationStatus == ImportRowStatuses.Duplicate && duplicateRow.RowDecision is null, "Duplicate did not require an explicit decision.");
        var values = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string?>>(duplicateRow.CorrectedPayloadJson!)!;
        await duplicatePrepared.Workflow.CorrectRowAsync(actor, duplicate.Id,
            new ImportRowCorrection(duplicateRow.SourceRowNumber, values, true, ImportRowDecisions.Update));
        var updateResult = await duplicatePrepared.Workflow.CommitAsync(actor, duplicate.Id);
        Assert(updateResult.Updated == 1, "Explicit update decision did not update one record.");
        Assert((await fixture.Db.Vehicles.SingleAsync(item => item.CompanyId == company.Id && item.RegistrationNumber == "IMP-101")).Callsign == "I02", "Vehicle update did not apply.");

        var invalid = await PrepareAsync(fixture, actor, ImportTargetTypes.Stock, ("Item", "Invalid stock"), ("Quantity", "not-a-number"));
        Assert((await fixture.Db.ImportRowResults.SingleAsync(item => item.ImportBatchId == invalid.Batch.Id)).ValidationStatus == ImportRowStatuses.Invalid,
            "Invalid integer was accepted.");

        var foreignAreaName = await fixture.Db.OperationalAreas.Where(item => item.CompanyId == fixture.TenantB.CompanyId).Select(item => item.Name).FirstAsync();
        var uniqueForeignName = foreignAreaName + " tenant-b-only";
        fixture.Db.OperationalAreas.Add(new OperationalArea { CompanyId = fixture.TenantB.CompanyId, Name = uniqueForeignName, AreaType = "Base" });
        await fixture.Db.SaveChangesAsync();
        var crossTenant = await PrepareAsync(fixture, actor, ImportTargetTypes.Vehicle, ("Registration", "IMP-X"), ("Area", uniqueForeignName));
        Assert((await fixture.Db.ImportRowResults.SingleAsync(item => item.ImportBatchId == crossTenant.Batch.Id)).ValidationStatus == ImportRowStatuses.Invalid,
            "Cross-tenant operational area resolved during import.");
    }

    private static async Task CommitCreateAsync(TenantFixture fixture, AppUser actor, string target, params (string Heading, string? Value)[] cells)
    {
        var prepared = await PrepareAsync(fixture, actor, target, cells);
        var result = await prepared.Workflow.CommitAsync(actor, prepared.Batch.Id);
        Assert(result.Created == 1, $"{target} import did not create exactly one record.");
        var replay = await prepared.Workflow.CommitAsync(actor, prepared.Batch.Id);
        Assert(replay.Created == 1, $"{target} commit replay was not idempotent.");
    }

    private static async Task<PreparedImport> PrepareAsync(TenantFixture fixture, AppUser actor, string target, params (string Heading, string? Value)[] cells)
    {
        var reader = new StaticReader(cells);
        var batchService = new ImportBatchService(fixture.Db, new UserActionPermissionService(fixture.Db));
        var file = new AssetFile
        {
            CompanyId = actor.CompanyId, UploadedByUserId = actor.Id, LinkedEntityType = SetupUploadService.RegisterUploadEntityType,
            Category = target, OriginalFileName = $"{target}.csv", ContentType = "text/csv", StorageProvider = "test",
            StoragePath = $"test/company-{actor.CompanyId}/{Guid.NewGuid():N}.csv", SizeBytes = 100
        };
        fixture.Db.AssetFiles.Add(file);
        var profile = new ImportSourceProfile(1, new string('A', 64), "CSV", 1, 2, cells.Length * 2,
            [new ImportWorksheetProfile("Sheet 1", 2, cells.Length, cells.Length * 2)]);
        var batch = await batchService.CreateUploadedBatchAsync(actor, file, target, profile, DateTime.UtcNow);
        await fixture.Db.SaveChangesAsync();
        var workflow = Workflow(fixture, reader);
        await workflow.SelectSourceAsync(actor, batch.Id, "Sheet 1", 1);
        batch = (await workflow.LoadAsync(actor, batch.Id))!;
        await workflow.SaveMappingsAsync(actor, batch.Id, batch.ColumnMappings.Select(mapping =>
            new ImportMappingInput(mapping.SourceColumnIndex, mapping.TargetFieldKey, mapping.TargetFieldKey is null)).ToList());
        await workflow.ValidateAsync(actor, batch.Id);
        return new PreparedImport((await workflow.LoadAsync(actor, batch.Id))!, workflow);
    }

    private static ImportRegisterWorkflowService Workflow(TenantFixture fixture, IImportTabularReader reader)
    {
        var batches = new ImportBatchService(fixture.Db, new UserActionPermissionService(fixture.Db));
        return new ImportRegisterWorkflowService(fixture.Db, batches, new ImportFieldRegistry(), reader);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed record PreparedImport(ImportBatch Batch, ImportRegisterWorkflowService Workflow);

    private sealed class StaticReader : IImportTabularReader
    {
        private readonly (string Heading, string? Value)[] _cells;
        public StaticReader(IEnumerable<(string Heading, string? Value)> cells) { _cells = cells.ToArray(); }
        public Task<ImportTabularData> ReadAsync(AssetFile sourceFile, string? worksheet, int headerRowNumber, CancellationToken cancellationToken = default)
        {
            var columns = _cells.Select((cell, index) => new ImportSourceColumn(index, cell.Heading, cell.Value is null ? [] : [cell.Value])).ToList();
            var rows = _cells.Length == 0 ? new List<ImportSourceRow>() :
                [new ImportSourceRow(2, _cells.Select((cell, index) => (index, cell.Value)).ToDictionary(item => item.index, item => item.Value))];
            return Task.FromResult(new ImportTabularData("Sheet 1", 1, columns, rows));
        }
    }
}
