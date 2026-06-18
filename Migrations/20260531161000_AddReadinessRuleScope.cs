using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260531161000_AddReadinessRuleScope")]
public partial class AddReadinessRuleScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ReadinessScope",
            table: "ReadinessEngineRules",
            maxLength: 80,
            nullable: false,
            defaultValue: "Active shift");

        migrationBuilder.Sql("""
            UPDATE ReadinessEngineRules
            SET ReadinessScope = 'Assigned to active vehicle'
            WHERE AssetType IN ('Vehicle', 'Equipment');
            """);

        migrationBuilder.Sql("""
            UPDATE ReadinessEngineRules
            SET ReadinessScope = 'Required stock minimum'
            WHERE AssetType IN ('Stock', 'Medication');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ReadinessScope",
            table: "ReadinessEngineRules");
    }
}
