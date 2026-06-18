using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260529110000_AddCompanyWorkspaceAccess")]
public partial class AddCompanyWorkspaceAccess : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "WorkspaceSlug",
            table: "Companies",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "WorkspaceAccessCode",
            table: "Companies",
            maxLength: 120,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Companies_WorkspaceSlug",
            table: "Companies",
            column: "WorkspaceSlug",
            unique: true,
            filter: "WorkspaceSlug IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Companies_WorkspaceSlug",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "WorkspaceSlug",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "WorkspaceAccessCode",
            table: "Companies");
    }
}
