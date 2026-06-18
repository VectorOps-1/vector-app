using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260530123000_AddStaffProfileFields")]
public partial class AddStaffProfileFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "StaffIdentifier",
            table: "AppUsers",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "NationalId",
            table: "AppUsers",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CellNumber",
            table: "AppUsers",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "AssignedOperationalAreaId",
            table: "AppUsers",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AppUsers_CompanyId_StaffIdentifier",
            table: "AppUsers",
            columns: new[] { "CompanyId", "StaffIdentifier" });

        migrationBuilder.CreateIndex(
            name: "IX_AppUsers_AssignedOperationalAreaId",
            table: "AppUsers",
            column: "AssignedOperationalAreaId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AppUsers_CompanyId_StaffIdentifier",
            table: "AppUsers");

        migrationBuilder.DropIndex(
            name: "IX_AppUsers_AssignedOperationalAreaId",
            table: "AppUsers");

        migrationBuilder.DropColumn(
            name: "StaffIdentifier",
            table: "AppUsers");

        migrationBuilder.DropColumn(
            name: "NationalId",
            table: "AppUsers");

        migrationBuilder.DropColumn(
            name: "CellNumber",
            table: "AppUsers");

        migrationBuilder.DropColumn(
            name: "AssignedOperationalAreaId",
            table: "AppUsers");
    }
}
