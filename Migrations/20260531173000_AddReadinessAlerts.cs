using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260531173000_AddReadinessAlerts")]
public partial class AddReadinessAlerts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReadinessAlerts",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                ReadinessEngineRuleId = table.Column<int>(type: "INTEGER", nullable: true),
                DailyVehicleReadinessReportId = table.Column<int>(type: "INTEGER", nullable: false),
                DailyVehicleEquipmentCheckId = table.Column<int>(type: "INTEGER", nullable: true),
                VehicleId = table.Column<int>(type: "INTEGER", nullable: false),
                TriggeredByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                AssignedToUserId = table.Column<int>(type: "INTEGER", nullable: true),
                ReviewedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                AssetType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                SourceArea = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                ItemName = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                FieldKey = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                TriggerValue = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                Severity = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                ImpactPercent = table.Column<int>(type: "INTEGER", nullable: false),
                IsHardBlocker = table.Column<bool>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                VehicleLabel = table.Column<string>(type: "TEXT", maxLength: 220, nullable: false),
                AlertSummary = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: false),
                SourceValue = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                ReviewNote = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                AcknowledgedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                ResolvedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReadinessAlerts", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReadinessAlerts_AppUsers_AssignedToUserId",
                    column: x => x.AssignedToUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessAlerts_AppUsers_ReviewedByUserId",
                    column: x => x.ReviewedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessAlerts_AppUsers_TriggeredByUserId",
                    column: x => x.TriggeredByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessAlerts_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessAlerts_DailyVehicleEquipmentChecks_DailyVehicleEquipmentCheckId",
                    column: x => x.DailyVehicleEquipmentCheckId,
                    principalTable: "DailyVehicleEquipmentChecks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessAlerts_DailyVehicleReadinessReports_DailyVehicleReadinessReportId",
                    column: x => x.DailyVehicleReadinessReportId,
                    principalTable: "DailyVehicleReadinessReports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessAlerts_ReadinessEngineRules_ReadinessEngineRuleId",
                    column: x => x.ReadinessEngineRuleId,
                    principalTable: "ReadinessEngineRules",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ReadinessAlerts_Vehicles_VehicleId",
                    column: x => x.VehicleId,
                    principalTable: "Vehicles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessAlerts_AssignedToUserId",
            table: "ReadinessAlerts",
            column: "AssignedToUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessAlerts_CompanyId_AssignedToUserId_Status_CreatedAtUtc",
            table: "ReadinessAlerts",
            columns: new[] { "CompanyId", "AssignedToUserId", "Status", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessAlerts_CompanyId_Status_CreatedAtUtc",
            table: "ReadinessAlerts",
            columns: new[] { "CompanyId", "Status", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessAlerts_DailyVehicleEquipmentCheckId",
            table: "ReadinessAlerts",
            column: "DailyVehicleEquipmentCheckId");

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessAlerts_DailyVehicleReadinessReportId",
            table: "ReadinessAlerts",
            column: "DailyVehicleReadinessReportId");

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessAlerts_ReadinessEngineRuleId",
            table: "ReadinessAlerts",
            column: "ReadinessEngineRuleId");

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessAlerts_ReviewedByUserId",
            table: "ReadinessAlerts",
            column: "ReviewedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessAlerts_TriggeredByUserId",
            table: "ReadinessAlerts",
            column: "TriggeredByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ReadinessAlerts_VehicleId",
            table: "ReadinessAlerts",
            column: "VehicleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ReadinessAlerts");
    }
}
