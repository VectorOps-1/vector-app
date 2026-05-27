using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations;

public partial class AddOperationalAreasAndAssetMovement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OperationalAreas",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                Name = table.Column<string>(maxLength: 180, nullable: false),
                AreaType = table.Column<string>(maxLength: 120, nullable: false),
                Address = table.Column<string>(maxLength: 360, nullable: true),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OperationalAreas", x => x.Id);
                table.ForeignKey(
                    name: "FK_OperationalAreas_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<int>(
            name: "CurrentOperationalAreaId",
            table: "Vehicles",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CurrentLocationDetail",
            table: "Vehicles",
            maxLength: 260,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "LastMovedByUserId",
            table: "Vehicles",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastMovedAtUtc",
            table: "Vehicles",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CurrentOperationalAreaId",
            table: "EquipmentItems",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CurrentLocationDetail",
            table: "EquipmentItems",
            maxLength: 260,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "LastMovedByUserId",
            table: "EquipmentItems",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastMovedAtUtc",
            table: "EquipmentItems",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CurrentOperationalAreaId",
            table: "StockItems",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "CurrentOperationalAreaId",
            table: "MedicationItems",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "AssetMovements",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                AssetType = table.Column<string>(maxLength: 80, nullable: false),
                AssetId = table.Column<int>(nullable: false),
                AssetLabel = table.Column<string>(maxLength: 260, nullable: false),
                FromOperationalAreaId = table.Column<int>(nullable: true),
                ToOperationalAreaId = table.Column<int>(nullable: false),
                FromLocationText = table.Column<string>(maxLength: 260, nullable: true),
                ToLocationText = table.Column<string>(maxLength: 260, nullable: true),
                QuantityMoved = table.Column<int>(nullable: true),
                MovementReason = table.Column<string>(maxLength: 1200, nullable: true),
                MovedByUserId = table.Column<int>(nullable: false),
                TaskItemId = table.Column<int>(nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AssetMovements", x => x.Id);
                table.ForeignKey(
                    name: "FK_AssetMovements_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AssetMovements_OperationalAreas_FromOperationalAreaId",
                    column: x => x.FromOperationalAreaId,
                    principalTable: "OperationalAreas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AssetMovements_AppUsers_MovedByUserId",
                    column: x => x.MovedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AssetMovements_TaskItems_TaskItemId",
                    column: x => x.TaskItemId,
                    principalTable: "TaskItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AssetMovements_OperationalAreas_ToOperationalAreaId",
                    column: x => x.ToOperationalAreaId,
                    principalTable: "OperationalAreas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OperationalAreas_CompanyId_Name",
            table: "OperationalAreas",
            columns: new[] { "CompanyId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Vehicles_CurrentOperationalAreaId",
            table: "Vehicles",
            column: "CurrentOperationalAreaId");

        migrationBuilder.CreateIndex(
            name: "IX_Vehicles_LastMovedByUserId",
            table: "Vehicles",
            column: "LastMovedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_EquipmentItems_CurrentOperationalAreaId",
            table: "EquipmentItems",
            column: "CurrentOperationalAreaId");

        migrationBuilder.CreateIndex(
            name: "IX_EquipmentItems_LastMovedByUserId",
            table: "EquipmentItems",
            column: "LastMovedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_StockItems_CurrentOperationalAreaId",
            table: "StockItems",
            column: "CurrentOperationalAreaId");

        migrationBuilder.CreateIndex(
            name: "IX_MedicationItems_CurrentOperationalAreaId",
            table: "MedicationItems",
            column: "CurrentOperationalAreaId");

        migrationBuilder.CreateIndex(
            name: "IX_AssetMovements_CompanyId_AssetType_AssetId_CreatedAtUtc",
            table: "AssetMovements",
            columns: new[] { "CompanyId", "AssetType", "AssetId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AssetMovements_FromOperationalAreaId",
            table: "AssetMovements",
            column: "FromOperationalAreaId");

        migrationBuilder.CreateIndex(
            name: "IX_AssetMovements_MovedByUserId",
            table: "AssetMovements",
            column: "MovedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_AssetMovements_TaskItemId",
            table: "AssetMovements",
            column: "TaskItemId");

        migrationBuilder.CreateIndex(
            name: "IX_AssetMovements_ToOperationalAreaId",
            table: "AssetMovements",
            column: "ToOperationalAreaId");

        if (migrationBuilder.ActiveProvider != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_OperationalAreas_CurrentOperationalAreaId",
                table: "Vehicles",
                column: "CurrentOperationalAreaId",
                principalTable: "OperationalAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_AppUsers_LastMovedByUserId",
                table: "Vehicles",
                column: "LastMovedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentItems_OperationalAreas_CurrentOperationalAreaId",
                table: "EquipmentItems",
                column: "CurrentOperationalAreaId",
                principalTable: "OperationalAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentItems_AppUsers_LastMovedByUserId",
                table: "EquipmentItems",
                column: "LastMovedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockItems_OperationalAreas_CurrentOperationalAreaId",
                table: "StockItems",
                column: "CurrentOperationalAreaId",
                principalTable: "OperationalAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationItems_OperationalAreas_CurrentOperationalAreaId",
                table: "MedicationItems",
                column: "CurrentOperationalAreaId",
                principalTable: "OperationalAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.ActiveProvider != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_OperationalAreas_CurrentOperationalAreaId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_AppUsers_LastMovedByUserId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentItems_OperationalAreas_CurrentOperationalAreaId",
                table: "EquipmentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentItems_AppUsers_LastMovedByUserId",
                table: "EquipmentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_OperationalAreas_CurrentOperationalAreaId",
                table: "StockItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MedicationItems_OperationalAreas_CurrentOperationalAreaId",
                table: "MedicationItems");
        }

        migrationBuilder.DropTable(
            name: "AssetMovements");

        migrationBuilder.DropIndex(
            name: "IX_Vehicles_CurrentOperationalAreaId",
            table: "Vehicles");

        migrationBuilder.DropIndex(
            name: "IX_Vehicles_LastMovedByUserId",
            table: "Vehicles");

        migrationBuilder.DropIndex(
            name: "IX_EquipmentItems_CurrentOperationalAreaId",
            table: "EquipmentItems");

        migrationBuilder.DropIndex(
            name: "IX_EquipmentItems_LastMovedByUserId",
            table: "EquipmentItems");

        migrationBuilder.DropIndex(
            name: "IX_StockItems_CurrentOperationalAreaId",
            table: "StockItems");

        migrationBuilder.DropIndex(
            name: "IX_MedicationItems_CurrentOperationalAreaId",
            table: "MedicationItems");

        migrationBuilder.DropColumn(
            name: "CurrentOperationalAreaId",
            table: "Vehicles");

        migrationBuilder.DropColumn(
            name: "CurrentLocationDetail",
            table: "Vehicles");

        migrationBuilder.DropColumn(
            name: "LastMovedByUserId",
            table: "Vehicles");

        migrationBuilder.DropColumn(
            name: "LastMovedAtUtc",
            table: "Vehicles");

        migrationBuilder.DropColumn(
            name: "CurrentOperationalAreaId",
            table: "EquipmentItems");

        migrationBuilder.DropColumn(
            name: "CurrentLocationDetail",
            table: "EquipmentItems");

        migrationBuilder.DropColumn(
            name: "LastMovedByUserId",
            table: "EquipmentItems");

        migrationBuilder.DropColumn(
            name: "LastMovedAtUtc",
            table: "EquipmentItems");

        migrationBuilder.DropColumn(
            name: "CurrentOperationalAreaId",
            table: "StockItems");

        migrationBuilder.DropColumn(
            name: "CurrentOperationalAreaId",
            table: "MedicationItems");

        migrationBuilder.DropTable(
            name: "OperationalAreas");
    }
}
