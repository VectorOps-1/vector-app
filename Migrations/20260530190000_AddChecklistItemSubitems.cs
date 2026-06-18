using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260530190000_AddChecklistItemSubitems")]
public partial class AddChecklistItemSubitems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ParentChecklistItemId",
            table: "ChecklistItems",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ChecklistItems_ParentChecklistItemId",
            table: "ChecklistItems",
            column: "ParentChecklistItemId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_ChecklistItems_ParentChecklistItemId", table: "ChecklistItems");
        migrationBuilder.DropColumn(name: "ParentChecklistItemId", table: "ChecklistItems");
    }
}
