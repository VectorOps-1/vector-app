using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class LocationOptionService
{
    public const string VehicleLocationPrefix = "Vehicle: ";

    private readonly VectorDbContext _db;

    public LocationOptionService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<List<SelectListItem>> GetOperationalAreaOptionsAsync(int companyId)
    {
        return await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == companyId && area.Status == "Active")
            .OrderBy(area => area.AreaType)
            .ThenBy(area => area.Name)
            .Select(area => new SelectListItem
            {
                Value = area.Name,
                Text = area.Address == null
                    ? area.Name + " (" + area.AreaType + ")"
                    : area.Name + " (" + area.AreaType + ") - " + area.Address
            })
            .ToListAsync();
    }

    public async Task<List<SelectListItem>> GetAssetLocationOptionsAsync(int companyId)
    {
        var options = await GetOperationalAreaOptionsAsync(companyId);

        var storageLocations = await _db.StorageLocations
            .AsNoTracking()
            .Include(location => location.OperationalArea)
            .Where(location =>
                location.CompanyId == companyId &&
                location.Status == "Active" &&
                location.OperationalArea != null &&
                location.OperationalArea.Status == "Active")
            .OrderBy(location => location.OperationalArea!.AreaType)
            .ThenBy(location => location.OperationalArea!.Name)
            .ThenBy(location => location.StorageType)
            .ThenBy(location => location.Name)
            .Select(location => new
            {
                ParentName = location.OperationalArea == null ? string.Empty : location.OperationalArea.Name,
                location.Name,
                location.StorageType
            })
            .ToListAsync();

        options.AddRange(storageLocations.Select(location =>
        {
            var value = BuildStorageLocationValue(location.ParentName, location.Name);
            return new SelectListItem
            {
                Value = value,
                Text = $"{value} ({location.StorageType})"
            };
        }));

        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.CompanyId == companyId && vehicle.Status != "Deleted")
            .OrderBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .Select(vehicle => new
            {
                vehicle.Callsign,
                vehicle.RegistrationNumber
            })
            .ToListAsync();

        options.AddRange(vehicles.Select(vehicle =>
        {
            var value = BuildVehicleLocationValue(vehicle.Callsign, vehicle.RegistrationNumber);
            return new SelectListItem
            {
                Value = value,
                Text = value
            };
        }));

        return options;
    }

    public async Task<OperationalArea?> FindOperationalAreaAsync(int companyId, string? selectedLocation)
    {
        var location = NormalizeSelectedLocation(selectedLocation);
        if (string.IsNullOrWhiteSpace(location) || location.StartsWith(VehicleLocationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return await _db.OperationalAreas
            .FirstOrDefaultAsync(area =>
                area.CompanyId == companyId
                && area.Status == "Active"
                && area.Name == location);
    }

    public static string BuildVehicleLocationValue(string? callsign, string? registration)
    {
        var label = string.Join(
            " / ",
            new[] { callsign, registration }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));

        return VehicleLocationPrefix + (string.IsNullOrWhiteSpace(label) ? "Unspecified vehicle" : label);
    }

    public static string BuildStorageLocationValue(string? parentName, string? storageName)
    {
        var label = string.Join(
            " - ",
            new[] { parentName, storageName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));

        return string.IsNullOrWhiteSpace(label) ? "Storage space" : label;
    }

    public static string? NormalizeSelectedLocation(string? selectedLocation)
    {
        return string.IsNullOrWhiteSpace(selectedLocation) ? null : selectedLocation.Trim();
    }
}
