using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260527220000_AddReadinessDraftAndSameShiftControls")]
public partial class AddReadinessDraftAndSameShiftControls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "AllowSameAsPreviousVehicleInspection",
            table: "Companies",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "AllowSameAsPreviousEquipmentCheck",
            table: "Companies",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ShiftStartedAtUtc",
            table: "DailyVehicleReadinessReports",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ShiftEndsAtUtc",
            table: "DailyVehicleReadinessReports",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DraftExpiresAtUtc",
            table: "DailyVehicleReadinessReports",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastSavedAtUtc",
            table: "DailyVehicleReadinessReports",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "WorkflowStatus",
            table: "DailyVehicleReadinessReports",
            maxLength: 80,
            nullable: false,
            defaultValue: "Draft");

        migrationBuilder.AddColumn<string>(
            name: "LastSavedSection",
            table: "DailyVehicleReadinessReports",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "VehicleSameAsPreviousShiftUsed",
            table: "DailyVehicleReadinessReports",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "EquipmentSameAsPreviousShiftUsed",
            table: "DailyVehicleReadinessReports",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "VehicleSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "EquipmentSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "VehicleSameAsPreviousAppliedAtUtc",
            table: "DailyVehicleReadinessReports",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "EquipmentSameAsPreviousAppliedAtUtc",
            table: "DailyVehicleReadinessReports",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "VehicleSameAsPreviousCopiedSummary",
            table: "DailyVehicleReadinessReports",
            maxLength: 1200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "EquipmentSameAsPreviousCopiedSummary",
            table: "DailyVehicleReadinessReports",
            maxLength: 1200,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "SameAsPreviousShiftUsed",
            table: "DailyVehicleEquipmentChecks",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "CopiedFromDailyVehicleEquipmentCheckId",
            table: "DailyVehicleEquipmentChecks",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "SameAsPreviousAppliedAtUtc",
            table: "DailyVehicleEquipmentChecks",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleReadinessReports_CompanyId_WorkflowStatus_DraftExpiresAtUtc",
            table: "DailyVehicleReadinessReports",
            columns: new[] { "CompanyId", "WorkflowStatus", "DraftExpiresAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleReadinessReports_VehicleSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports",
            column: "VehicleSameAsPreviousSourceReportId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleReadinessReports_EquipmentSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports",
            column: "EquipmentSameAsPreviousSourceReportId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyVehicleEquipmentChecks_CopiedFromDailyVehicleEquipmentCheckId",
            table: "DailyVehicleEquipmentChecks",
            column: "CopiedFromDailyVehicleEquipmentCheckId");

        migrationBuilder.AddForeignKey(
            name: "FK_DailyVehicleReadinessReports_DailyVehicleReadinessReports_VehicleSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports",
            column: "VehicleSameAsPreviousSourceReportId",
            principalTable: "DailyVehicleReadinessReports",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_DailyVehicleReadinessReports_DailyVehicleReadinessReports_EquipmentSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports",
            column: "EquipmentSameAsPreviousSourceReportId",
            principalTable: "DailyVehicleReadinessReports",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_DailyVehicleEquipmentChecks_DailyVehicleEquipmentChecks_CopiedFromDailyVehicleEquipmentCheckId",
            table: "DailyVehicleEquipmentChecks",
            column: "CopiedFromDailyVehicleEquipmentCheckId",
            principalTable: "DailyVehicleEquipmentChecks",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DailyVehicleReadinessReports_DailyVehicleReadinessReports_VehicleSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropForeignKey(
            name: "FK_DailyVehicleReadinessReports_DailyVehicleReadinessReports_EquipmentSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropForeignKey(
            name: "FK_DailyVehicleEquipmentChecks_DailyVehicleEquipmentChecks_CopiedFromDailyVehicleEquipmentCheckId",
            table: "DailyVehicleEquipmentChecks");

        migrationBuilder.DropIndex(
            name: "IX_DailyVehicleReadinessReports_CompanyId_WorkflowStatus_DraftExpiresAtUtc",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropIndex(
            name: "IX_DailyVehicleReadinessReports_VehicleSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropIndex(
            name: "IX_DailyVehicleReadinessReports_EquipmentSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropIndex(
            name: "IX_DailyVehicleEquipmentChecks_CopiedFromDailyVehicleEquipmentCheckId",
            table: "DailyVehicleEquipmentChecks");

        migrationBuilder.DropColumn(
            name: "AllowSameAsPreviousVehicleInspection",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "AllowSameAsPreviousEquipmentCheck",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "ShiftStartedAtUtc",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "ShiftEndsAtUtc",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "DraftExpiresAtUtc",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "LastSavedAtUtc",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "WorkflowStatus",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "LastSavedSection",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "VehicleSameAsPreviousShiftUsed",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "EquipmentSameAsPreviousShiftUsed",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "VehicleSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "EquipmentSameAsPreviousSourceReportId",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "VehicleSameAsPreviousAppliedAtUtc",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "EquipmentSameAsPreviousAppliedAtUtc",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "VehicleSameAsPreviousCopiedSummary",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "EquipmentSameAsPreviousCopiedSummary",
            table: "DailyVehicleReadinessReports");

        migrationBuilder.DropColumn(
            name: "SameAsPreviousShiftUsed",
            table: "DailyVehicleEquipmentChecks");

        migrationBuilder.DropColumn(
            name: "CopiedFromDailyVehicleEquipmentCheckId",
            table: "DailyVehicleEquipmentChecks");

        migrationBuilder.DropColumn(
            name: "SameAsPreviousAppliedAtUtc",
            table: "DailyVehicleEquipmentChecks");
    }
}
