using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260531150000_AddReadinessEngine")]
public partial class AddReadinessEngine : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReadinessEngineVersions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                Name = table.Column<string>(maxLength: 120, nullable: false),
                VersionNumber = table.Column<string>(maxLength: 40, nullable: false),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                SourceReadinessEngineVersionId = table.Column<int>(nullable: true),
                CreatedByUserId = table.Column<int>(nullable: true),
                PublishedByUserId = table.Column<int>(nullable: true),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true),
                PublishedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReadinessEngineVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReadinessEngineVersions_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessEngineVersions_AppUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessEngineVersions_AppUsers_PublishedByUserId",
                    column: x => x.PublishedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessEngineVersions_ReadinessEngineVersions_SourceReadinessEngineVersionId",
                    column: x => x.SourceReadinessEngineVersionId,
                    principalTable: "ReadinessEngineVersions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ReadinessEngineRules",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                ReadinessEngineVersionId = table.Column<int>(nullable: false),
                AssetType = table.Column<string>(maxLength: 80, nullable: false),
                Section = table.Column<string>(maxLength: 120, nullable: false),
                ItemName = table.Column<string>(maxLength: 180, nullable: false),
                FieldKey = table.Column<string>(maxLength: 120, nullable: true),
                TriggerValue = table.Column<string>(maxLength: 180, nullable: false),
                AppliesTo = table.Column<string>(maxLength: 180, nullable: false),
                TargetVehicleType = table.Column<string>(maxLength: 120, nullable: true),
                OperationalAreaId = table.Column<int>(nullable: true),
                ChecklistTemplateId = table.Column<int>(nullable: true),
                Severity = table.Column<string>(maxLength: 40, nullable: false),
                DefaultImpactPercent = table.Column<int>(nullable: false),
                ManualImpactPercent = table.Column<int>(nullable: true),
                IsHardBlocker = table.Column<bool>(nullable: false),
                ManagerAlert = table.Column<bool>(nullable: false),
                IsActive = table.Column<bool>(nullable: false),
                IsAutoPopulated = table.Column<bool>(nullable: false),
                SourceType = table.Column<string>(maxLength: 80, nullable: false),
                SourceEntityType = table.Column<string>(maxLength: 80, nullable: true),
                SourceEntityId = table.Column<int>(nullable: true),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                SortOrder = table.Column<int>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReadinessEngineRules", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReadinessEngineRules_ChecklistTemplates_ChecklistTemplateId",
                    column: x => x.ChecklistTemplateId,
                    principalTable: "ChecklistTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessEngineRules_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessEngineRules_OperationalAreas_OperationalAreaId",
                    column: x => x.OperationalAreaId,
                    principalTable: "OperationalAreas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessEngineRules_ReadinessEngineVersions_ReadinessEngineVersionId",
                    column: x => x.ReadinessEngineVersionId,
                    principalTable: "ReadinessEngineVersions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ReadinessScoringChangeRequests",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                RequestedByUserId = table.Column<int>(nullable: false),
                ReviewedByUserId = table.Column<int>(nullable: true),
                ReadinessEngineRuleId = table.Column<int>(nullable: true),
                Status = table.Column<string>(maxLength: 40, nullable: false),
                AssetType = table.Column<string>(maxLength: 80, nullable: false),
                ItemName = table.Column<string>(maxLength: 180, nullable: false),
                TriggerValue = table.Column<string>(maxLength: 180, nullable: false),
                CurrentSeverity = table.Column<string>(maxLength: 40, nullable: true),
                ProposedSeverity = table.Column<string>(maxLength: 40, nullable: false),
                CurrentImpactPercent = table.Column<int>(nullable: true),
                ProposedImpactPercent = table.Column<int>(nullable: true),
                CurrentActive = table.Column<bool>(nullable: true),
                ProposedActive = table.Column<bool>(nullable: true),
                Reason = table.Column<string>(maxLength: 1200, nullable: false),
                SeniorDecisionNote = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                ReviewedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReadinessScoringChangeRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReadinessScoringChangeRequests_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessScoringChangeRequests_ReadinessEngineRules_ReadinessEngineRuleId",
                    column: x => x.ReadinessEngineRuleId,
                    principalTable: "ReadinessEngineRules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessScoringChangeRequests_AppUsers_RequestedByUserId",
                    column: x => x.RequestedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessScoringChangeRequests_AppUsers_ReviewedByUserId",
                    column: x => x.ReviewedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessEngineVersions_CompanyId_Status_CreatedAtUtc",
            table: "ReadinessEngineVersions",
            columns: new[] { "CompanyId", "Status", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_ReadinessEngineVersions_CreatedByUserId",
            table: "ReadinessEngineVersions",
            column: "CreatedByUserId");
        migrationBuilder.CreateIndex(
            name: "IX_ReadinessEngineVersions_PublishedByUserId",
            table: "ReadinessEngineVersions",
            column: "PublishedByUserId");
        migrationBuilder.CreateIndex(
            name: "IX_ReadinessEngineVersions_SourceReadinessEngineVersionId",
            table: "ReadinessEngineVersions",
            column: "SourceReadinessEngineVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessEngineRules_CompanyId_AssetType_Section_ItemName_TriggerValue",
            table: "ReadinessEngineRules",
            columns: new[] { "CompanyId", "AssetType", "Section", "ItemName", "TriggerValue" });
        migrationBuilder.CreateIndex(
            name: "IX_ReadinessEngineRules_ReadinessEngineVersionId_SortOrder",
            table: "ReadinessEngineRules",
            columns: new[] { "ReadinessEngineVersionId", "SortOrder" });
        migrationBuilder.CreateIndex(
            name: "IX_ReadinessEngineRules_ChecklistTemplateId",
            table: "ReadinessEngineRules",
            column: "ChecklistTemplateId");
        migrationBuilder.CreateIndex(
            name: "IX_ReadinessEngineRules_OperationalAreaId",
            table: "ReadinessEngineRules",
            column: "OperationalAreaId");

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessScoringChangeRequests_CompanyId_Status_CreatedAtUtc",
            table: "ReadinessScoringChangeRequests",
            columns: new[] { "CompanyId", "Status", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_ReadinessScoringChangeRequests_ReadinessEngineRuleId",
            table: "ReadinessScoringChangeRequests",
            column: "ReadinessEngineRuleId");
        migrationBuilder.CreateIndex(
            name: "IX_ReadinessScoringChangeRequests_RequestedByUserId",
            table: "ReadinessScoringChangeRequests",
            column: "RequestedByUserId");
        migrationBuilder.CreateIndex(
            name: "IX_ReadinessScoringChangeRequests_ReviewedByUserId",
            table: "ReadinessScoringChangeRequests",
            column: "ReviewedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ReadinessScoringChangeRequests");
        migrationBuilder.DropTable(name: "ReadinessEngineRules");
        migrationBuilder.DropTable(name: "ReadinessEngineVersions");
    }
}
