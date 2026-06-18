using vector_app_local.Models;

namespace vector_app_local.Services;

public static class VehicleTaxonomyService
{
    public const string AmbulanceFunction = "Ambulance";
    public const string ResponseVehicleFunction = "Response Vehicle";

    public static string? InferFunction(string? vehicleType)
    {
        if (string.IsNullOrWhiteSpace(vehicleType))
        {
            return null;
        }

        var normalized = vehicleType.Trim();
        if (normalized.Contains("Ambulance", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Medic", StringComparison.OrdinalIgnoreCase))
        {
            return AmbulanceFunction;
        }

        if (normalized.Contains("Pickup", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Response", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("Rescue", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("RV", StringComparison.OrdinalIgnoreCase))
        {
            return ResponseVehicleFunction;
        }

        return null;
    }

    public static string? InferSubtype(string? vehicleType)
    {
        if (string.IsNullOrWhiteSpace(vehicleType))
        {
            return null;
        }

        var normalized = vehicleType.Trim();
        return string.Equals(normalized, "Vehicle", StringComparison.OrdinalIgnoreCase)
            ? null
            : normalized;
    }

    public static bool Backfill(Vehicle vehicle)
    {
        var changed = false;
        if (string.IsNullOrWhiteSpace(vehicle.VehicleFunction))
        {
            var function = InferFunction(vehicle.VehicleType);
            if (!string.IsNullOrWhiteSpace(function))
            {
                vehicle.VehicleFunction = function;
                changed = true;
            }
        }

        if (string.IsNullOrWhiteSpace(vehicle.VehicleSubtype) &&
            !string.IsNullOrWhiteSpace(vehicle.VehicleFunction))
        {
            var subtype = InferSubtype(vehicle.VehicleType);
            if (!string.IsNullOrWhiteSpace(subtype))
            {
                vehicle.VehicleSubtype = subtype;
                changed = true;
            }
        }

        return changed;
    }
}
