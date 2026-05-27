using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260527193000_AddMedicationItems")]
public partial class AddMedicationItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MedicationItems",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                CreatedByUserId = table.Column<int>(nullable: false),
                Name = table.Column<string>(maxLength: 180, nullable: false),
                MedicationCode = table.Column<string>(maxLength: 120, nullable: true),
                MedicationType = table.Column<string>(maxLength: 160, nullable: true),
                BatchNumber = table.Column<string>(maxLength: 160, nullable: true),
                StorageLocation = table.Column<string>(maxLength: 260, nullable: true),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                Quantity = table.Column<int>(nullable: true),
                ExpiryDate = table.Column<DateTime>(nullable: true),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MedicationItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_MedicationItems_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MedicationItems_AppUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MedicationItems_CompanyId",
            table: "MedicationItems",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_MedicationItems_CreatedByUserId",
            table: "MedicationItems",
            column: "CreatedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MedicationItems");
    }
}
