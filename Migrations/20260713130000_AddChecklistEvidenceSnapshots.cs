using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260713130000_AddChecklistEvidenceSnapshots")]
public partial class AddChecklistEvidenceSnapshots : Migration
{
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvidenceSnapshotJson",
                table: "DailyVehicleReadinessReports",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvidenceSnapshotVersion",
                table: "DailyVehicleReadinessReports",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EvidenceSnapshotCapturedAtUtc",
                table: "DailyVehicleReadinessReports",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvidenceSnapshotJson",
                table: "DailyVehicleReadinessReports");

            migrationBuilder.DropColumn(
                name: "EvidenceSnapshotVersion",
                table: "DailyVehicleReadinessReports");

            migrationBuilder.DropColumn(
                name: "EvidenceSnapshotCapturedAtUtc",
                table: "DailyVehicleReadinessReports");
        }
}
