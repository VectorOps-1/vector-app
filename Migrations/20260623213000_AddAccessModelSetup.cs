using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations
{
    public partial class AddAccessModelSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AccessModelDefaultsConfigured",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CompanyOwnerDefaultPermissionKeys",
                table: "Companies",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationalManagerDefaultPermissionKeys",
                table: "Companies",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationalManagerScopeBehavior",
                table: "Companies",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeniorManagerDefaultPermissionKeys",
                table: "Companies",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffDefaultPermissionKeys",
                table: "Companies",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessModelDefaultsConfigured",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CompanyOwnerDefaultPermissionKeys",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "OperationalManagerDefaultPermissionKeys",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "OperationalManagerScopeBehavior",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SeniorManagerDefaultPermissionKeys",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StaffDefaultPermissionKeys",
                table: "Companies");
        }
    }
}
