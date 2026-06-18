using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260610183000_AddCustomDropdownOptions")]
public partial class AddCustomDropdownOptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CustomDropdownOptions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                DropdownKey = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CustomDropdownOptions", x => x.Id);
                table.ForeignKey(
                    name: "FK_CustomDropdownOptions_AppUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CustomDropdownOptions_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CustomDropdownOptions_CompanyId_DropdownKey_Value",
            table: "CustomDropdownOptions",
            columns: new[] { "CompanyId", "DropdownKey", "Value" });

        migrationBuilder.CreateIndex(
            name: "IX_CustomDropdownOptions_CreatedByUserId",
            table: "CustomDropdownOptions",
            column: "CreatedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CustomDropdownOptions");
    }
}
