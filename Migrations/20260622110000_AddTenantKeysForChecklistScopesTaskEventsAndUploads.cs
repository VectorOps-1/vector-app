using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260622110000_AddTenantKeysForChecklistScopesTaskEventsAndUploads")]
public partial class AddTenantKeysForChecklistScopesTaskEventsAndUploads : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ChecklistPublishScopes_ChecklistTemplateId_ScopeType_IsActive",
            table: "ChecklistPublishScopes");

        migrationBuilder.AddColumn<int>(
            name: "CompanyId",
            table: "ChecklistPublishScopes",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("""
            UPDATE "ChecklistPublishScopes"
            SET "CompanyId" = COALESCE((
                SELECT "CompanyId"
                FROM "ChecklistTemplates"
                WHERE "ChecklistTemplates"."Id" = "ChecklistPublishScopes"."ChecklistTemplateId"
            ), 0)
            WHERE "CompanyId" = 0;
            """);

        migrationBuilder.CreateTable(
            name: "TaskEvents",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                TaskItemId = table.Column<int>(nullable: false),
                PerformedByUserId = table.Column<int>(nullable: false),
                EventType = table.Column<string>(maxLength: 80, nullable: false),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                EvidenceStoragePath = table.Column<string>(maxLength: 520, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaskEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_TaskEvents_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TaskEvents_AppUsers_PerformedByUserId",
                    column: x => x.PerformedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TaskEvents_TaskItems_TaskItemId",
                    column: x => x.TaskItemId,
                    principalTable: "TaskItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UploadedFiles",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                ChecklistTemplateId = table.Column<int>(nullable: false),
                OriginalFileName = table.Column<string>(maxLength: 260, nullable: false),
                ContentType = table.Column<string>(maxLength: 120, nullable: false),
                StoragePath = table.Column<string>(maxLength: 520, nullable: false),
                SizeBytes = table.Column<long>(nullable: false),
                UploadedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UploadedFiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_UploadedFiles_ChecklistTemplates_ChecklistTemplateId",
                    column: x => x.ChecklistTemplateId,
                    principalTable: "ChecklistTemplates",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UploadedFiles_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistPublishScopes_CompanyId_ChecklistTemplateId_ScopeType_IsActive",
            table: "ChecklistPublishScopes",
            columns: new[] { "CompanyId", "ChecklistTemplateId", "ScopeType", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_TaskEvents_CompanyId_TaskItemId_CreatedAtUtc",
            table: "TaskEvents",
            columns: new[] { "CompanyId", "TaskItemId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_TaskEvents_PerformedByUserId",
            table: "TaskEvents",
            column: "PerformedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_TaskEvents_TaskItemId",
            table: "TaskEvents",
            column: "TaskItemId");

        migrationBuilder.CreateIndex(
            name: "IX_UploadedFiles_ChecklistTemplateId",
            table: "UploadedFiles",
            column: "ChecklistTemplateId");

        migrationBuilder.CreateIndex(
            name: "IX_UploadedFiles_CompanyId_ChecklistTemplateId_UploadedAtUtc",
            table: "UploadedFiles",
            columns: new[] { "CompanyId", "ChecklistTemplateId", "UploadedAtUtc" });

        migrationBuilder.AddForeignKey(
            name: "FK_ChecklistPublishScopes_Companies_CompanyId",
            table: "ChecklistPublishScopes",
            column: "CompanyId",
            principalTable: "Companies",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ChecklistPublishScopes_Companies_CompanyId",
            table: "ChecklistPublishScopes");

        migrationBuilder.DropTable(
            name: "TaskEvents");

        migrationBuilder.DropTable(
            name: "UploadedFiles");

        migrationBuilder.DropIndex(
            name: "IX_ChecklistPublishScopes_CompanyId_ChecklistTemplateId_ScopeType_IsActive",
            table: "ChecklistPublishScopes");

        migrationBuilder.DropColumn(
            name: "CompanyId",
            table: "ChecklistPublishScopes");

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistPublishScopes_ChecklistTemplateId_ScopeType_IsActive",
            table: "ChecklistPublishScopes",
            columns: new[] { "ChecklistTemplateId", "ScopeType", "IsActive" });
    }
}
