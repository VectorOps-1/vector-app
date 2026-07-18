using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260718152000_AddImportMappingProfiles")]
public partial class AddImportMappingProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ImportMappingProfiles",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                Name = table.Column<string>(maxLength: 160, nullable: false),
                TargetType = table.Column<string>(maxLength: 80, nullable: false),
                HeadingSignature = table.Column<string>(maxLength: 128, nullable: false),
                MappingJson = table.Column<string>(nullable: false, defaultValue: "[]"),
                CreatedByUserId = table.Column<int>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImportMappingProfiles", x => x.Id);
                table.ForeignKey("FK_ImportMappingProfiles_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ImportMappingProfiles_AppUsers_CreatedByUserId", x => x.CreatedByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_ImportMappingProfiles_CreatedByUserId", "ImportMappingProfiles", "CreatedByUserId");
        migrationBuilder.CreateIndex("IX_ImportMappingProfiles_CompanyId_TargetType_HeadingSignature", "ImportMappingProfiles", new[] { "CompanyId", "TargetType", "HeadingSignature" });
        migrationBuilder.CreateIndex("IX_ImportMappingProfiles_CompanyId_TargetType_Name", "ImportMappingProfiles", new[] { "CompanyId", "TargetType", "Name" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ImportMappingProfiles");
    }
}
