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

    public static string? CurrentFunction(Vehicle vehicle)
    {
        return NormalizeOptional(vehicle.VehicleFunction) ?? InferFunction(vehicle.VehicleType);
    }

    public static string? CurrentSubtype(Vehicle vehicle)
    {
        return NormalizeOptional(vehicle.VehicleSubtype) ?? InferSubtype(vehicle.VehicleType);
    }

    public static string DisplayClassification(Vehicle vehicle)
    {
        return CurrentSubtype(vehicle) ??
            CurrentFunction(vehicle) ??
            NormalizeOptional(vehicle.VehicleType) ??
            "Vehicle";
    }

    public static bool MatchesClassification(Vehicle vehicle, string? target)
    {
        var normalizedTarget = NormalizeOptional(target);
        if (normalizedTarget is null)
        {
            return false;
        }

        if (string.Equals(normalizedTarget, "All Vehicles", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedTarget, "All", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(CurrentFunction(vehicle), normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(CurrentSubtype(vehicle), normalizedTarget, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizeOptional(vehicle.VehicleType), normalizedTarget, StringComparison.OrdinalIgnoreCase);
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
