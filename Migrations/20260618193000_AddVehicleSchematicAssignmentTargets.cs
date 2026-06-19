using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations
{
    public partial class AddVehicleSchematicAssignmentTargets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OperationalAreaId",
                table: "VehicleSchematicAssignments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "VehicleSchematicAssignments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSchematicAssignments_CompanyId_ScopeType_OperationalAreaId",
                table: "VehicleSchematicAssignments",
                columns: new[] { "CompanyId", "ScopeType", "OperationalAreaId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSchematicAssignments_CompanyId_ScopeType_VehicleId",
                table: "VehicleSchematicAssignments",
                columns: new[] { "CompanyId", "ScopeType", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSchematicAssignments_OperationalAreaId",
                table: "VehicleSchematicAssignments",
                column: "OperationalAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSchematicAssignments_VehicleId",
                table: "VehicleSchematicAssignments",
                column: "VehicleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleSchematicAssignments_CompanyId_ScopeType_OperationalAreaId",
                table: "VehicleSchematicAssignments");

            migrationBuilder.DropIndex(
                name: "IX_VehicleSchematicAssignments_CompanyId_ScopeType_VehicleId",
                table: "VehicleSchematicAssignments");

            migrationBuilder.DropIndex(
                name: "IX_VehicleSchematicAssignments_OperationalAreaId",
                table: "VehicleSchematicAssignments");

            migrationBuilder.DropIndex(
                name: "IX_VehicleSchematicAssignments_VehicleId",
                table: "VehicleSchematicAssignments");

            migrationBuilder.DropColumn(
                name: "OperationalAreaId",
                table: "VehicleSchematicAssignments");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "VehicleSchematicAssignments");
        }
    }
}
