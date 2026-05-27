using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations;

public partial class AddIssueReports : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IssueReports",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                ReportedByUserId = table.Column<int>(nullable: false),
                AssignedToUserId = table.Column<int>(nullable: false),
                ResolvedByUserId = table.Column<int>(nullable: true),
                ManagerLevel = table.Column<string>(maxLength: 80, nullable: false),
                Module = table.Column<string>(maxLength: 80, nullable: false),
                IssueType = table.Column<string>(maxLength: 160, nullable: false),
                RelatedItem = table.Column<string>(maxLength: 260, nullable: true),
                Location = table.Column<string>(maxLength: 260, nullable: true),
                Severity = table.Column<string>(maxLength: 80, nullable: true),
                OperationalStatus = table.Column<string>(maxLength: 120, nullable: true),
                Description = table.Column<string>(maxLength: 1200, nullable: false),
                NotificationMethod = table.Column<string>(maxLength: 80, nullable: false),
                EvidenceFileNames = table.Column<string>(maxLength: 1200, nullable: true),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                ResolutionOutcome = table.Column<string>(maxLength: 80, nullable: true),
                ActionTaken = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                ResolvedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IssueReports", x => x.Id);
                table.ForeignKey(
                    name: "FK_IssueReports_AppUsers_AssignedToUserId",
                    column: x => x.AssignedToUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_IssueReports_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_IssueReports_AppUsers_ReportedByUserId",
                    column: x => x.ReportedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_IssueReports_AppUsers_ResolvedByUserId",
                    column: x => x.ResolvedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "IssueReportEvents",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                IssueReportId = table.Column<int>(nullable: false),
                PerformedByUserId = table.Column<int>(nullable: false),
                EventType = table.Column<string>(maxLength: 80, nullable: false),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IssueReportEvents", x => x.Id);
                table.ForeignKey(
                    name: "FK_IssueReportEvents_IssueReports_IssueReportId",
                    column: x => x.IssueReportId,
                    principalTable: "IssueReports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_IssueReportEvents_AppUsers_PerformedByUserId",
                    column: x => x.PerformedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IssueReportEvents_IssueReportId",
            table: "IssueReportEvents",
            column: "IssueReportId");

        migrationBuilder.CreateIndex(
            name: "IX_IssueReportEvents_PerformedByUserId",
            table: "IssueReportEvents",
            column: "PerformedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_IssueReports_AssignedToUserId",
            table: "IssueReports",
            column: "AssignedToUserId");

        migrationBuilder.CreateIndex(
            name: "IX_IssueReports_CompanyId",
            table: "IssueReports",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_IssueReports_ReportedByUserId",
            table: "IssueReports",
            column: "ReportedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_IssueReports_ResolvedByUserId",
            table: "IssueReports",
            column: "ResolvedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IssueReportEvents");

        migrationBuilder.DropTable(
            name: "IssueReports");
    }
}
