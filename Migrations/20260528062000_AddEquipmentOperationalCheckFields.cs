using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260528062000_AddEquipmentOperationalCheckFields")]
public partial class AddEquipmentOperationalCheckFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsOperational",
            table: "DailyVehicleEquipmentChecks",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "IssueNotes",
            table: "DailyVehicleEquipmentChecks",
            maxLength: 1200,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsOperational",
            table: "DailyVehicleEquipmentChecks");

        migrationBuilder.DropColumn(
            name: "IssueNotes",
            table: "DailyVehicleEquipmentChecks");
    }
}
