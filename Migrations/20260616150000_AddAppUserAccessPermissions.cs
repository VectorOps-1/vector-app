using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations
{
    public partial class AddAppUserAccessPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUserAccessPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    AppUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PermissionKey = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserAccessPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserAccessPermissions_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppUserAccessPermissions_AppUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppUserAccessPermissions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserAccessPermissions_AppUserId",
                table: "AppUserAccessPermissions",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserAccessPermissions_CompanyId_AppUserId_PermissionKey",
                table: "AppUserAccessPermissions",
                columns: new[] { "CompanyId", "AppUserId", "PermissionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserAccessPermissions_UpdatedByUserId",
                table: "AppUserAccessPermissions",
                column: "UpdatedByUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUserAccessPermissions");
        }
    }
}
