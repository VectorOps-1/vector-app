using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vector_app_local.Migrations
{
    public partial class AddVehicleStructureSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleFunctionSetups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleFunctionSetups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleFunctionSetups_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleSubtypeSetups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleFunctionSetupId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleSubtypeSetups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleSubtypeSetups_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleSubtypeSetups_VehicleFunctionSetups_VehicleFunctionSetupId",
                        column: x => x.VehicleFunctionSetupId,
                        principalTable: "VehicleFunctionSetups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleFunctionSetups_CompanyId_Name",
                table: "VehicleFunctionSetups",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleFunctionSetups_CompanyId_Status_SortOrder",
                table: "VehicleFunctionSetups",
                columns: new[] { "CompanyId", "Status", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSubtypeSetups_CompanyId_Status_SortOrder",
                table: "VehicleSubtypeSetups",
                columns: new[] { "CompanyId", "Status", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSubtypeSetups_CompanyId_VehicleFunctionSetupId_Name",
                table: "VehicleSubtypeSetups",
                columns: new[] { "CompanyId", "VehicleFunctionSetupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSubtypeSetups_VehicleFunctionSetupId",
                table: "VehicleSubtypeSetups",
                column: "VehicleFunctionSetupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleSubtypeSetups");

            migrationBuilder.DropTable(
                name: "VehicleFunctionSetups");
        }
    }
}
