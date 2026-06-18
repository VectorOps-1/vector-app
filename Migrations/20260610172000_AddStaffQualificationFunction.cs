using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260610172000_AddStaffQualificationFunction")]
public partial class AddStaffQualificationFunction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "QualificationFunction",
            table: "AppUsers",
            maxLength: 120,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AppUsers_CompanyId_QualificationFunction",
            table: "AppUsers",
            columns: new[] { "CompanyId", "QualificationFunction" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AppUsers_CompanyId_QualificationFunction",
            table: "AppUsers");

        migrationBuilder.DropColumn(
            name: "QualificationFunction",
            table: "AppUsers");
    }
}
