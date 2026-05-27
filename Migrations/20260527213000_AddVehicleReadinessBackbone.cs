using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260527213000_AddVehicleReadinessBackbone")]
public partial class AddVehicleReadinessBackbone : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Vehicles",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                RegistrationNumber = table.Column<string>(maxLength: 120, nullable: false),
                Callsign = table.Column<string>(maxLength: 120, nullable: false),
                VehicleType = table.Column<string>(maxLength: 120, nullable: false),
                QualificationLevel = table.Column<string>(maxLength: 120, nullable: true),
                SchematicType = table.Column<string>(maxLength: 120, nullable: true),
                NextServiceDate = table.Column<DateTime>(nullable: true),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Vehicles", x => x.Id);
                table.ForeignKey(
                    name: "FK_Vehicles_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "EquipmentItems",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                Name = table.Column<string>(maxLength: 180, nullable: false),
                EquipmentType = table.Column<string>(maxLength: 160, nullable: true),
                Model = table.Column<string>(maxLength: 160, nullable: true),
                SerialOrAssetId = table.Column<string>(maxLength: 160, nullable: true),
                NextServiceDate = table.Column<DateTime>(nullable: true),
                BatteryRequired = table.Column<bool>(nullable: false),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EquipmentItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_EquipmentItems_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "VehicleEquipmentAssignments",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                VehicleId = table.Column<int>(nullable: true),
                EquipmentItemId = table.Column<int>(nullable: true),
                VehicleType = table.Column<string>(maxLength: 120, nullable: true),
                QualificationLevel = table.Column<string>(maxLength: 120, nullable: true),
                ExpectedEquipmentName = table.Column<string>(maxLength: 180, nullable: false),
                ExpectedEquipmentType = table.Column<string>(maxLength: 160, nullable: true),
                ExpectedModel = table.Column<string>(maxLength: 160, nullable: true),
                ExpectedQuantity = table.Column<int>(nullable: false),
                RequiredForReadiness = table.Column<bool>(nullable: false),
                RequiresBatteryCheck = table.Column<bool>(nullable: false),
                DefaultLocation = table.Column<string>(maxLength: 160, nullable: true),
                SortOrder = table.Column<int>(nullable: false),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VehicleEquipmentAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_VehicleEquipmentAssignments_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_VehicleEquipmentAssignments_EquipmentItems_EquipmentItemId",
                    column: x => x.EquipmentItemId,
                    principalTable: "EquipmentItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_VehicleEquipmentAssignments_Vehicles_VehicleId",
                    column: x => x.VehicleId,
                    principalTable: "Vehicles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DailyVehicleReadinessReports",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                VehicleId = table.Column<int>(nullable: false),
                PerformedByUserId = table.Column<int>(nullable: false),
                InspectionDateUtc = table.Column<DateTime>(nullable: false),
                ShiftName = table.Column<string>(maxLength: 80, nullable: true),
                VehicleRegistrationNumber = table.Column<string>(maxLength: 120, nullable: false),
                CallsignAtCheck = table.Column<string>(maxLength: 120, nullable: false),
                VehicleTypeAtCheck = table.Column<string>(maxLength: 120, nullable: false),
                QualificationLevelAtCheck = table.Column<string>(maxLength: 120, nullable: true),
                SchematicTypeAtCheck = table.Column<string>(maxLength: 120, nullable: true),
                VehicleNextServiceDateAtCheck = table.Column<DateTime>(nullable: true),
                SameAsPreviousShiftUsed = table.Column<bool>(nullable: false),
                LightsStatus = table.Column<string>(maxLength: 80, nullable: true),
                SirensStatus = table.Column<string>(maxLength: 80, nullable: true),
                WarningLightsStatus = table.Column<string>(maxLength: 80, nullable: true),
                TyresStatus = table.Column<string>(maxLength: 80, nullable: true),
                RadioConnectivityStatus = table.Column<string>(maxLength: 80, nullable: true),
                OperationalNotes = table.Column<string>(maxLength: 1200, nullable: true),
                DamageNotes = table.Column<string>(maxLength: 1200, nullable: true),
                SchematicNotes = table.Column<string>(maxLength: 1200, nullable: true),
                GeneralNotes = table.Column<string>(maxLength: 1200, nullable: true),
                ReadinessStatus = table.Column<string>(maxLength: 80, nullable: false),
                CriticalIssueCount = table.Column<int>(nullable: false),
                WarningIssueCount = table.Column<int>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                SubmittedAtUtc = table.Column<DateTime>(nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DailyVehicleReadinessReports", x => x.Id);
                table.ForeignKey(
                    name: "FK_DailyVehicleReadinessReports_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DailyVehicleReadinessReports_AppUsers_PerformedByUserId",
                    column: x => x.PerformedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DailyVehicleReadinessReports_Vehicles_VehicleId",
                    column: x => x.VehicleId,
                    principalTable: "Vehicles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DailyVehicleEquipmentChecks",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                DailyVehicleReadinessReportId = table.Column<int>(nullable: false),
                VehicleEquipmentAssignmentId = table.Column<int>(nullable: true),
                EquipmentItemId = table.Column<int>(nullable: true),
                EquipmentName = table.Column<string>(maxLength: 180, nullable: false),
                EquipmentType = table.Column<string>(maxLength: 160, nullable: true),
                Model = table.Column<string>(maxLength: 160, nullable: true),
                SerialOrAssetId = table.Column<string>(maxLength: 160, nullable: true),
                NextServiceDateAtCheck = table.Column<DateTime>(nullable: true),
                PresentStatus = table.Column<string>(maxLength: 80, nullable: false),
                DamageStatus = table.Column<string>(maxLength: 80, nullable: true),
                BatteryStatus = table.Column<string>(maxLength: 80, nullable: true),
                ReadinessImpact = table.Column<string>(maxLength: 80, nullable: false),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                SortOrder = table.Column<int>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DailyVehicleEquipmentChecks", x => x.Id);
                table.ForeignKey(
                    name: "FK_DailyVehicleEquipmentChecks_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DailyVehicleEquipmentChecks_DailyVehicleReadinessReports_DailyVehicleReadinessReportId",
                    column: x => x.DailyVehicleReadinessReportId,
                    principalTable: "DailyVehicleReadinessReports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DailyVehicleEquipmentChecks_EquipmentItems_EquipmentItemId",
                    column: x => x.EquipmentItemId,
                    principalTable: "EquipmentItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_DailyVehicleEquipmentChecks_VehicleEquipmentAssignments_VehicleEquipmentAssignmentId",
                    column: x => x.VehicleEquipmentAssignmentId,
                    principalTable: "VehicleEquipmentAssignments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Vehicles_CompanyId_RegistrationNumber",
            table: "Vehicles",
            columns: new[] { "CompanyId", "RegistrationNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EquipmentItems_CompanyId_SerialOrAssetId",
            table: "EquipmentItems",
            columns: new[] { "CompanyId", "SerialOrAssetId" });

        migrationBuilder.CreateIndex(
            name: "IX_VehicleEquipmentAssignments_CompanyId_VehicleId_SortOrder",
            table: "VehicleEquipmentAssignments",
            columns: new[] { "CompanyId", "VehicleId", "SortOrder" });

        migrationBuilder.CreateIndex(
            name: "IX_VehicleEquipmentAssignments_CompanyId_VehicleType_QualificationLevel",
            table: "VehicleEquipmentAssignments",
            columns: new[] { "CompanyId", "VehicleType", "QualificationLevel" });

        migrationBuilder.CreateIndex(
            name: "IX_VehicleEquipmentAssignments_EquipmentItemId",
            table: "VehicleEquipmentAssignments",
            column: "EquipmentItemId");

        migrationBuilder.CreateIndex(
            name: "IX_VehicleEquipmentAssignments_VehicleId",
            table: "VehicleEquipmentAssignments",
            column: "VehicleId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleReadinessReports_CompanyId_VehicleId_InspectionDateUtc",
            table: "DailyVehicleReadinessReports",
            columns: new[] { "CompanyId", "VehicleId", "InspectionDateUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleReadinessReports_CompanyId_ReadinessStatus_InspectionDateUtc",
            table: "DailyVehicleReadinessReports",
            columns: new[] { "CompanyId", "ReadinessStatus", "InspectionDateUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleReadinessReports_PerformedByUserId",
            table: "DailyVehicleReadinessReports",
            column: "PerformedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleReadinessReports_VehicleId",
            table: "DailyVehicleReadinessReports",
            column: "VehicleId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleEquipmentChecks_CompanyId_DailyVehicleReadinessReportId_SortOrder",
            table: "DailyVehicleEquipmentChecks",
            columns: new[] { "CompanyId", "DailyVehicleReadinessReportId", "SortOrder" });

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleEquipmentChecks_DailyVehicleReadinessReportId",
            table: "DailyVehicleEquipmentChecks",
            column: "DailyVehicleReadinessReportId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleEquipmentChecks_EquipmentItemId",
            table: "DailyVehicleEquipmentChecks",
            column: "EquipmentItemId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleEquipmentChecks_VehicleEquipmentAssignmentId",
            table: "DailyVehicleEquipmentChecks",
            column: "VehicleEquipmentAssignmentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DailyVehicleEquipmentChecks");

        migrationBuilder.DropTable(
            name: "DailyVehicleReadinessReports");

        migrationBuilder.DropTable(
            name: "VehicleEquipmentAssignments");

        migrationBuilder.DropTable(
            name: "EquipmentItems");

        migrationBuilder.DropTable(
            name: "Vehicles");
    }
}
