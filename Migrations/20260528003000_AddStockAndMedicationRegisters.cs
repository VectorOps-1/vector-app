using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations;

public partial class AddStockAndMedicationRegisters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "LastAllocatedByUserId",
            table: "MedicationItems",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastAllocatedAtUtc",
            table: "MedicationItems",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LastAllocationLocation",
            table: "MedicationItems",
            maxLength: 260,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Schedule",
            table: "MedicationItems",
            maxLength: 80,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "StockItems",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                CreatedByUserId = table.Column<int>(nullable: false),
                LastMovedByUserId = table.Column<int>(nullable: true),
                ItemName = table.Column<string>(maxLength: 180, nullable: false),
                ItemType = table.Column<string>(maxLength: 160, nullable: true),
                BatchNumber = table.Column<string>(maxLength: 160, nullable: true),
                Quantity = table.Column<int>(nullable: false),
                Location = table.Column<string>(maxLength: 260, nullable: true),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                LastMovementType = table.Column<string>(maxLength: 120, nullable: true),
                LastMovementAtUtc = table.Column<DateTime>(nullable: true),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_StockItems_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StockItems_AppUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StockItems_AppUsers_LastMovedByUserId",
                    column: x => x.LastMovedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MedicationItems_LastAllocatedByUserId",
            table: "MedicationItems",
            column: "LastAllocatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_StockItems_CompanyId_ItemName_BatchNumber_Location",
            table: "StockItems",
            columns: new[] { "CompanyId", "ItemName", "BatchNumber", "Location" });

        migrationBuilder.CreateIndex(
            name: "IX_StockItems_CreatedByUserId",
            table: "StockItems",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_StockItems_LastMovedByUserId",
            table: "StockItems",
            column: "LastMovedByUserId");

        if (migrationBuilder.ActiveProvider != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            migrationBuilder.AddForeignKey(
                name: "FK_MedicationItems_AppUsers_LastAllocatedByUserId",
                table: "MedicationItems",
                column: "LastAllocatedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "StockItems");

        if (migrationBuilder.ActiveProvider != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationItems_AppUsers_LastAllocatedByUserId",
                table: "MedicationItems");
        }

        migrationBuilder.DropIndex(
            name: "IX_MedicationItems_LastAllocatedByUserId",
            table: "MedicationItems");

        migrationBuilder.DropColumn(
            name: "LastAllocatedByUserId",
            table: "MedicationItems");

        migrationBuilder.DropColumn(
            name: "LastAllocatedAtUtc",
            table: "MedicationItems");

        migrationBuilder.DropColumn(
            name: "LastAllocationLocation",
            table: "MedicationItems");

        migrationBuilder.DropColumn(
            name: "Schedule",
            table: "MedicationItems");
    }
}
