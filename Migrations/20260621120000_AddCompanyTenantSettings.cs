using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260621120000_AddCompanyTenantSettings")]
public partial class AddCompanyTenantSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TradingName",
            table: "Companies",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Country",
            table: "Companies",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Timezone",
            table: "Companies",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BrandingStatus",
            table: "Companies",
            maxLength: 80,
            nullable: false,
            defaultValue: "Incomplete");

        migrationBuilder.AddColumn<bool>(
            name: "LogoRemoved",
            table: "Companies",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TradingName",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "Country",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "Timezone",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "BrandingStatus",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "LogoRemoved",
            table: "Companies");
    }
}
