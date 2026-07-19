using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260719120000_AddComplianceSourceRegistryFoundation")]
public partial class AddComplianceSourceRegistryFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Jurisdictions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>(maxLength: 32, nullable: false),
                Name = table.Column<string>(maxLength: 160, nullable: false),
                Level = table.Column<string>(maxLength: 40, nullable: false),
                ParentJurisdictionId = table.Column<int>(nullable: true),
                IsSelectable = table.Column<bool>(nullable: false),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Jurisdictions", x => x.Id);
                table.ForeignKey("FK_Jurisdictions_Jurisdictions_ParentJurisdictionId", x => x.ParentJurisdictionId, "Jurisdictions", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ComplianceGovernanceEvents",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                EntityType = table.Column<string>(maxLength: 80, nullable: false),
                EntityId = table.Column<int>(nullable: false),
                EventType = table.Column<string>(maxLength: 80, nullable: false),
                ActorIdentifier = table.Column<string>(maxLength: 120, nullable: false),
                FromState = table.Column<string>(maxLength: 40, nullable: true),
                ToState = table.Column<string>(maxLength: 40, nullable: true),
                Reason = table.Column<string>(maxLength: 2000, nullable: false),
                PayloadHashSha256 = table.Column<string>(maxLength: 64, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ComplianceGovernanceEvents", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Regulators",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>(maxLength: 80, nullable: false),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                AuthorityType = table.Column<string>(maxLength: 80, nullable: false),
                JurisdictionId = table.Column<int>(nullable: false),
                OfficialWebsiteUrl = table.Column<string>(maxLength: 1000, nullable: true),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Regulators", x => x.Id);
                table.ForeignKey("FK_Regulators_Jurisdictions_JurisdictionId", x => x.JurisdictionId, "Jurisdictions", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ComplianceRequirementPacks",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                PackCode = table.Column<string>(maxLength: 100, nullable: false),
                Name = table.Column<string>(maxLength: 240, nullable: false),
                Description = table.Column<string>(maxLength: 2000, nullable: true),
                PrimaryJurisdictionId = table.Column<int>(nullable: false),
                PackType = table.Column<string>(maxLength: 80, nullable: false),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplianceRequirementPacks", x => x.Id);
                table.ForeignKey("FK_ComplianceRequirementPacks_Jurisdictions_PrimaryJurisdictionId", x => x.PrimaryJurisdictionId, "Jurisdictions", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "RegulatorySources",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                SourceCode = table.Column<string>(maxLength: 100, nullable: false),
                OfficialTitle = table.Column<string>(maxLength: 500, nullable: false),
                Classification = table.Column<string>(maxLength: 80, nullable: false),
                RegulatorId = table.Column<int>(nullable: false),
                JurisdictionId = table.Column<int>(nullable: false),
                DocumentIdentifier = table.Column<string>(maxLength: 200, nullable: true),
                GazetteNumber = table.Column<string>(maxLength: 120, nullable: true),
                NoticeNumber = table.Column<string>(maxLength: 120, nullable: true),
                OfficialUrl = table.Column<string>(maxLength: 1000, nullable: false),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RegulatorySources", x => x.Id);
                table.ForeignKey("FK_RegulatorySources_Regulators_RegulatorId", x => x.RegulatorId, "Regulators", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_RegulatorySources_Jurisdictions_JurisdictionId", x => x.JurisdictionId, "Jurisdictions", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ComplianceRequirementPackVersions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                ComplianceRequirementPackId = table.Column<int>(nullable: false),
                VersionLabel = table.Column<string>(maxLength: 100, nullable: false),
                LifecycleState = table.Column<string>(maxLength: 40, nullable: false),
                SourceCompletenessState = table.Column<string>(maxLength: 40, nullable: false),
                EffectiveFrom = table.Column<DateTime>(nullable: true),
                EffectiveTo = table.Column<DateTime>(nullable: true),
                ActivatedAtUtc = table.Column<DateTime>(nullable: true),
                SupersededAtUtc = table.Column<DateTime>(nullable: true),
                ActiveSlot = table.Column<int>(nullable: true),
                ContentHashSha256 = table.Column<string>(maxLength: 64, nullable: true),
                LimitationsNote = table.Column<string>(maxLength: 2000, nullable: true),
                ConflictNote = table.Column<string>(maxLength: 2000, nullable: true),
                ConcurrencyToken = table.Column<string>(maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplianceRequirementPackVersions", x => x.Id);
                table.ForeignKey("FK_ComplianceRequirementPackVersions_ComplianceRequirementPacks_ComplianceRequirementPackId", x => x.ComplianceRequirementPackId, "ComplianceRequirementPacks", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "RegulatorySourceVersions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                RegulatorySourceId = table.Column<int>(nullable: false),
                VersionLabel = table.Column<string>(maxLength: 100, nullable: false),
                PublicationDate = table.Column<DateTime>(nullable: true),
                EffectiveFrom = table.Column<DateTime>(nullable: true),
                EffectiveTo = table.Column<DateTime>(nullable: true),
                SupersededAtUtc = table.Column<DateTime>(nullable: true),
                AcquiredAtUtc = table.Column<DateTime>(nullable: false),
                OfficialUrl = table.Column<string>(maxLength: 1000, nullable: false),
                StoredArtifactReference = table.Column<string>(maxLength: 500, nullable: false),
                ContentHashSha256 = table.Column<string>(maxLength: 64, nullable: false),
                LifecycleState = table.Column<string>(maxLength: 40, nullable: false),
                UncertaintyNote = table.Column<string>(maxLength: 2000, nullable: true),
                ConflictNote = table.Column<string>(maxLength: 2000, nullable: true),
                ConcurrencyToken = table.Column<string>(maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RegulatorySourceVersions", x => x.Id);
                table.ForeignKey("FK_RegulatorySourceVersions_RegulatorySources_RegulatorySourceId", x => x.RegulatorySourceId, "RegulatorySources", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ComplianceRequirements",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                ComplianceRequirementPackVersionId = table.Column<int>(nullable: false),
                RequirementCode = table.Column<string>(maxLength: 120, nullable: false),
                Title = table.Column<string>(maxLength: 240, nullable: false),
                PlainEnglishRequirement = table.Column<string>(nullable: false),
                Domain = table.Column<string>(maxLength: 120, nullable: false),
                Classification = table.Column<string>(maxLength: 80, nullable: false),
                Priority = table.Column<string>(maxLength: 8, nullable: false),
                IsPotentialBlocker = table.Column<bool>(nullable: false),
                ConsequenceText = table.Column<string>(maxLength: 2000, nullable: true),
                CorrectiveActionText = table.Column<string>(maxLength: 2000, nullable: true),
                UncertaintyNote = table.Column<string>(maxLength: 2000, nullable: true),
                ConflictNote = table.Column<string>(maxLength: 2000, nullable: true),
                SortOrder = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplianceRequirements", x => x.Id);
                table.ForeignKey("FK_ComplianceRequirements_ComplianceRequirementPackVersions_ComplianceRequirementPackVersionId", x => x.ComplianceRequirementPackVersionId, "ComplianceRequirementPackVersions", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "RegulatoryClauses",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                RegulatorySourceVersionId = table.Column<int>(nullable: false),
                ClauseCode = table.Column<string>(maxLength: 160, nullable: false),
                PageReference = table.Column<string>(maxLength: 160, nullable: true),
                Heading = table.Column<string>(maxLength: 500, nullable: true),
                ExactText = table.Column<string>(nullable: false),
                IsVerified = table.Column<bool>(nullable: false),
                VerifiedAtUtc = table.Column<DateTime>(nullable: true),
                VerifiedBy = table.Column<string>(maxLength: 240, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RegulatoryClauses", x => x.Id);
                table.ForeignKey("FK_RegulatoryClauses_RegulatorySourceVersions_RegulatorySourceVersionId", x => x.RegulatorySourceVersionId, "RegulatorySourceVersions", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ComplianceApplicabilityRules",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                ComplianceRequirementId = table.Column<int>(nullable: false),
                GroupNumber = table.Column<int>(nullable: false),
                SortOrder = table.Column<int>(nullable: false),
                IsExclusion = table.Column<bool>(nullable: false),
                JurisdictionId = table.Column<int>(nullable: true),
                OperatorType = table.Column<string>(maxLength: 100, nullable: true),
                ServiceCategory = table.Column<string>(maxLength: 100, nullable: true),
                LicenceCategory = table.Column<string>(maxLength: 100, nullable: true),
                ClinicalCapability = table.Column<string>(maxLength: 100, nullable: true),
                ObjectType = table.Column<string>(maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplianceApplicabilityRules", x => x.Id);
                table.ForeignKey("FK_ComplianceApplicabilityRules_ComplianceRequirements_ComplianceRequirementId", x => x.ComplianceRequirementId, "ComplianceRequirements", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ComplianceApplicabilityRules_Jurisdictions_JurisdictionId", x => x.JurisdictionId, "Jurisdictions", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ComplianceEvidenceDefinitions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                ComplianceRequirementId = table.Column<int>(nullable: false),
                EvidenceCode = table.Column<string>(maxLength: 120, nullable: false),
                EvidenceType = table.Column<string>(maxLength: 120, nullable: false),
                ObjectType = table.Column<string>(maxLength: 100, nullable: true),
                Description = table.Column<string>(maxLength: 2000, nullable: false),
                VerificationMethod = table.Column<string>(maxLength: 2000, nullable: false),
                IsRequired = table.Column<bool>(nullable: false),
                MinimumCount = table.Column<int>(nullable: false),
                MaximumAgeDays = table.Column<int>(nullable: true),
                RequiresIndependentVerification = table.Column<bool>(nullable: false),
                SortOrder = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplianceEvidenceDefinitions", x => x.Id);
                table.ForeignKey("FK_ComplianceEvidenceDefinitions_ComplianceRequirements_ComplianceRequirementId", x => x.ComplianceRequirementId, "ComplianceRequirements", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ComplianceRequirementPackSources",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                ComplianceRequirementPackVersionId = table.Column<int>(nullable: false),
                RegulatorySourceVersionId = table.Column<int>(nullable: false),
                Purpose = table.Column<string>(maxLength: 120, nullable: false),
                IsMandatory = table.Column<bool>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplianceRequirementPackSources", x => x.Id);
                table.ForeignKey("FK_ComplianceRequirementPackSources_ComplianceRequirementPackVersions_ComplianceRequirementPackVersionId", x => x.ComplianceRequirementPackVersionId, "ComplianceRequirementPackVersions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ComplianceRequirementPackSources_RegulatorySourceVersions_RegulatorySourceVersionId", x => x.RegulatorySourceVersionId, "RegulatorySourceVersions", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ComplianceRuleReviews",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                RegulatorySourceVersionId = table.Column<int>(nullable: true),
                ComplianceRequirementPackVersionId = table.Column<int>(nullable: true),
                ComplianceRequirementId = table.Column<int>(nullable: true),
                ReviewType = table.Column<string>(maxLength: 80, nullable: false),
                Decision = table.Column<string>(maxLength: 40, nullable: false),
                ReviewerName = table.Column<string>(maxLength: 240, nullable: false),
                ReviewerOrganization = table.Column<string>(maxLength: 240, nullable: true),
                ReviewerCredential = table.Column<string>(maxLength: 500, nullable: true),
                DecisionNote = table.Column<string>(maxLength: 2000, nullable: true),
                EvidenceReference = table.Column<string>(maxLength: 500, nullable: true),
                SignatureHashSha256 = table.Column<string>(maxLength: 64, nullable: true),
                ReviewedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplianceRuleReviews", x => x.Id);
                table.CheckConstraint("CK_ComplianceRuleReviews_ExactlyOneSubject", "((RegulatorySourceVersionId IS NOT NULL AND ComplianceRequirementPackVersionId IS NULL AND ComplianceRequirementId IS NULL) OR (RegulatorySourceVersionId IS NULL AND ComplianceRequirementPackVersionId IS NOT NULL AND ComplianceRequirementId IS NULL) OR (RegulatorySourceVersionId IS NULL AND ComplianceRequirementPackVersionId IS NULL AND ComplianceRequirementId IS NOT NULL))");
                table.ForeignKey("FK_ComplianceRuleReviews_RegulatorySourceVersions_RegulatorySourceVersionId", x => x.RegulatorySourceVersionId, "RegulatorySourceVersions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ComplianceRuleReviews_ComplianceRequirementPackVersions_ComplianceRequirementPackVersionId", x => x.ComplianceRequirementPackVersionId, "ComplianceRequirementPackVersions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ComplianceRuleReviews_ComplianceRequirements_ComplianceRequirementId", x => x.ComplianceRequirementId, "ComplianceRequirements", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ComplianceRequirementSourceClauses",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                ComplianceRequirementId = table.Column<int>(nullable: false),
                RegulatoryClauseId = table.Column<int>(nullable: false),
                RelationshipType = table.Column<string>(maxLength: 40, nullable: false),
                Note = table.Column<string>(maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplianceRequirementSourceClauses", x => x.Id);
                table.ForeignKey("FK_ComplianceRequirementSourceClauses_ComplianceRequirements_ComplianceRequirementId", x => x.ComplianceRequirementId, "ComplianceRequirements", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ComplianceRequirementSourceClauses_RegulatoryClauses_RegulatoryClauseId", x => x.RegulatoryClauseId, "RegulatoryClauses", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_Jurisdictions_Code", "Jurisdictions", "Code", unique: true);
        migrationBuilder.CreateIndex("IX_Jurisdictions_ParentJurisdictionId_Level_Name", "Jurisdictions", new[] { "ParentJurisdictionId", "Level", "Name" });
        migrationBuilder.CreateIndex("IX_Regulators_Code", "Regulators", "Code", unique: true);
        migrationBuilder.CreateIndex("IX_Regulators_JurisdictionId", "Regulators", "JurisdictionId");
        migrationBuilder.CreateIndex("IX_RegulatorySources_SourceCode", "RegulatorySources", "SourceCode", unique: true);
        migrationBuilder.CreateIndex("IX_RegulatorySources_RegulatorId", "RegulatorySources", "RegulatorId");
        migrationBuilder.CreateIndex("IX_RegulatorySources_JurisdictionId", "RegulatorySources", "JurisdictionId");
        migrationBuilder.CreateIndex("IX_RegulatorySourceVersions_RegulatorySourceId_VersionLabel", "RegulatorySourceVersions", new[] { "RegulatorySourceId", "VersionLabel" }, unique: true);
        migrationBuilder.CreateIndex("IX_RegulatorySourceVersions_LifecycleState", "RegulatorySourceVersions", "LifecycleState");
        migrationBuilder.CreateIndex("IX_RegulatoryClauses_RegulatorySourceVersionId_ClauseCode", "RegulatoryClauses", new[] { "RegulatorySourceVersionId", "ClauseCode" }, unique: true);
        migrationBuilder.CreateIndex("IX_ComplianceRequirementPacks_PackCode", "ComplianceRequirementPacks", "PackCode", unique: true);
        migrationBuilder.CreateIndex("IX_ComplianceRequirementPacks_PrimaryJurisdictionId", "ComplianceRequirementPacks", "PrimaryJurisdictionId");
        migrationBuilder.CreateIndex("IX_ComplianceRequirementPackVersions_ComplianceRequirementPackId_VersionLabel", "ComplianceRequirementPackVersions", new[] { "ComplianceRequirementPackId", "VersionLabel" }, unique: true);
        migrationBuilder.CreateIndex("IX_ComplianceRequirementPackVersions_ComplianceRequirementPackId_ActiveSlot", "ComplianceRequirementPackVersions", new[] { "ComplianceRequirementPackId", "ActiveSlot" }, unique: true, filter: "ActiveSlot IS NOT NULL");
        migrationBuilder.CreateIndex("IX_ComplianceRequirementPackVersions_LifecycleState_SourceCompletenessState", "ComplianceRequirementPackVersions", new[] { "LifecycleState", "SourceCompletenessState" });
        migrationBuilder.CreateIndex("IX_ComplianceRequirements_ComplianceRequirementPackVersionId_RequirementCode", "ComplianceRequirements", new[] { "ComplianceRequirementPackVersionId", "RequirementCode" }, unique: true);
        migrationBuilder.CreateIndex("IX_ComplianceApplicabilityRules_ComplianceRequirementId_GroupNumber_SortOrder", "ComplianceApplicabilityRules", new[] { "ComplianceRequirementId", "GroupNumber", "SortOrder" }, unique: true);
        migrationBuilder.CreateIndex("IX_ComplianceApplicabilityRules_JurisdictionId", "ComplianceApplicabilityRules", "JurisdictionId");
        migrationBuilder.CreateIndex("IX_ComplianceEvidenceDefinitions_ComplianceRequirementId_EvidenceCode", "ComplianceEvidenceDefinitions", new[] { "ComplianceRequirementId", "EvidenceCode" }, unique: true);
        migrationBuilder.CreateIndex("IX_ComplianceRequirementPackSources_ComplianceRequirementPackVersionId_RegulatorySourceVersionId", "ComplianceRequirementPackSources", new[] { "ComplianceRequirementPackVersionId", "RegulatorySourceVersionId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ComplianceRequirementPackSources_RegulatorySourceVersionId", "ComplianceRequirementPackSources", "RegulatorySourceVersionId");
        migrationBuilder.CreateIndex("IX_ComplianceRuleReviews_RegulatorySourceVersionId_ReviewType_ReviewedAtUtc", "ComplianceRuleReviews", new[] { "RegulatorySourceVersionId", "ReviewType", "ReviewedAtUtc" });
        migrationBuilder.CreateIndex("IX_ComplianceRuleReviews_ComplianceRequirementPackVersionId_ReviewType_ReviewedAtUtc", "ComplianceRuleReviews", new[] { "ComplianceRequirementPackVersionId", "ReviewType", "ReviewedAtUtc" });
        migrationBuilder.CreateIndex("IX_ComplianceRuleReviews_ComplianceRequirementId_ReviewType_ReviewedAtUtc", "ComplianceRuleReviews", new[] { "ComplianceRequirementId", "ReviewType", "ReviewedAtUtc" });
        migrationBuilder.CreateIndex("IX_ComplianceRequirementSourceClauses_ComplianceRequirementId_RegulatoryClauseId", "ComplianceRequirementSourceClauses", new[] { "ComplianceRequirementId", "RegulatoryClauseId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ComplianceRequirementSourceClauses_RegulatoryClauseId", "ComplianceRequirementSourceClauses", "RegulatoryClauseId");
        migrationBuilder.CreateIndex("IX_ComplianceGovernanceEvents_EntityType_EntityId_CreatedAtUtc", "ComplianceGovernanceEvents", new[] { "EntityType", "EntityId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ComplianceApplicabilityRules");
        migrationBuilder.DropTable("ComplianceEvidenceDefinitions");
        migrationBuilder.DropTable("ComplianceGovernanceEvents");
        migrationBuilder.DropTable("ComplianceRequirementPackSources");
        migrationBuilder.DropTable("ComplianceRequirementSourceClauses");
        migrationBuilder.DropTable("ComplianceRuleReviews");
        migrationBuilder.DropTable("RegulatoryClauses");
        migrationBuilder.DropTable("ComplianceRequirements");
        migrationBuilder.DropTable("RegulatorySourceVersions");
        migrationBuilder.DropTable("ComplianceRequirementPackVersions");
        migrationBuilder.DropTable("RegulatorySources");
        migrationBuilder.DropTable("ComplianceRequirementPacks");
        migrationBuilder.DropTable("Regulators");
        migrationBuilder.DropTable("Jurisdictions");
    }
}
