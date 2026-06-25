using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations
{
    public partial class AddReadinessEngineSetupChoices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReadinessEngineSetupConfigured",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReadinessEngineSetupNotes",
                table: "Companies",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReadinessScoringActivated",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReadinessScoringSetupChoice",
                table: "Companies",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireSeniorApprovalForScoringChanges",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadinessEngineSetupConfigured",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ReadinessEngineSetupNotes",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ReadinessScoringActivated",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ReadinessScoringSetupChoice",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "RequireSeniorApprovalForScoringChanges",
                table: "Companies");
        }
    }
}
