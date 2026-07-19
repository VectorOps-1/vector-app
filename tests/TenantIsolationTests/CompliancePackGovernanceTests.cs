using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

internal static class CompliancePackGovernanceTests
{
    private const string MigrationId = "20260719120000_AddComplianceSourceRegistryFoundation";
    private static readonly (string Code, string Name)[] SouthAfricanProvinces =
    [
        ("ZA-EC", "Eastern Cape"),
        ("ZA-FS", "Free State"),
        ("ZA-GP", "Gauteng"),
        ("ZA-KZN", "KwaZulu-Natal"),
        ("ZA-LP", "Limpopo"),
        ("ZA-MP", "Mpumalanga"),
        ("ZA-NC", "Northern Cape"),
        ("ZA-NW", "North West"),
        ("ZA-WC", "Western Cape")
    ];

    public static async Task RunAllAsync()
    {
        await MigrationIsAdditiveAndProviderCompatibleAsync();
        await JurisdictionAndPackCompositionRemainSeparatedAsync();
        await GovernanceIsDefaultDenyAndLifecycleControlledAsync();
    }

    private static async Task MigrationIsAdditiveAndProviderCompatibleAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"acuityops-b61-{Guid.NewGuid():N}.db");
        try
        {
            var sqliteOptions = new DbContextOptionsBuilder<VectorDbContext>()
                .UseSqlite($"Data Source={path}")
                .Options;
            await using (var db = new VectorDbContext(sqliteOptions))
            {
                await db.Database.OpenConnectionAsync();
                await db.Database.ExecuteSqlRawAsync(
                    "CREATE TABLE __EFMigrationsHistory (MigrationId TEXT NOT NULL PRIMARY KEY, ProductVersion TEXT NOT NULL);");

                var migrations = db.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(item => item).ToArray();
                var previous = migrations.TakeWhile(item => item != MigrationId).Last();
                foreach (var migration in migrations.TakeWhile(item => item != MigrationId))
                {
                    await db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({0}, '8.0.5');",
                        migration);
                }

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(MigrationId);

                Ensure(await TableExistsAsync(db, "Jurisdictions"), "B6.1 migration did not create Jurisdictions.");
                Ensure(await TableExistsAsync(db, "ComplianceRequirementPackVersions"), "B6.1 migration did not create pack versions.");
                Ensure(await TableExistsAsync(db, "ComplianceRequirementSourceClauses"), "B6.1 migration did not create clause provenance links.");
                Ensure(await IndexExistsAsync(db, "IX_ComplianceRequirementPackVersions_ComplianceRequirementPackId_ActiveSlot"),
                    "B6.1 migration did not create the one-active-version index.");
                Ensure(await ForeignKeyCountAsync(db, "ComplianceRequirementSourceClauses") == 2,
                    "Clause provenance links do not have both restrictive foreign keys.");
                Ensure(await db.Jurisdictions.CountAsync() == 0, "B6.1 migration inserted jurisdictions.");
                Ensure(await db.ComplianceRequirementPacks.CountAsync() == 0, "B6.1 migration inserted requirement packs.");

                await migrator.MigrateAsync(previous);
                Ensure(!await TableExistsAsync(db, "Jurisdictions"), "B6.1 rollback left Jurisdictions behind.");
                Ensure(!await TableExistsAsync(db, "ComplianceRequirementPacks"), "B6.1 rollback left requirement packs behind.");
                await db.Database.CloseConnectionAsync();
            }

            var sqlServerOptions = new DbContextOptionsBuilder<VectorDbContext>()
                .UseSqlServer("Server=(local);Database=AcuityOpsScriptOnly;Integrated Security=true;TrustServerCertificate=true")
                .Options;
            await using var sqlServerDb = new VectorDbContext(sqlServerOptions);
            var sqlMigrations = sqlServerDb.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(item => item).ToArray();
            var sqlPrevious = sqlMigrations.TakeWhile(item => item != MigrationId).Last();
            var script = sqlServerDb.GetService<IMigrator>().GenerateScript(sqlPrevious, MigrationId);
            Ensure(script.Contains("CREATE TABLE [Jurisdictions]", StringComparison.Ordinal),
                "SQL Server migration script does not create Jurisdictions.");
            Ensure(script.Contains("CREATE TABLE [ComplianceRequirementPacks]", StringComparison.Ordinal),
                "SQL Server migration script does not create requirement packs.");
            Ensure(script.Contains("WHERE ActiveSlot IS NOT NULL", StringComparison.OrdinalIgnoreCase),
                "SQL Server migration script does not preserve the filtered active-version index.");
            Ensure(!script.Contains("INSERT INTO [Jurisdictions]", StringComparison.OrdinalIgnoreCase),
                "SQL Server migration script inserts regulatory data.");
            Ensure(!script.Contains("INSERT INTO [Companies]", StringComparison.OrdinalIgnoreCase),
                "SQL Server migration script inserts tenant data.");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static async Task JurisdictionAndPackCompositionRemainSeparatedAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<VectorDbContext>().UseSqlite(connection).Options;
        await using var db = new VectorDbContext(options);
        await db.Database.EnsureCreatedAsync();

        Ensure(await db.Jurisdictions.CountAsync() == 0, "The model creates jurisdictions automatically.");
        Ensure(await db.ComplianceRequirements.CountAsync() == 0, "The model creates compliance requirements automatically.");
        Ensure(await db.Companies.CountAsync() == 0, "The model creates tenant records automatically.");

        var southAfrica = new Jurisdiction
        {
            Code = "ZA",
            Name = "South Africa",
            Level = ComplianceJurisdictionLevels.Country,
            IsSelectable = true
        };
        db.Jurisdictions.Add(southAfrica);
        foreach (var province in SouthAfricanProvinces)
        {
            db.Jurisdictions.Add(new Jurisdiction
            {
                Code = province.Code,
                Name = province.Name,
                Level = ComplianceJurisdictionLevels.Province,
                ParentJurisdiction = southAfrica,
                IsSelectable = true
            });
        }

        await db.SaveChangesAsync();

        Ensure(await db.Jurisdictions.CountAsync(item => item.Level == ComplianceJurisdictionLevels.Province) == 9,
            "All nine South African provinces cannot be represented.");

        var gauteng = await db.Jurisdictions.SingleAsync(item => item.Code == "ZA-GP");
        var westernCape = await db.Jurisdictions.SingleAsync(item => item.Code == "ZA-WC");
        var district = new Jurisdiction
        {
            Code = "ZA-GP-JHB",
            Name = "Johannesburg District",
            Level = ComplianceJurisdictionLevels.District,
            ParentJurisdiction = gauteng,
            IsSelectable = false
        };
        var municipality = new Jurisdiction
        {
            Code = "ZA-GP-JHB-COJ",
            Name = "City of Johannesburg",
            Level = ComplianceJurisdictionLevels.Municipality,
            ParentJurisdiction = district,
            IsSelectable = false
        };
        db.Jurisdictions.AddRange(district, municipality);
        await db.SaveChangesAsync();
        Ensure(district.ParentJurisdictionId == gauteng.Id && municipality.ParentJurisdictionId == district.Id,
            "Country, province, district, and municipality hierarchy could not be represented.");

        await AddActivePackAsync(db, southAfrica, "ZA-NATIONAL", "South Africa national baseline");
        await AddActivePackAsync(db, gauteng, "ZA-GP-OVERLAY", "Gauteng overlay");
        await AddActivePackAsync(db, westernCape, "ZA-WC-OVERLAY", "Western Cape overlay");

        var reader = new ComplianceSourceRegistryReader(db);
        var composition = await reader.GetActivePackCompositionAsync("ZA", ["ZA-GP", "ZA-WC"]);
        Ensure(composition.IsAuthoritative, "Complete national and provincial packs were not reported as authoritative.");
        Ensure(composition.Jurisdictions.Count == 3, "National and two provincial jurisdictions were not returned separately.");
        Ensure(composition.Jurisdictions.SelectMany(item => item.ActivePacks).Select(item => item.PackCode).ToHashSet()
            .SetEquals(["ZA-NATIONAL", "ZA-GP-OVERLAY", "ZA-WC-OVERLAY"]),
            "National and provincial packs were silently merged or substituted.");

        var incomplete = await reader.GetActivePackCompositionAsync("ZA", ["ZA-EC"]);
        Ensure(!incomplete.IsAuthoritative, "An incomplete province produced an authoritative composition.");
        Ensure(incomplete.Jurisdictions.Single(item => item.JurisdictionCode == "ZA-EC").StatusLabel
            == ComplianceSourceRegistryReader.IncompleteStatusLabel,
            "An incomplete province was not exposed as Source pack incomplete.");

        var foreignCountry = new Jurisdiction
        {
            Code = "NA",
            Name = "Namibia",
            Level = ComplianceJurisdictionLevels.Country
        };
        var foreignProvince = new Jurisdiction
        {
            Code = "NA-KH",
            Name = "Khomas",
            Level = ComplianceJurisdictionLevels.Province,
            ParentJurisdiction = foreignCountry
        };
        db.Jurisdictions.AddRange(foreignCountry, foreignProvince);
        await db.SaveChangesAsync();
        await EnsureThrowsAsync<InvalidOperationException>(
            () => reader.GetActivePackCompositionAsync("ZA", ["NA-KH"]),
            "A province from another country was silently substituted.");
    }

    private static async Task GovernanceIsDefaultDenyAndLifecycleControlledAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<VectorDbContext>().UseSqlite(connection).Options;
        await using var db = new VectorDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var jurisdiction = new Jurisdiction
        {
            Code = "ZA",
            Name = "South Africa",
            Level = ComplianceJurisdictionLevels.Country
        };
        var pack = new ComplianceRequirementPack
        {
            PackCode = "TEST-PACK",
            Name = "Test pack",
            PackType = "NationalBaseline",
            PrimaryJurisdiction = jurisdiction
        };
        var deniedVersion = new ComplianceRequirementPackVersion
        {
            ComplianceRequirementPack = pack,
            VersionLabel = "denied",
            LifecycleState = ComplianceLifecycleStates.Draft
        };
        db.Add(deniedVersion);
        await db.SaveChangesAsync();

        var deniedService = new CompliancePackGovernanceService(db, new DenyProductComplianceAuthorization());
        await EnsureThrowsAsync<UnauthorizedAccessException>(
            () => deniedService.TransitionPackVersionAsync(deniedVersion.Id, ComplianceLifecycleStates.Acquired, "test"),
            "Default product compliance authorization allowed a governance write.");

        var allowedService = new CompliancePackGovernanceService(db, new AllowProductComplianceAuthorization());
        await EnsureThrowsAsync<InvalidOperationException>(
            () => allowedService.TransitionPackVersionAsync(deniedVersion.Id, ComplianceLifecycleStates.Active, "skip lifecycle"),
            "Governance allowed a lifecycle state to be skipped.");

        Ensure(ComplianceLifecycleStates.CanTransition(ComplianceLifecycleStates.Draft, ComplianceLifecycleStates.Acquired),
            "The required lifecycle cannot start.");
        Ensure(!ComplianceLifecycleStates.CanTransition(ComplianceLifecycleStates.Active, ComplianceLifecycleStates.Approved),
            "An active pack can be edited back into an approval state.");
        Ensure(!ComplianceLifecycleStates.CanTransition(ComplianceLifecycleStates.Superseded, ComplianceLifecycleStates.Active),
            "A superseded pack can be reactivated.");

        var regulator = new Regulator
        {
            Code = "TEST-REG",
            Name = "Test regulator",
            AuthorityType = "Government",
            Jurisdiction = jurisdiction
        };
        var source = new RegulatorySource
        {
            SourceCode = "TEST-SOURCE",
            OfficialTitle = "Test official source",
            Classification = ComplianceSourceClassifications.BindingRegulation,
            Regulator = regulator,
            Jurisdiction = jurisdiction,
            OfficialUrl = "https://example.invalid/source"
        };
        var sourceVersion = new RegulatorySourceVersion
        {
            RegulatorySource = source,
            VersionLabel = "1",
            LifecycleState = ComplianceLifecycleStates.Approved,
            OfficialUrl = source.OfficialUrl,
            StoredArtifactReference = "product/compliance/test-source-v1.pdf",
            ContentHashSha256 = new string('A', 64)
        };
        var clause = new RegulatoryClause
        {
            RegulatorySourceVersion = sourceVersion,
            ClauseCode = "1.1",
            ExactText = "Verified source clause.",
            IsVerified = true,
            VerifiedAtUtc = DateTime.UtcNow,
            VerifiedBy = "product-reviewer"
        };
        var versionOne = BuildApprovedPackVersion(pack, "1", sourceVersion, clause);
        var versionTwo = BuildApprovedPackVersion(pack, "2", sourceVersion, clause);
        db.AddRange(versionOne, versionTwo);
        await db.SaveChangesAsync();

        await allowedService.TransitionPackVersionAsync(versionOne.Id, ComplianceLifecycleStates.Active, "activate v1");
        await allowedService.TransitionPackVersionAsync(versionTwo.Id, ComplianceLifecycleStates.Active, "activate v2");
        await db.Entry(versionOne).ReloadAsync();
        await db.Entry(versionTwo).ReloadAsync();

        Ensure(versionOne.LifecycleState == ComplianceLifecycleStates.Superseded && versionOne.ActiveSlot is null,
            "Activating a replacement did not supersede the prior active pack version.");
        Ensure(versionTwo.LifecycleState == ComplianceLifecycleStates.Active && versionTwo.ActiveSlot == 1,
            "The replacement pack did not become the only active version.");
        Ensure(await db.ComplianceRequirementPackVersions.CountAsync(item => item.ComplianceRequirementPackId == pack.Id
            && item.LifecycleState == ComplianceLifecycleStates.Active) == 1,
            "More than one pack version is active.");
        Ensure(versionTwo.Requirements.Single().SourceClauses.Single().RegulatoryClauseId == clause.Id,
            "Requirement provenance was not retained at clause level.");

        await EnsureThrowsAsync<InvalidOperationException>(
            () => allowedService.TransitionPackVersionAsync(versionTwo.Id, ComplianceLifecycleStates.Approved, "attempt mutation"),
            "An active pack was mutable outside supersede/withdraw lifecycle actions.");

        var conflicted = BuildApprovedPackVersion(pack, "3", sourceVersion, clause);
        conflicted.LifecycleState = ComplianceLifecycleStates.Approved;
        conflicted.ConflictNote = "Unresolved conflict";
        db.Add(conflicted);
        await db.SaveChangesAsync();
        await EnsureThrowsAsync<InvalidOperationException>(
            () => allowedService.TransitionPackVersionAsync(conflicted.Id, ComplianceLifecycleStates.Active, "conflicted"),
            "A conflicted pack was activated.");

        Ensure(await db.ComplianceGovernanceEvents.CountAsync() >= 3,
            "Governance lifecycle writes were not recorded.");
    }

    private static ComplianceRequirementPackVersion BuildApprovedPackVersion(
        ComplianceRequirementPack pack,
        string versionLabel,
        RegulatorySourceVersion sourceVersion,
        RegulatoryClause clause)
    {
        var version = new ComplianceRequirementPackVersion
        {
            ComplianceRequirementPack = pack,
            VersionLabel = versionLabel,
            LifecycleState = ComplianceLifecycleStates.Approved,
            SourceCompletenessState = ComplianceSourceCompletenessStates.Complete,
            ContentHashSha256 = new string('B', 64)
        };
        version.Sources.Add(new ComplianceRequirementPackSource
        {
            RegulatorySourceVersion = sourceVersion,
            Purpose = "Authoritative basis"
        });
        var requirement = new ComplianceRequirement
        {
            RequirementCode = $"REQ-{versionLabel}",
            Title = "Verified requirement",
            PlainEnglishRequirement = "Maintain verified evidence.",
            Domain = "Governance",
            Classification = "Mandatory",
            Priority = CompliancePriorities.P1
        };
        requirement.SourceClauses.Add(new ComplianceRequirementSourceClause
        {
            RegulatoryClause = clause,
            RelationshipType = "Creates"
        });
        version.Requirements.Add(requirement);
        version.Reviews.Add(new ComplianceRuleReview
        {
            ReviewType = ComplianceReviewTypes.Legal,
            Decision = ComplianceReviewDecisions.Approved,
            ReviewerName = "Legal reviewer"
        });
        version.Reviews.Add(new ComplianceRuleReview
        {
            ReviewType = ComplianceReviewTypes.Operational,
            Decision = ComplianceReviewDecisions.Approved,
            ReviewerName = "Operational reviewer"
        });
        return version;
    }

    private static async Task AddActivePackAsync(
        VectorDbContext db,
        Jurisdiction jurisdiction,
        string packCode,
        string packName)
    {
        var pack = new ComplianceRequirementPack
        {
            PackCode = packCode,
            Name = packName,
            PackType = jurisdiction.Level == ComplianceJurisdictionLevels.Country ? "NationalBaseline" : "ProvincialOverlay",
            PrimaryJurisdiction = jurisdiction
        };
        var regulator = new Regulator
        {
            Code = $"REG-{packCode}",
            Name = $"Regulator for {packName}",
            AuthorityType = "Government",
            Jurisdiction = jurisdiction
        };
        var source = new RegulatorySource
        {
            SourceCode = $"SRC-{packCode}",
            OfficialTitle = $"Official source for {packName}",
            Classification = ComplianceSourceClassifications.BindingRegulation,
            Regulator = regulator,
            Jurisdiction = jurisdiction,
            OfficialUrl = "https://example.invalid/official-source"
        };
        var sourceVersion = new RegulatorySourceVersion
        {
            RegulatorySource = source,
            VersionLabel = "1",
            LifecycleState = ComplianceLifecycleStates.Approved,
            OfficialUrl = source.OfficialUrl,
            StoredArtifactReference = $"product/compliance/{packCode}.pdf",
            ContentHashSha256 = new string('C', 64)
        };
        var clause = new RegulatoryClause
        {
            RegulatorySourceVersion = sourceVersion,
            ClauseCode = "1",
            ExactText = "Verified source clause.",
            IsVerified = true,
            VerifiedAtUtc = DateTime.UtcNow,
            VerifiedBy = "product-reviewer"
        };
        var version = new ComplianceRequirementPackVersion
        {
            VersionLabel = "1",
            LifecycleState = ComplianceLifecycleStates.Active,
            SourceCompletenessState = ComplianceSourceCompletenessStates.Complete,
            ActiveSlot = 1,
            ActivatedAtUtc = DateTime.UtcNow
        };
        version.Sources.Add(new ComplianceRequirementPackSource
        {
            RegulatorySourceVersion = sourceVersion,
            Purpose = "Authoritative basis"
        });
        var requirement = new ComplianceRequirement
        {
            RequirementCode = "REQ-1",
            Title = "Verified requirement",
            PlainEnglishRequirement = "Maintain verified evidence.",
            Domain = "Governance",
            Classification = "Mandatory",
            Priority = CompliancePriorities.P1
        };
        requirement.SourceClauses.Add(new ComplianceRequirementSourceClause
        {
            RegulatoryClause = clause,
            RelationshipType = "Creates"
        });
        version.Requirements.Add(requirement);
        version.Reviews.Add(new ComplianceRuleReview
        {
            ReviewType = ComplianceReviewTypes.Legal,
            Decision = ComplianceReviewDecisions.Approved,
            ReviewerName = "Legal reviewer"
        });
        version.Reviews.Add(new ComplianceRuleReview
        {
            ReviewType = ComplianceReviewTypes.Operational,
            Decision = ComplianceReviewDecisions.Approved,
            ReviewerName = "Operational reviewer"
        });
        pack.Versions.Add(version);
        db.Add(pack);
        await db.SaveChangesAsync();
    }

    private static async Task<bool> TableExistsAsync(VectorDbContext db, string tableName)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task<bool> IndexExistsAsync(VectorDbContext db, string indexName)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name=$name;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = indexName;
        command.Parameters.Add(parameter);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task<int> ForeignKeyCountAsync(VectorDbContext db, string tableName)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list('{tableName.Replace("'", "''", StringComparison.Ordinal)}');";
        await using var reader = await command.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            count++;
        }
        return count;
    }

    private static async Task EnsureThrowsAsync<TException>(Func<Task> action, string message)
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

        throw new InvalidOperationException(message);
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class AllowProductComplianceAuthorization : IProductComplianceAuthorization
    {
        public Task<ProductComplianceAuthorizationResult> AuthorizeAsync(
            string operation,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProductComplianceAuthorizationResult(true, "product-test-reviewer"));
        }
    }
}
