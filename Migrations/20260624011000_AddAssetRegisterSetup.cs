using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations
{
    public partial class AddAssetRegisterSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AssetRegisterSetupConfigured",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AssetRegisterSetupNotes",
                table: "Companies",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentRegisterSetupChoice",
                table: "Companies",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicationRegisterSetupChoice",
                table: "Companies",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffRegisterSetupChoice",
                table: "Companies",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StockRegisterSetupChoice",
                table: "Companies",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageLocationSetupChoice",
                table: "Companies",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleRegisterSetupChoice",
                table: "Companies",
                type: "TEXT",
                maxLength: 80,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetRegisterSetupConfigured",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "AssetRegisterSetupNotes",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "EquipmentRegisterSetupChoice",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MedicationRegisterSetupChoice",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StaffRegisterSetupChoice",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StockRegisterSetupChoice",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StorageLocationSetupChoice",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VehicleRegisterSetupChoice",
                table: "Companies");
        }
    }
}
