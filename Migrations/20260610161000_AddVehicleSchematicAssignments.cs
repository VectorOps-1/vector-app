using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260610161000_AddVehicleSchematicAssignments")]
public partial class AddVehicleSchematicAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "VehicleSchematicAssignments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                SchematicKey = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                ScopeType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                VehicleFunction = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                VehicleSubtype = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VehicleSchematicAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_VehicleSchematicAssignments_AppUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_VehicleSchematicAssignments_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_VehicleSchematicAssignments_CompanyId_SchematicKey",
            table: "VehicleSchematicAssignments",
            columns: new[] { "CompanyId", "SchematicKey" });

        migrationBuilder.CreateIndex(
            name: "IX_VehicleSchematicAssignments_CompanyId_ScopeType_VehicleFunction_VehicleSubtype",
            table: "VehicleSchematicAssignments",
            columns: new[] { "CompanyId", "ScopeType", "VehicleFunction", "VehicleSubtype" });

        migrationBuilder.CreateIndex(
            name: "IX_VehicleSchematicAssignments_CreatedByUserId",
            table: "VehicleSchematicAssignments",
            column: "CreatedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "VehicleSchematicAssignments");
    }
}
