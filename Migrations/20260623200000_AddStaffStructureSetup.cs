using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations
{
    public partial class AddStaffStructureSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StaffAnnualLicenseExpiryRequired",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StaffCpdTrackingRequired",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StaffDefaultProfileFields",
                table: "Companies",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffIdFormat",
                table: "Companies",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StaffPractitionerNumberRequired",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "StaffQualificationSetups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffQualificationSetups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffQualificationSetups_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffQualificationSetups_CompanyId_Name",
                table: "StaffQualificationSetups",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffQualificationSetups_CompanyId_Status_SortOrder",
                table: "StaffQualificationSetups",
                columns: new[] { "CompanyId", "Status", "SortOrder" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffQualificationSetups");

            migrationBuilder.DropColumn(
                name: "StaffAnnualLicenseExpiryRequired",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StaffCpdTrackingRequired",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StaffDefaultProfileFields",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StaffIdFormat",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "StaffPractitionerNumberRequired",
                table: "Companies");
        }
    }
}
