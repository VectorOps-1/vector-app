using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations;

public partial class AddStockOrdering : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "StockOrders",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                RequestedByUserId = table.Column<int>(nullable: false),
                ApprovedBySeniorUserId = table.Column<int>(nullable: true),
                RegisterEntryAuthorisedUserId = table.Column<int>(nullable: true),
                SupplierName = table.Column<string>(maxLength: 180, nullable: false),
                SupplierEmail = table.Column<string>(maxLength: 180, nullable: false),
                DeliveryAddress = table.Column<string>(maxLength: 360, nullable: true),
                DeliveryInstructions = table.Column<string>(maxLength: 1200, nullable: true),
                OrderNotes = table.Column<string>(maxLength: 1200, nullable: true),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                EmailSubject = table.Column<string>(maxLength: 220, nullable: false),
                EmailBody = table.Column<string>(maxLength: 4000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                ApprovedAtUtc = table.Column<DateTime>(nullable: true),
                EmailSentAtUtc = table.Column<DateTime>(nullable: true),
                SupplierConfirmedAtUtc = table.Column<DateTime>(nullable: true),
                RegisterEnteredAtUtc = table.Column<DateTime>(nullable: true),
                AllocatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockOrders", x => x.Id);
                table.ForeignKey(
                    name: "FK_StockOrders_AppUsers_ApprovedBySeniorUserId",
                    column: x => x.ApprovedBySeniorUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StockOrders_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StockOrders_AppUsers_RegisterEntryAuthorisedUserId",
                    column: x => x.RegisterEntryAuthorisedUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StockOrders_AppUsers_RequestedByUserId",
                    column: x => x.RequestedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "StockOrderLines",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StockOrderId = table.Column<int>(nullable: false),
                ItemName = table.Column<string>(maxLength: 180, nullable: false),
                ItemType = table.Column<string>(maxLength: 160, nullable: true),
                QuantityRequested = table.Column<int>(nullable: false),
                QuantityConfirmed = table.Column<int>(nullable: true),
                BatchNumber = table.Column<string>(maxLength: 160, nullable: true),
                ExpiryDate = table.Column<DateTime>(nullable: true),
                RegisterLocation = table.Column<string>(maxLength: 260, nullable: true),
                QuantityAllocated = table.Column<int>(nullable: true),
                AllocationLocation = table.Column<string>(maxLength: 260, nullable: true),
                Notes = table.Column<string>(maxLength: 1200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockOrderLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_StockOrderLines_StockOrders_StockOrderId",
                    column: x => x.StockOrderId,
                    principalTable: "StockOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StockOrderLines_StockOrderId",
            table: "StockOrderLines",
            column: "StockOrderId");

        migrationBuilder.CreateIndex(
            name: "IX_StockOrders_ApprovedBySeniorUserId",
            table: "StockOrders",
            column: "ApprovedBySeniorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_StockOrders_CompanyId_Status_CreatedAtUtc",
            table: "StockOrders",
            columns: new[] { "CompanyId", "Status", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_StockOrders_RegisterEntryAuthorisedUserId",
            table: "StockOrders",
            column: "RegisterEntryAuthorisedUserId");

        migrationBuilder.CreateIndex(
            name: "IX_StockOrders_RequestedByUserId",
            table: "StockOrders",
            column: "RequestedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "StockOrderLines");

        migrationBuilder.DropTable(
            name: "StockOrders");
    }
}
