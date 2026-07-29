using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260729120000_AddPremiumAiImportGovernance")]
public partial class AddPremiumAiImportGovernance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AiProcessingJobs",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                RequestedByUserId = table.Column<int>(nullable: false),
                ImportBatchId = table.Column<int>(nullable: false),
                FeatureKey = table.Column<string>(maxLength: 80, nullable: false),
                SourceType = table.Column<string>(maxLength: 80, nullable: false),
                InputHash = table.Column<string>(maxLength: 128, nullable: false),
                Provider = table.Column<string>(maxLength: 80, nullable: false),
                Deployment = table.Column<string>(maxLength: 120, nullable: false),
                Model = table.Column<string>(maxLength: 120, nullable: false),
                PromptVersion = table.Column<string>(maxLength: 80, nullable: false),
                SchemaVersion = table.Column<string>(maxLength: 80, nullable: false),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                AttemptCount = table.Column<int>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                StartedAtUtc = table.Column<DateTime>(nullable: true),
                CompletedAtUtc = table.Column<DateTime>(nullable: true),
                FailureCode = table.Column<string>(maxLength: 80, nullable: true),
                FailureSummary = table.Column<string>(maxLength: 1200, nullable: true),
                CorrelationId = table.Column<string>(maxLength: 80, nullable: false),
                ConcurrencyToken = table.Column<string>(maxLength: 36, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiProcessingJobs", x => x.Id);
                table.ForeignKey("FK_AiProcessingJobs_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiProcessingJobs_AppUsers_RequestedByUserId", x => x.RequestedByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiProcessingJobs_ImportBatches_ImportBatchId", x => x.ImportBatchId, "ImportBatches", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CompanyAiUsagePolicies",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                EnabledFeaturesJson = table.Column<string>(nullable: false, defaultValue: "[]"),
                MonthlySoftLimitUsd = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                MonthlyHardLimitUsd = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                PerJobLimitUsd = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                MaxConcurrentJobs = table.Column<int>(nullable: false),
                AllowHighCapabilityModel = table.Column<bool>(nullable: false),
                ChangedByUserId = table.Column<int>(nullable: false),
                ChangedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyAiUsagePolicies", x => x.Id);
                table.ForeignKey("FK_CompanyAiUsagePolicies_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_CompanyAiUsagePolicies_AppUsers_ChangedByUserId", x => x.ChangedByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AiJobAttempts",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                AiProcessingJobId = table.Column<int>(nullable: false),
                AttemptNumber = table.Column<int>(nullable: false),
                ProviderRequestId = table.Column<string>(maxLength: 120, nullable: true),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                InputTokens = table.Column<int>(nullable: false),
                OutputTokens = table.Column<int>(nullable: false),
                EstimatedCostUsd = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                FailureCode = table.Column<string>(maxLength: 80, nullable: true),
                FailureSummary = table.Column<string>(maxLength: 1200, nullable: true),
                StartedAtUtc = table.Column<DateTime>(nullable: false),
                CompletedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiJobAttempts", x => x.Id);
                table.ForeignKey("FK_AiJobAttempts_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiJobAttempts_AiProcessingJobs_AiProcessingJobId", x => x.AiProcessingJobId, "AiProcessingJobs", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AiSuggestionSets",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                AiProcessingJobId = table.Column<int>(nullable: false),
                TargetType = table.Column<string>(maxLength: 80, nullable: false),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                Confidence = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                WarningsJson = table.Column<string>(nullable: false, defaultValue: "[]"),
                ReviewedByUserId = table.Column<int>(nullable: true),
                ReviewedAtUtc = table.Column<DateTime>(nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiSuggestionSets", x => x.Id);
                table.ForeignKey("FK_AiSuggestionSets_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiSuggestionSets_AiProcessingJobs_AiProcessingJobId", x => x.AiProcessingJobId, "AiProcessingJobs", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiSuggestionSets_AppUsers_ReviewedByUserId", x => x.ReviewedByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AiUsageLedgers",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                AiProcessingJobId = table.Column<int>(nullable: false),
                FeatureKey = table.Column<string>(maxLength: 80, nullable: false),
                Provider = table.Column<string>(maxLength: 80, nullable: false),
                Model = table.Column<string>(maxLength: 120, nullable: false),
                InputTokens = table.Column<int>(nullable: false),
                OutputTokens = table.Column<int>(nullable: false),
                EstimatedCostUsd = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                RecordedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiUsageLedgers", x => x.Id);
                table.ForeignKey("FK_AiUsageLedgers_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiUsageLedgers_AiProcessingJobs_AiProcessingJobId", x => x.AiProcessingJobId, "AiProcessingJobs", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AiSuggestions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                AiSuggestionSetId = table.Column<int>(nullable: false),
                Kind = table.Column<string>(maxLength: 80, nullable: false),
                SourceLocator = table.Column<string>(maxLength: 300, nullable: false),
                TargetKey = table.Column<string>(maxLength: 160, nullable: true),
                ProposedValueJson = table.Column<string>(nullable: false),
                Confidence = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                Explanation = table.Column<string>(maxLength: 1200, nullable: false),
                WarningCodesJson = table.Column<string>(nullable: false, defaultValue: "[]"),
                SortOrder = table.Column<int>(nullable: false),
                Status = table.Column<string>(maxLength: 40, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiSuggestions", x => x.Id);
                table.ForeignKey("FK_AiSuggestions_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiSuggestions_AiSuggestionSets_AiSuggestionSetId", x => x.AiSuggestionSetId, "AiSuggestionSets", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AiImportProposals",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                ImportBatchId = table.Column<int>(nullable: false),
                AiSuggestionSetId = table.Column<int>(nullable: false),
                ProposedDomain = table.Column<string>(maxLength: 80, nullable: true),
                ProposedChecklistLayout = table.Column<string>(maxLength: 80, nullable: true),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiImportProposals", x => x.Id);
                table.ForeignKey("FK_AiImportProposals_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiImportProposals_ImportBatches_ImportBatchId", x => x.ImportBatchId, "ImportBatches", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiImportProposals_AiSuggestionSets_AiSuggestionSetId", x => x.AiSuggestionSetId, "AiSuggestionSets", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AiChecklistStructureProposals",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                ImportBatchId = table.Column<int>(nullable: false),
                AiSuggestionSetId = table.Column<int>(nullable: false),
                ProposedName = table.Column<string>(maxLength: 160, nullable: false),
                StructureJson = table.Column<string>(nullable: false),
                SourceCitationJson = table.Column<string>(nullable: false, defaultValue: "[]"),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiChecklistStructureProposals", x => x.Id);
                table.ForeignKey("FK_AiChecklistStructureProposals_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiChecklistStructureProposals_ImportBatches_ImportBatchId", x => x.ImportBatchId, "ImportBatches", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiChecklistStructureProposals_AiSuggestionSets_AiSuggestionSetId", x => x.AiSuggestionSetId, "AiSuggestionSets", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AiHumanDecisions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                AiSuggestionId = table.Column<int>(nullable: false),
                Decision = table.Column<string>(maxLength: 40, nullable: false),
                CorrectedValueJson = table.Column<string>(nullable: true),
                ReviewNote = table.Column<string>(maxLength: 1200, nullable: true),
                ReviewedByUserId = table.Column<int>(nullable: false),
                ReviewedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiHumanDecisions", x => x.Id);
                table.ForeignKey("FK_AiHumanDecisions_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiHumanDecisions_AiSuggestions_AiSuggestionId", x => x.AiSuggestionId, "AiSuggestions", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiHumanDecisions_AppUsers_ReviewedByUserId", x => x.ReviewedByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AiImportColumnProposals",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                AiImportProposalId = table.Column<int>(nullable: false),
                SourceColumnKey = table.Column<string>(maxLength: 160, nullable: false),
                CanonicalFieldKey = table.Column<string>(maxLength: 160, nullable: true),
                ProposedTransformationKey = table.Column<string>(maxLength: 120, nullable: true),
                Confidence = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                EvidenceJson = table.Column<string>(nullable: false, defaultValue: "[]"),
                WarningCodesJson = table.Column<string>(nullable: false, defaultValue: "[]")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AiImportColumnProposals", x => x.Id);
                table.ForeignKey("FK_AiImportColumnProposals_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AiImportColumnProposals_AiImportProposals_AiImportProposalId", x => x.AiImportProposalId, "AiImportProposals", "Id", onDelete: ReferentialAction.Restrict);
            });

        Indexes(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AiHumanDecisions");
        migrationBuilder.DropTable("AiImportColumnProposals");
        migrationBuilder.DropTable("AiChecklistStructureProposals");
        migrationBuilder.DropTable("AiSuggestions");
        migrationBuilder.DropTable("AiImportProposals");
        migrationBuilder.DropTable("AiUsageLedgers");
        migrationBuilder.DropTable("CompanyAiUsagePolicies");
        migrationBuilder.DropTable("AiJobAttempts");
        migrationBuilder.DropTable("AiSuggestionSets");
        migrationBuilder.DropTable("AiProcessingJobs");
    }

    private static void Indexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex("IX_AiProcessingJobs_CompanyId_ImportBatchId_CreatedAtUtc", "AiProcessingJobs", new[] { "CompanyId", "ImportBatchId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_AiProcessingJobs_CompanyId_Status", "AiProcessingJobs", new[] { "CompanyId", "Status" });
        migrationBuilder.CreateIndex("IX_AiProcessingJobs_ImportBatchId", "AiProcessingJobs", "ImportBatchId");
        migrationBuilder.CreateIndex("IX_AiProcessingJobs_RequestedByUserId", "AiProcessingJobs", "RequestedByUserId");
        migrationBuilder.CreateIndex("IX_CompanyAiUsagePolicies_CompanyId", "CompanyAiUsagePolicies", "CompanyId", unique: true);
        migrationBuilder.CreateIndex("IX_CompanyAiUsagePolicies_ChangedByUserId", "CompanyAiUsagePolicies", "ChangedByUserId");
        migrationBuilder.CreateIndex("IX_AiJobAttempts_CompanyId_AiProcessingJobId_AttemptNumber", "AiJobAttempts", new[] { "CompanyId", "AiProcessingJobId", "AttemptNumber" }, unique: true);
        migrationBuilder.CreateIndex("IX_AiJobAttempts_AiProcessingJobId", "AiJobAttempts", "AiProcessingJobId");
        migrationBuilder.CreateIndex("IX_AiSuggestionSets_CompanyId_AiProcessingJobId_CreatedAtUtc", "AiSuggestionSets", new[] { "CompanyId", "AiProcessingJobId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_AiSuggestionSets_AiProcessingJobId", "AiSuggestionSets", "AiProcessingJobId");
        migrationBuilder.CreateIndex("IX_AiSuggestionSets_ReviewedByUserId", "AiSuggestionSets", "ReviewedByUserId");
        migrationBuilder.CreateIndex("IX_AiUsageLedgers_CompanyId_RecordedAtUtc", "AiUsageLedgers", new[] { "CompanyId", "RecordedAtUtc" });
        migrationBuilder.CreateIndex("IX_AiUsageLedgers_AiProcessingJobId", "AiUsageLedgers", "AiProcessingJobId");
        migrationBuilder.CreateIndex("IX_AiSuggestions_CompanyId_AiSuggestionSetId_SortOrder", "AiSuggestions", new[] { "CompanyId", "AiSuggestionSetId", "SortOrder" });
        migrationBuilder.CreateIndex("IX_AiSuggestions_AiSuggestionSetId", "AiSuggestions", "AiSuggestionSetId");
        migrationBuilder.CreateIndex("IX_AiImportProposals_CompanyId_ImportBatchId_CreatedAtUtc", "AiImportProposals", new[] { "CompanyId", "ImportBatchId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_AiImportProposals_ImportBatchId", "AiImportProposals", "ImportBatchId");
        migrationBuilder.CreateIndex("IX_AiImportProposals_AiSuggestionSetId", "AiImportProposals", "AiSuggestionSetId");
        migrationBuilder.CreateIndex("IX_AiChecklistStructureProposals_CompanyId_ImportBatchId_CreatedAtUtc", "AiChecklistStructureProposals", new[] { "CompanyId", "ImportBatchId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_AiChecklistStructureProposals_ImportBatchId", "AiChecklistStructureProposals", "ImportBatchId");
        migrationBuilder.CreateIndex("IX_AiChecklistStructureProposals_AiSuggestionSetId", "AiChecklistStructureProposals", "AiSuggestionSetId");
        migrationBuilder.CreateIndex("IX_AiHumanDecisions_CompanyId_AiSuggestionId_ReviewedAtUtc", "AiHumanDecisions", new[] { "CompanyId", "AiSuggestionId", "ReviewedAtUtc" });
        migrationBuilder.CreateIndex("IX_AiHumanDecisions_AiSuggestionId", "AiHumanDecisions", "AiSuggestionId");
        migrationBuilder.CreateIndex("IX_AiHumanDecisions_ReviewedByUserId", "AiHumanDecisions", "ReviewedByUserId");
        migrationBuilder.CreateIndex("IX_AiImportColumnProposals_CompanyId_AiImportProposalId_SourceColumnKey", "AiImportColumnProposals", new[] { "CompanyId", "AiImportProposalId", "SourceColumnKey" });
        migrationBuilder.CreateIndex("IX_AiImportColumnProposals_AiImportProposalId", "AiImportColumnProposals", "AiImportProposalId");
    }
}
