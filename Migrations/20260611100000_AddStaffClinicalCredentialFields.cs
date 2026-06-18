using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260611100000_AddStaffClinicalCredentialFields")]
public partial class AddStaffClinicalCredentialFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PractitionerNumber",
            table: "AppUsers",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "AnnualLicenseExpiryDate",
            table: "AppUsers",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CpdComplianceStatus",
            table: "AppUsers",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CpdComplianceExpiryDate",
            table: "AppUsers",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AppUsers_CompanyId_PractitionerNumber",
            table: "AppUsers",
            columns: new[] { "CompanyId", "PractitionerNumber" });

        migrationBuilder.CreateIndex(
            name: "IX_AppUsers_CompanyId_AnnualLicenseExpiryDate",
            table: "AppUsers",
            columns: new[] { "CompanyId", "AnnualLicenseExpiryDate" });

        migrationBuilder.CreateIndex(
            name: "IX_AppUsers_CompanyId_CpdComplianceStatus",
            table: "AppUsers",
            columns: new[] { "CompanyId", "CpdComplianceStatus" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AppUsers_CompanyId_PractitionerNumber",
            table: "AppUsers");

        migrationBuilder.DropIndex(
            name: "IX_AppUsers_CompanyId_AnnualLicenseExpiryDate",
            table: "AppUsers");

        migrationBuilder.DropIndex(
            name: "IX_AppUsers_CompanyId_CpdComplianceStatus",
            table: "AppUsers");

        migrationBuilder.DropColumn(
            name: "PractitionerNumber",
            table: "AppUsers");

        migrationBuilder.DropColumn(
            name: "AnnualLicenseExpiryDate",
            table: "AppUsers");

        migrationBuilder.DropColumn(
            name: "CpdComplianceStatus",
            table: "AppUsers");

        migrationBuilder.DropColumn(
            name: "CpdComplianceExpiryDate",
            table: "AppUsers");
    }
}
