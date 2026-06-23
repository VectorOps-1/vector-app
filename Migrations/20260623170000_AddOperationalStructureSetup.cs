using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260623170000_AddOperationalStructureSetup")]
public partial class AddOperationalStructureSetup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OperationalStructureMode",
            table: "Companies",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ParentOperationalAreaId",
            table: "OperationalAreas",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "StorageLocations",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                OperationalAreaId = table.Column<int>(nullable: false),
                Name = table.Column<string>(maxLength: 180, nullable: false),
                StorageType = table.Column<string>(maxLength: 120, nullable: false),
                Status = table.Column<string>(maxLength: 80, nullable: false),
                Notes = table.Column<string>(maxLength: 1200, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StorageLocations", x => x.Id);
                table.ForeignKey(
                    name: "FK_StorageLocations_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StorageLocations_OperationalAreas_OperationalAreaId",
                    column: x => x.OperationalAreaId,
                    principalTable: "OperationalAreas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OperationalAreas_CompanyId_AreaType_ParentOperationalAreaId",
            table: "OperationalAreas",
            columns: new[] { "CompanyId", "AreaType", "ParentOperationalAreaId" });

        migrationBuilder.CreateIndex(
            name: "IX_OperationalAreas_ParentOperationalAreaId",
            table: "OperationalAreas",
            column: "ParentOperationalAreaId");

        migrationBuilder.CreateIndex(
            name: "IX_StorageLocations_CompanyId_OperationalAreaId_Name",
            table: "StorageLocations",
            columns: new[] { "CompanyId", "OperationalAreaId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StorageLocations_OperationalAreaId",
            table: "StorageLocations",
            column: "OperationalAreaId");

        migrationBuilder.AddForeignKey(
            name: "FK_OperationalAreas_OperationalAreas_ParentOperationalAreaId",
            table: "OperationalAreas",
            column: "ParentOperationalAreaId",
            principalTable: "OperationalAreas",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_OperationalAreas_OperationalAreas_ParentOperationalAreaId",
            table: "OperationalAreas");

        migrationBuilder.DropTable(
            name: "StorageLocations");

        migrationBuilder.DropIndex(
            name: "IX_OperationalAreas_CompanyId_AreaType_ParentOperationalAreaId",
            table: "OperationalAreas");

        migrationBuilder.DropIndex(
            name: "IX_OperationalAreas_ParentOperationalAreaId",
            table: "OperationalAreas");

        migrationBuilder.DropColumn(
            name: "OperationalStructureMode",
            table: "Companies");

        migrationBuilder.DropColumn(
            name: "ParentOperationalAreaId",
            table: "OperationalAreas");
    }
}
