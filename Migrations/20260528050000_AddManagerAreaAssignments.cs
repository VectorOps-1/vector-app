using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations;

public partial class AddManagerAreaAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ManagerOperationalAreaAssignments",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                ManagerUserId = table.Column<int>(nullable: false),
                OperationalAreaId = table.Column<int>(nullable: false),
                AssignedByUserId = table.Column<int>(nullable: true),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                AssignedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ManagerOperationalAreaAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_ManagerOperationalAreaAssignments_AppUsers_AssignedByUserId",
                    column: x => x.AssignedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ManagerOperationalAreaAssignments_AppUsers_ManagerUserId",
                    column: x => x.ManagerUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ManagerOperationalAreaAssignments_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ManagerOperationalAreaAssignments_OperationalAreas_OperationalAreaId",
                    column: x => x.OperationalAreaId,
                    principalTable: "OperationalAreas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ManagerOperationalAreaAssignments_AssignedByUserId",
            table: "ManagerOperationalAreaAssignments",
            column: "AssignedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ManagerOperationalAreaAssignments_CompanyId_ManagerUserId_OperationalAreaId",
            table: "ManagerOperationalAreaAssignments",
            columns: new[] { "CompanyId", "ManagerUserId", "OperationalAreaId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ManagerOperationalAreaAssignments_ManagerUserId",
            table: "ManagerOperationalAreaAssignments",
            column: "ManagerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ManagerOperationalAreaAssignments_OperationalAreaId",
            table: "ManagerOperationalAreaAssignments",
            column: "OperationalAreaId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ManagerOperationalAreaAssignments");
    }
}
