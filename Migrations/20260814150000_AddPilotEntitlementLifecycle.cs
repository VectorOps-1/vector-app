using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260814150000_AddPilotEntitlementLifecycle")]
public partial class AddPilotEntitlementLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PilotEntitlements",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                Tier = table.Column<string>(maxLength: 40, nullable: false),
                Status = table.Column<string>(maxLength: 24, nullable: false),
                StartsAtUtc = table.Column<DateTime>(nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: false),
                RevokedAtUtc = table.Column<DateTime>(nullable: true),
                CreatedByUserId = table.Column<int>(nullable: true),
                RevokedByUserId = table.Column<int>(nullable: true),
                RevocationReason = table.Column<string>(maxLength: 500, nullable: true),
                ConcurrencyToken = table.Column<string>(maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PilotEntitlements", x => x.Id);
                table.ForeignKey("FK_PilotEntitlements_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PilotEntitlements_AppUsers_CreatedByUserId", x => x.CreatedByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PilotEntitlements_AppUsers_RevokedByUserId", x => x.RevokedByUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PilotEntitlementEvents",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("Sqlite:Autoincrement", true),
                CompanyId = table.Column<int>(nullable: false),
                PilotEntitlementId = table.Column<int>(nullable: false),
                ActorUserId = table.Column<int>(nullable: true),
                EventType = table.Column<string>(maxLength: 40, nullable: false),
                PreviousStatus = table.Column<string>(maxLength: 24, nullable: true),
                NewStatus = table.Column<string>(maxLength: 24, nullable: false),
                PreviousExpiresAtUtc = table.Column<DateTime>(nullable: true),
                NewExpiresAtUtc = table.Column<DateTime>(nullable: false),
                Details = table.Column<string>(maxLength: 500, nullable: true),
                OccurredAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PilotEntitlementEvents", x => x.Id);
                table.ForeignKey("FK_PilotEntitlementEvents_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PilotEntitlementEvents_PilotEntitlements_PilotEntitlementId", x => x.PilotEntitlementId, "PilotEntitlements", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PilotEntitlementEvents_AppUsers_ActorUserId", x => x.ActorUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_PilotEntitlements_CompanyId", "PilotEntitlements", "CompanyId", unique: true);
        migrationBuilder.CreateIndex("IX_PilotEntitlements_CreatedByUserId", "PilotEntitlements", "CreatedByUserId");
        migrationBuilder.CreateIndex("IX_PilotEntitlements_RevokedByUserId", "PilotEntitlements", "RevokedByUserId");
        migrationBuilder.CreateIndex("IX_PilotEntitlementEvents_ActorUserId", "PilotEntitlementEvents", "ActorUserId");
        migrationBuilder.CreateIndex(
            name: "IX_PilotEntitlementEvents_CompanyId_OccurredAtUtc",
            table: "PilotEntitlementEvents",
            columns: new[] { "CompanyId", "OccurredAtUtc" });
        migrationBuilder.CreateIndex("IX_PilotEntitlementEvents_PilotEntitlementId", "PilotEntitlementEvents", "PilotEntitlementId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PilotEntitlementEvents");
        migrationBuilder.DropTable(name: "PilotEntitlements");
    }
}
