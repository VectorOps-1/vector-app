using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260718134500_AddChecklistImportDraftLinkage")]
public partial class AddChecklistImportDraftLinkage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ProposedRecordName",
            table: "ImportBatches",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SourceImportBatchId",
            table: "ChecklistTemplates",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistTemplates_CompanyId_SourceImportBatchId",
            table: "ChecklistTemplates",
            columns: new[] { "CompanyId", "SourceImportBatchId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ChecklistTemplates_CompanyId_SourceImportBatchId",
            table: "ChecklistTemplates");

        if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            migrationBuilder.Sql("ALTER TABLE ChecklistTemplates DROP COLUMN SourceImportBatchId;");
            migrationBuilder.Sql("ALTER TABLE ImportBatches DROP COLUMN ProposedRecordName;");
        }
        else
        {
            migrationBuilder.DropColumn(
                name: "SourceImportBatchId",
                table: "ChecklistTemplates");

            migrationBuilder.DropColumn(
                name: "ProposedRecordName",
                table: "ImportBatches");
        }
    }
}
