using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260530170000_AddChecklistCatalogueAndVarianceBackbone")]
public partial class AddChecklistCatalogueAndVarianceBackbone : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ChecklistTemplateId",
            table: "DailyVehicleReadinessReports",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ChecklistTemplateVersion",
            table: "DailyVehicleReadinessReports",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ChecklistItemId",
            table: "DailyVehicleEquipmentChecks",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SourceType",
            table: "ChecklistTemplates",
            maxLength: 80,
            nullable: false,
            defaultValue: "Built");

        migrationBuilder.AddColumn<int>(
            name: "ParentChecklistTemplateId",
            table: "ChecklistTemplates",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CreatedByUserId",
            table: "ChecklistTemplates",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PublishedByUserId",
            table: "ChecklistTemplates",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PublishScopeSummary",
            table: "ChecklistTemplates",
            maxLength: 260,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PublishNotes",
            table: "ChecklistTemplates",
            maxLength: 1200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ItemKind",
            table: "ChecklistItems",
            maxLength: 80,
            nullable: false,
            defaultValue: "Field");

        migrationBuilder.AddColumn<int>(
            name: "CatalogueItemId",
            table: "ChecklistItems",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "EquipmentType",
            table: "ChecklistItems",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Model",
            table: "ChecklistItems",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FieldKey",
            table: "ChecklistItems",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsRequired",
            table: "ChecklistItems",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsReadinessCritical",
            table: "ChecklistItems",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "AllowsSameAsPrevious",
            table: "ChecklistItems",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "DefaultLocation",
            table: "ChecklistItems",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StockCategory",
            table: "StockItems",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "MinimumQuantity",
            table: "StockItems",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Unit",
            table: "StockItems",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ExpiryDate",
            table: "StockItems",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsReadinessCritical",
            table: "StockItems",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "CatalogueItems",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                CatalogueType = table.Column<string>(maxLength: 80, nullable: false),
                Category = table.Column<string>(maxLength: 160, nullable: false),
                Subcategory = table.Column<string>(maxLength: 160, nullable: true),
                ItemName = table.Column<string>(maxLength: 220, nullable: false),
                Variant = table.Column<string>(maxLength: 220, nullable: true),
                Manufacturer = table.Column<string>(maxLength: 160, nullable: true),
                Model = table.Column<string>(maxLength: 160, nullable: true),
                Size = table.Column<string>(maxLength: 80, nullable: true),
                Unit = table.Column<string>(maxLength: 80, nullable: true),
                ServiceRequired = table.Column<bool>(nullable: false),
                BatteryRequired = table.Column<bool>(nullable: false),
                SerialRequired = table.Column<bool>(nullable: false),
                BatchRequired = table.Column<bool>(nullable: false),
                ExpiryRequired = table.Column<bool>(nullable: false),
                ReadinessCritical = table.Column<bool>(nullable: false),
                DefaultChecklistColumns = table.Column<string>(maxLength: 600, nullable: true),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CatalogueItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_CatalogueItems_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ChecklistColumnDefinitions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ChecklistItemId = table.Column<int>(nullable: false),
                Heading = table.Column<string>(maxLength: 120, nullable: false),
                FieldKey = table.Column<string>(maxLength: 120, nullable: false),
                ResponseType = table.Column<string>(maxLength: 80, nullable: false),
                RegisterSource = table.Column<string>(maxLength: 160, nullable: true),
                IsRequired = table.Column<bool>(nullable: false),
                IsEditable = table.Column<bool>(nullable: false),
                AllowsNotApplicable = table.Column<bool>(nullable: false),
                PullsFromRegister = table.Column<bool>(nullable: false),
                AffectsReadiness = table.Column<bool>(nullable: false),
                SameAsPreviousEligible = table.Column<bool>(nullable: false),
                RequiresNoteWhenNotNormal = table.Column<bool>(nullable: false),
                ReadinessImpact = table.Column<string>(maxLength: 80, nullable: true),
                DropdownOptionsJson = table.Column<string>(maxLength: 1200, nullable: true),
                SortOrder = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChecklistColumnDefinitions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ChecklistColumnDefinitions_ChecklistItems_ChecklistItemId",
                    column: x => x.ChecklistItemId,
                    principalTable: "ChecklistItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ChecklistPublishScopes",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ChecklistTemplateId = table.Column<int>(nullable: false),
                ScopeType = table.Column<string>(maxLength: 80, nullable: false),
                OperationalAreaId = table.Column<int>(nullable: true),
                VehicleId = table.Column<int>(nullable: true),
                PublishedByUserId = table.Column<int>(nullable: false),
                PublishNote = table.Column<string>(maxLength: 1200, nullable: true),
                IsActive = table.Column<bool>(nullable: false),
                PublishedAtUtc = table.Column<DateTime>(nullable: false),
                RetiredAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChecklistPublishScopes", x => x.Id);
                table.ForeignKey(
                    name: "FK_ChecklistPublishScopes_ChecklistTemplates_ChecklistTemplateId",
                    column: x => x.ChecklistTemplateId,
                    principalTable: "ChecklistTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ChecklistPublishScopes_OperationalAreas_OperationalAreaId",
                    column: x => x.OperationalAreaId,
                    principalTable: "OperationalAreas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChecklistPublishScopes_AppUsers_PublishedByUserId",
                    column: x => x.PublishedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChecklistPublishScopes_Vehicles_VehicleId",
                    column: x => x.VehicleId,
                    principalTable: "Vehicles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ChecklistVarianceAlerts",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                DailyVehicleReadinessReportId = table.Column<int>(nullable: false),
                DailyVehicleEquipmentCheckId = table.Column<int>(nullable: true),
                VehicleId = table.Column<int>(nullable: true),
                DetectedForUserId = table.Column<int>(nullable: false),
                AssignedToUserId = table.Column<int>(nullable: true),
                ReviewedByUserId = table.Column<int>(nullable: true),
                AlertType = table.Column<string>(maxLength: 80, nullable: false),
                FieldKey = table.Column<string>(maxLength: 120, nullable: false),
                AssetLabel = table.Column<string>(maxLength: 220, nullable: true),
                PreviousValue = table.Column<string>(maxLength: 1200, nullable: true),
                NewValue = table.Column<string>(maxLength: 1200, nullable: true),
                RegisterValue = table.Column<string>(maxLength: 1200, nullable: true),
                Severity = table.Column<string>(maxLength: 80, nullable: false),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                RequiresRegisterUpdate = table.Column<bool>(nullable: false),
                RegisterUpdatedAtUtc = table.Column<DateTime>(nullable: true),
                ReviewedAtUtc = table.Column<DateTime>(nullable: true),
                ReviewNote = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChecklistVarianceAlerts", x => x.Id);
                table.ForeignKey(
                    name: "FK_ChecklistVarianceAlerts_AppUsers_AssignedToUserId",
                    column: x => x.AssignedToUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChecklistVarianceAlerts_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChecklistVarianceAlerts_DailyVehicleEquipmentChecks_DailyVehicleEquipmentCheckId",
                    column: x => x.DailyVehicleEquipmentCheckId,
                    principalTable: "DailyVehicleEquipmentChecks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChecklistVarianceAlerts_DailyVehicleReadinessReports_DailyVehicleReadinessReportId",
                    column: x => x.DailyVehicleReadinessReportId,
                    principalTable: "DailyVehicleReadinessReports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChecklistVarianceAlerts_AppUsers_DetectedForUserId",
                    column: x => x.DetectedForUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChecklistVarianceAlerts_AppUsers_ReviewedByUserId",
                    column: x => x.ReviewedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ChecklistVarianceAlerts_Vehicles_VehicleId",
                    column: x => x.VehicleId,
                    principalTable: "Vehicles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleReadinessReports_ChecklistTemplateId",
            table: "DailyVehicleReadinessReports",
            column: "ChecklistTemplateId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleEquipmentChecks_ChecklistItemId",
            table: "DailyVehicleEquipmentChecks",
            column: "ChecklistItemId");

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistColumnDefinitions_ChecklistItemId_SortOrder",
            table: "ChecklistColumnDefinitions",
            columns: new[] { "ChecklistItemId", "SortOrder" });

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistPublishScopes_ChecklistTemplateId_ScopeType_IsActive",
            table: "ChecklistPublishScopes",
            columns: new[] { "ChecklistTemplateId", "ScopeType", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistPublishScopes_OperationalAreaId",
            table: "ChecklistPublishScopes",
            column: "OperationalAreaId");

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistPublishScopes_PublishedByUserId",
            table: "ChecklistPublishScopes",
            column: "PublishedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistPublishScopes_VehicleId",
            table: "ChecklistPublishScopes",
            column: "VehicleId");

        migrationBuilder.CreateIndex(
            name: "IX_CatalogueItems_CompanyId_CatalogueType_Category_ItemName",
            table: "CatalogueItems",
            columns: new[] { "CompanyId", "CatalogueType", "Category", "ItemName" });

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistVarianceAlerts_AssignedToUserId",
            table: "ChecklistVarianceAlerts",
            column: "AssignedToUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistVarianceAlerts_CompanyId_AssignedToUserId_Status_CreatedAtUtc",
            table: "ChecklistVarianceAlerts",
            columns: new[] { "CompanyId", "AssignedToUserId", "Status", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistVarianceAlerts_DailyVehicleEquipmentCheckId",
            table: "ChecklistVarianceAlerts",
            column: "DailyVehicleEquipmentCheckId");

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistVarianceAlerts_DailyVehicleReadinessReportId",
            table: "ChecklistVarianceAlerts",
            column: "DailyVehicleReadinessReportId");

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistVarianceAlerts_DetectedForUserId",
            table: "ChecklistVarianceAlerts",
            column: "DetectedForUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistVarianceAlerts_ReviewedByUserId",
            table: "ChecklistVarianceAlerts",
            column: "ReviewedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistVarianceAlerts_VehicleId",
            table: "ChecklistVarianceAlerts",
            column: "VehicleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ChecklistVarianceAlerts");
        migrationBuilder.DropTable(name: "ChecklistPublishScopes");
        migrationBuilder.DropTable(name: "ChecklistColumnDefinitions");
        migrationBuilder.DropTable(name: "CatalogueItems");

        migrationBuilder.DropIndex(name: "IX_DailyVehicleReadinessReports_ChecklistTemplateId", table: "DailyVehicleReadinessReports");
        migrationBuilder.DropIndex(name: "IX_DailyVehicleEquipmentChecks_ChecklistItemId", table: "DailyVehicleEquipmentChecks");

        migrationBuilder.DropColumn(name: "ChecklistTemplateId", table: "DailyVehicleReadinessReports");
        migrationBuilder.DropColumn(name: "ChecklistTemplateVersion", table: "DailyVehicleReadinessReports");
        migrationBuilder.DropColumn(name: "ChecklistItemId", table: "DailyVehicleEquipmentChecks");
        migrationBuilder.DropColumn(name: "SourceType", table: "ChecklistTemplates");
        migrationBuilder.DropColumn(name: "ParentChecklistTemplateId", table: "ChecklistTemplates");
        migrationBuilder.DropColumn(name: "CreatedByUserId", table: "ChecklistTemplates");
        migrationBuilder.DropColumn(name: "PublishedByUserId", table: "ChecklistTemplates");
        migrationBuilder.DropColumn(name: "PublishScopeSummary", table: "ChecklistTemplates");
        migrationBuilder.DropColumn(name: "PublishNotes", table: "ChecklistTemplates");
        migrationBuilder.DropColumn(name: "ItemKind", table: "ChecklistItems");
        migrationBuilder.DropColumn(name: "CatalogueItemId", table: "ChecklistItems");
        migrationBuilder.DropColumn(name: "EquipmentType", table: "ChecklistItems");
        migrationBuilder.DropColumn(name: "Model", table: "ChecklistItems");
        migrationBuilder.DropColumn(name: "FieldKey", table: "ChecklistItems");
        migrationBuilder.DropColumn(name: "IsRequired", table: "ChecklistItems");
        migrationBuilder.DropColumn(name: "IsReadinessCritical", table: "ChecklistItems");
        migrationBuilder.DropColumn(name: "AllowsSameAsPrevious", table: "ChecklistItems");
        migrationBuilder.DropColumn(name: "DefaultLocation", table: "ChecklistItems");
        migrationBuilder.DropColumn(name: "StockCategory", table: "StockItems");
        migrationBuilder.DropColumn(name: "MinimumQuantity", table: "StockItems");
        migrationBuilder.DropColumn(name: "Unit", table: "StockItems");
        migrationBuilder.DropColumn(name: "ExpiryDate", table: "StockItems");
        migrationBuilder.DropColumn(name: "IsReadinessCritical", table: "StockItems");
    }
}
