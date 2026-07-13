using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260713190000_AddImportFoundation")]
public partial class AddImportFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ImportBatches",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                SourceAssetFileId = table.Column<int>(nullable: false),
                TargetType = table.Column<string>(maxLength: 80, nullable: false),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                FileHash = table.Column<string>(maxLength: 128, nullable: false),
                OriginalFileName = table.Column<string>(maxLength: 260, nullable: false),
                SelectedWorksheet = table.Column<string>(maxLength: 160, nullable: true),
                HeaderRowNumber = table.Column<int>(nullable: true),
                LayoutMode = table.Column<string>(maxLength: 80, nullable: true),
                ParserContractVersion = table.Column<int>(nullable: false, defaultValue: 1),
                SourceProfileJson = table.Column<string>(nullable: false, defaultValue: "{}"),
                WorksheetCount = table.Column<int>(nullable: false),
                SourceRowCount = table.Column<int>(nullable: false),
                IncludedRowCount = table.Column<int>(nullable: false),
                ValidRowCount = table.Column<int>(nullable: false),
                InvalidRowCount = table.Column<int>(nullable: false),
                WarningRowCount = table.Column<int>(nullable: false),
                CreatedByUserId = table.Column<int>(nullable: false),
                ValidatedByUserId = table.Column<int>(nullable: true),
                CommittedByUserId = table.Column<int>(nullable: true),
                RolledBackByUserId = table.Column<int>(nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: false),
                ValidatedAtUtc = table.Column<DateTime>(nullable: true),
                CommittedAtUtc = table.Column<DateTime>(nullable: true),
                RolledBackAtUtc = table.Column<DateTime>(nullable: true),
                FailureCode = table.Column<string>(maxLength: 80, nullable: true),
                FailureSummary = table.Column<string>(maxLength: 1200, nullable: true),
                ConcurrencyToken = table.Column<string>(maxLength: 36, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImportBatches", x => x.Id);
                table.ForeignKey("FK_ImportBatches_AssetFiles_SourceAssetFileId", x => x.SourceAssetFileId, "AssetFiles", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportBatches_AppUsers_CommittedByUserId", x => x.CommittedByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportBatches_AppUsers_CreatedByUserId", x => x.CreatedByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportBatches_AppUsers_RolledBackByUserId", x => x.RolledBackByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportBatches_AppUsers_ValidatedByUserId", x => x.ValidatedByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportBatches_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ImportColumnMappings",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                ImportBatchId = table.Column<int>(nullable: false),
                SourceColumnIndex = table.Column<int>(nullable: false),
                SourceHeading = table.Column<string>(maxLength: 260, nullable: false),
                NormalizedSourceHeading = table.Column<string>(maxLength: 260, nullable: false),
                TargetFieldKey = table.Column<string>(maxLength: 160, nullable: true),
                ConversionRule = table.Column<string>(maxLength: 120, nullable: true),
                FixedValue = table.Column<string>(maxLength: 1000, nullable: true),
                SuggestionReason = table.Column<string>(maxLength: 600, nullable: true),
                IsIgnored = table.Column<bool>(nullable: false),
                IsUserConfirmed = table.Column<bool>(nullable: false),
                DisplayOrder = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImportColumnMappings", x => x.Id);
                table.ForeignKey("FK_ImportColumnMappings_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportColumnMappings_ImportBatches_ImportBatchId", x => x.ImportBatchId, "ImportBatches", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ImportRowResults",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                ImportBatchId = table.Column<int>(nullable: false),
                SourceRowNumber = table.Column<int>(nullable: false),
                OriginalPayloadJson = table.Column<string>(nullable: false, defaultValue: "{}"),
                CorrectedPayloadJson = table.Column<string>(nullable: true),
                ValidationStatus = table.Column<string>(maxLength: 40, nullable: false),
                FieldErrorsJson = table.Column<string>(nullable: true),
                WarningsJson = table.Column<string>(nullable: true),
                DuplicateCandidatesJson = table.Column<string>(nullable: true),
                RowDecision = table.Column<string>(maxLength: 40, nullable: true),
                IsIncluded = table.Column<bool>(nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImportRowResults", x => x.Id);
                table.ForeignKey("FK_ImportRowResults_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportRowResults_ImportBatches_ImportBatchId", x => x.ImportBatchId, "ImportBatches", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ImportEntityChanges",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                ImportBatchId = table.Column<int>(nullable: false),
                ImportRowResultId = table.Column<int>(nullable: true),
                EntityType = table.Column<string>(maxLength: 80, nullable: false),
                EntityId = table.Column<int>(nullable: true),
                Action = table.Column<string>(maxLength: 40, nullable: false),
                BeforeValuesJson = table.Column<string>(nullable: true),
                AfterValuesJson = table.Column<string>(nullable: true),
                EntityStateToken = table.Column<string>(maxLength: 160, nullable: true),
                IsRollbackEligible = table.Column<bool>(nullable: false),
                RollbackStatus = table.Column<string>(maxLength: 40, nullable: true),
                RollbackReason = table.Column<string>(maxLength: 1200, nullable: true),
                RolledBackByUserId = table.Column<int>(nullable: true),
                RolledBackAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImportEntityChanges", x => x.Id);
                table.ForeignKey("FK_ImportEntityChanges_AppUsers_RolledBackByUserId", x => x.RolledBackByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportEntityChanges_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportEntityChanges_ImportBatches_ImportBatchId", x => x.ImportBatchId, "ImportBatches", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportEntityChanges_ImportRowResults_ImportRowResultId", x => x.ImportRowResultId, "ImportRowResults", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_ImportBatches_CommittedByUserId", "ImportBatches", "CommittedByUserId");
        migrationBuilder.CreateIndex("IX_ImportBatches_CompanyId_Status_CreatedAtUtc", "ImportBatches", new[] { "CompanyId", "Status", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_ImportBatches_CreatedByUserId", "ImportBatches", "CreatedByUserId");
        migrationBuilder.CreateIndex("IX_ImportBatches_RolledBackByUserId", "ImportBatches", "RolledBackByUserId");
        migrationBuilder.CreateIndex("IX_ImportBatches_SourceAssetFileId", "ImportBatches", "SourceAssetFileId", unique: true);
        migrationBuilder.CreateIndex("IX_ImportBatches_ValidatedByUserId", "ImportBatches", "ValidatedByUserId");
        migrationBuilder.CreateIndex("IX_ImportColumnMappings_CompanyId_ImportBatchId_SourceColumnIndex", "ImportColumnMappings", new[] { "CompanyId", "ImportBatchId", "SourceColumnIndex" }, unique: true);
        migrationBuilder.CreateIndex("IX_ImportColumnMappings_ImportBatchId", "ImportColumnMappings", "ImportBatchId");
        migrationBuilder.CreateIndex("IX_ImportEntityChanges_CompanyId_ImportBatchId_EntityType_EntityId", "ImportEntityChanges", new[] { "CompanyId", "ImportBatchId", "EntityType", "EntityId" });
        migrationBuilder.CreateIndex("IX_ImportEntityChanges_ImportBatchId", "ImportEntityChanges", "ImportBatchId");
        migrationBuilder.CreateIndex("IX_ImportEntityChanges_ImportRowResultId", "ImportEntityChanges", "ImportRowResultId");
        migrationBuilder.CreateIndex("IX_ImportEntityChanges_RolledBackByUserId", "ImportEntityChanges", "RolledBackByUserId");
        migrationBuilder.CreateIndex("IX_ImportRowResults_CompanyId_ImportBatchId_SourceRowNumber", "ImportRowResults", new[] { "CompanyId", "ImportBatchId", "SourceRowNumber" }, unique: true);
        migrationBuilder.CreateIndex("IX_ImportRowResults_ImportBatchId", "ImportRowResults", "ImportBatchId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ImportColumnMappings");
        migrationBuilder.DropTable(name: "ImportEntityChanges");
        migrationBuilder.DropTable(name: "ImportRowResults");
        migrationBuilder.DropTable(name: "ImportBatches");
    }
}
