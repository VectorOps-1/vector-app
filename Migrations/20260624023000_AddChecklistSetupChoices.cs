using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations
{
    public partial class AddChecklistSetupChoices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChecklistSetupConfigured",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ChecklistSetupNotes",
                table: "Companies",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DailyChecklistPublishScopeKeys",
                table: "Companies",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DailyChecklistSetupChoice",
                table: "Companies",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullAuditChecklistSetupChoice",
                table: "Companies",
                type: "TEXT",
                maxLength: 80,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChecklistSetupConfigured",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ChecklistSetupNotes",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DailyChecklistPublishScopeKeys",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DailyChecklistSetupChoice",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FullAuditChecklistSetupChoice",
                table: "Companies");
        }
    }
}
