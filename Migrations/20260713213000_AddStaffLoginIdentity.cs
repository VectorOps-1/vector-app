using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260713213000_AddStaffLoginIdentity")]
public partial class AddStaffLoginIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<string>(maxLength: 450, nullable: false),
                CompanyId = table.Column<int>(nullable: false),
                AppUserId = table.Column<int>(nullable: false),
                IsLoginEnabled = table.Column<bool>(nullable: false),
                MustChangePassword = table.Column<bool>(nullable: false, defaultValue: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: false),
                AuthenticationProvider = table.Column<string>(maxLength: 80, nullable: false, defaultValue: "LocalIdentity"),
                UserName = table.Column<string>(maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                Email = table.Column<string>(maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(nullable: false),
                PasswordHash = table.Column<string>(nullable: true),
                SecurityStamp = table.Column<string>(nullable: true),
                ConcurrencyStamp = table.Column<string>(nullable: true),
                PhoneNumber = table.Column<string>(nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                TwoFactorEnabled = table.Column<bool>(nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                LockoutEnabled = table.Column<bool>(nullable: false),
                AccessFailedCount = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                table.ForeignKey("FK_AspNetUsers_AppUsers_AppUserId", x => x.AppUserId, "AppUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_AspNetUsers_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1").Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(maxLength: 450, nullable: false),
                ClaimType = table.Column<string>(nullable: true),
                ClaimValue = table.Column<string>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey("FK_AspNetUserClaims_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(maxLength: 128, nullable: false),
                ProviderKey = table.Column<string>(maxLength: 128, nullable: false),
                ProviderDisplayName = table.Column<string>(nullable: true),
                UserId = table.Column<string>(maxLength: 450, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey("FK_AspNetUserLogins_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<string>(maxLength: 450, nullable: false),
                LoginProvider = table.Column<string>(maxLength: 128, nullable: false),
                Name = table.Column<string>(maxLength: 128, nullable: false),
                Value = table.Column<string>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey("FK_AspNetUserTokens_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_AspNetUserClaims_UserId", "AspNetUserClaims", "UserId");
        migrationBuilder.CreateIndex("IX_AspNetUserLogins_UserId", "AspNetUserLogins", "UserId");
        migrationBuilder.CreateIndex("IX_AspNetUsers_AppUserId", "AspNetUsers", "AppUserId", unique: true);
        migrationBuilder.CreateIndex("IX_AspNetUsers_CompanyId_NormalizedEmail", "AspNetUsers", new[] { "CompanyId", "NormalizedEmail" });
        migrationBuilder.CreateIndex("EmailIndex", "AspNetUsers", "NormalizedEmail");
        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "NormalizedUserName",
            unique: true,
            filter: ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)
                ? "[NormalizedUserName] IS NOT NULL"
                : "NormalizedUserName IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AspNetUserClaims");
        migrationBuilder.DropTable("AspNetUserLogins");
        migrationBuilder.DropTable("AspNetUserTokens");
        migrationBuilder.DropTable("AspNetUsers");
    }
}
