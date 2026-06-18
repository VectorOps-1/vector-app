using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using vector_app_local.Data;

#nullable disable

namespace vector_app_local.Migrations;

[DbContext(typeof(VectorDbContext))]
[Migration("20260610160000_AddVehicleFunctionAndSubtype")]
public partial class AddVehicleFunctionAndSubtype : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "VehicleFunction",
            table: "Vehicles",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "VehicleSubtype",
            table: "Vehicles",
            maxLength: 120,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE Vehicles
            SET VehicleFunction = CASE
                WHEN VehicleType LIKE '%Ambulance%' OR VehicleType LIKE '%Medic%' THEN 'Ambulance'
                WHEN VehicleType LIKE '%Pickup%' OR VehicleType LIKE '%Response%' OR VehicleType LIKE '%Rescue%' OR VehicleType LIKE '%RV%' THEN 'Response Vehicle'
                ELSE VehicleFunction
            END
            WHERE VehicleFunction IS NULL;
            """);

        migrationBuilder.Sql("""
            UPDATE Vehicles
            SET VehicleSubtype = VehicleType
            WHERE VehicleSubtype IS NULL
                AND VehicleFunction IS NOT NULL
                AND VehicleType IS NOT NULL
                AND TRIM(VehicleType) <> ''
                AND VehicleType <> 'Vehicle';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "VehicleFunction",
            table: "Vehicles");

        migrationBuilder.DropColumn(
            name: "VehicleSubtype",
            table: "Vehicles");
    }
}
