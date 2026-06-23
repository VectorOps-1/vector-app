using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260622143000_AddCompanySetupWizardProgress")]
public partial class AddCompanySetupWizardProgress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SetupWizardCurrentStepKey",
            table: "Companies",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SetupWizardCompletedStepKeys",
            table: "Companies",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "SetupWizardUpdatedAtUtc",
            table: "Companies",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SetupWizardCurrentStepKey",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "SetupWizardCompletedStepKeys",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "SetupWizardUpdatedAtUtc",
            table: "Companies");
    }
}
