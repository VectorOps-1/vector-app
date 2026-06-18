using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260530133000_AddVehicleIdentityAndServiceFields")]
public partial class AddVehicleIdentityAndServiceFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "VinNumber",
            table: "Vehicles",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ChassisNumber",
            table: "Vehicles",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastServiceDate",
            table: "Vehicles",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "VinNumber",
            table: "Vehicles");

        migrationBuilder.DropColumn(
            name: "ChassisNumber",
            table: "Vehicles");

        migrationBuilder.DropColumn(
            name: "LastServiceDate",
            table: "Vehicles");
    }
}
