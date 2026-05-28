using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations;

public partial class AddChecklistTemplateTargeting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CompanyId",
            table: "ChecklistTemplates",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "ChecklistType",
            table: "ChecklistTemplates",
            maxLength: 80,
            nullable: false,
            defaultValue: "Vehicle");

        migrationBuilder.AddColumn<string>(
            name: "TargetVehicleType",
            table: "ChecklistTemplates",
            maxLength: 120,
            nullable: false,
            defaultValue: "All Vehicles");

        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "ChecklistTemplates",
            maxLength: 80,
            nullable: false,
            defaultValue: "Draft");

        migrationBuilder.AddColumn<bool>(
            name: "IsPublished",
            table: "ChecklistTemplates",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "PublishedAtUtc",
            table: "ChecklistTemplates",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "UpdatedAtUtc",
            table: "ChecklistTemplates",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistTemplates_CompanyId_ChecklistType_TargetVehicleType_Name",
            table: "ChecklistTemplates",
            columns: new[] { "CompanyId", "ChecklistType", "TargetVehicleType", "Name" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ChecklistTemplates_CompanyId_ChecklistType_TargetVehicleType_Name",
            table: "ChecklistTemplates");

        migrationBuilder.DropColumn(
            name: "CompanyId",
            table: "ChecklistTemplates");

        migrationBuilder.DropColumn(
            name: "ChecklistType",
            table: "ChecklistTemplates");

        migrationBuilder.DropColumn(
            name: "TargetVehicleType",
            table: "ChecklistTemplates");

        migrationBuilder.DropColumn(
            name: "Status",
            table: "ChecklistTemplates");

        migrationBuilder.DropColumn(
            name: "IsPublished",
            table: "ChecklistTemplates");

        migrationBuilder.DropColumn(
            name: "PublishedAtUtc",
            table: "ChecklistTemplates");

        migrationBuilder.DropColumn(
            name: "UpdatedAtUtc",
            table: "ChecklistTemplates");
    }
}
