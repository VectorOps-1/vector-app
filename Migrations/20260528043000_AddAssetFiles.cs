using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations;

public partial class AddAssetFiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AssetFiles",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(nullable: false),
                UploadedByUserId = table.Column<int>(nullable: false),
                LinkedEntityType = table.Column<string>(maxLength: 80, nullable: false),
                LinkedEntityId = table.Column<int>(nullable: false),
                Category = table.Column<string>(maxLength: 120, nullable: false),
                OriginalFileName = table.Column<string>(maxLength: 260, nullable: false),
                ContentType = table.Column<string>(maxLength: 120, nullable: false),
                StorageProvider = table.Column<string>(maxLength: 40, nullable: false),
                StoragePath = table.Column<string>(maxLength: 520, nullable: false),
                SizeBytes = table.Column<long>(nullable: false),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                UploadedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AssetFiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_AssetFiles_AppUsers_UploadedByUserId",
                    column: x => x.UploadedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AssetFiles_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AssetFiles_CompanyId_LinkedEntityType_LinkedEntityId_Category",
            table: "AssetFiles",
            columns: new[] { "CompanyId", "LinkedEntityType", "LinkedEntityId", "Category" });

        migrationBuilder.CreateIndex(
            name: "IX_AssetFiles_UploadedByUserId",
            table: "AssetFiles",
            column: "UploadedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AssetFiles");
    }
}
