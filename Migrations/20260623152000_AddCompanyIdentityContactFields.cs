using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260623152000_AddCompanyIdentityContactFields")]
public partial class AddCompanyIdentityContactFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ContactEmail",
            table: "Companies",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ContactPhone",
            table: "Companies",
            maxLength: 60,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Region",
            table: "Companies",
            maxLength: 120,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ContactEmail",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "ContactPhone",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "Region",
            table: "Companies");
    }
}
