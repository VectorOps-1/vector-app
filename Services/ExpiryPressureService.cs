using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class ExpiryPressureService
{
    public const int DefaultLookAheadDays = 60;

    private readonly VectorDbContext _db;

    public ExpiryPressureService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ExpiryPressureItem>> LoadForUserAsync(
        AppUser currentUser,
        DateTime? referenceDateUtc = null,
        int lookAheadDays = DefaultLookAheadDays)
    {
        var today = (referenceDateUtc ?? DateTime.UtcNow).Date;
        var cutoff = today.AddDays(Math.Max(0, lookAheadDays));
        var rows = new List<ExpiryPressureItem>();
        var isSenior = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);
        var isOpsManager = string.Equals(currentUser.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase);
        var isStaff = string.Equals(currentUser.AppRole?.Name, "Staff", StringComparison.OrdinalIgnoreCase);
        var assignedAreaIds = isSenior || isStaff
            ? new List<int>()
            : await LoadAssignedAreaIdsAsync(currentUser);

        if (isSenior || isOpsManager)
        {
            await AddVehicleRowsAsync(currentUser.CompanyId, isSenior, assignedAreaIds, cutoff, today, rows);
            await AddEquipmentRowsAsync(currentUser.CompanyId, isSenior, assignedAreaIds, cutoff, today, rows);
            await AddStockRowsAsync(currentUser.CompanyId, isSenior, assignedAreaIds, cutoff, today, rows);
            await AddMedicationRowsAsync(currentUser.CompanyId, isSenior, assignedAreaIds, cutoff, today, rows);
            await AddStaffComplianceRowsAsync(currentUser.CompanyId, isSenior, assignedAreaIds, cutoff, today, rows);
        }
        else
        {
            await AddOwnStaffComplianceRowsAsync(currentUser, cutoff, today, rows);
        }

        return rows
            .OrderBy(row => row.DueAtUtc)
            .ThenBy(row => row.AssetType)
            .ThenBy(row => row.AssetLabel)
            .ToList();
    }

    private async Task<List<int>> LoadAssignedAreaIdsAsync(AppUser currentUser)
    {
        var assignedAreaIds = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active")
            .Select(assignment => assignment.OperationalAreaId)
            .ToListAsync();

        if (currentUser.AssignedOperationalAreaId.HasValue &&
            !assignedAreaIds.Contains(currentUser.AssignedOperationalAreaId.Value))
        {
            assignedAreaIds.Add(currentUser.AssignedOperationalAreaId.Value);
        }

        return assignedAreaIds;
    }

    private async Task AddVehicleRowsAsync(
        int companyId,
        bool isSenior,
        IReadOnlyCollection<int> assignedAreaIds,
        DateTime cutoff,
        DateTime today,
        List<ExpiryPressureItem> rows)
    {
        var query = _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle =>
                vehicle.CompanyId == companyId &&
                vehicle.Status != "Deleted" &&
                ((vehicle.NextServiceDate.HasValue && vehicle.NextServiceDate.Value <= cutoff) ||
                    (vehicle.LicenseDiscExpiryDate.HasValue && vehicle.LicenseDiscExpiryDate.Value <= cutoff)));

        if (!isSenior)
        {
            query = query.Where(vehicle =>
                vehicle.CurrentOperationalAreaId.HasValue &&
                assignedAreaIds.Contains(vehicle.CurrentOperationalAreaId.Value));
        }

        var vehicles = await query.ToListAsync();
        foreach (var vehicle in vehicles)
        {
            var label = BuildVehicleLabel(vehicle);
            var location = BuildLocation(vehicle.CurrentOperationalArea?.Name, vehicle.CurrentLocationDetail);
            var detailUrl = BuildUrl("/EditVehicle", "vehicleId", vehicle.Id, "/ExpiryNotifications");

            if (vehicle.NextServiceDate.HasValue)
            {
                AddRow(rows, today, new ExpiryPressureItem
                {
                    AssetType = "Vehicle service",
                    AssetLabel = label,
                    Source = "Vehicle register",
                    Location = location,
                    OwnerName = "Register controlled",
                    DueAtUtc = vehicle.NextServiceDate.Value,
                    Status = vehicle.Status,
                    DetailUrl = detailUrl
                });
            }

            if (vehicle.LicenseDiscExpiryDate.HasValue)
            {
                AddRow(rows, today, new ExpiryPressureItem
                {
                    AssetType = "Vehicle licence disc",
                    AssetLabel = label,
                    Source = "Vehicle register",
                    Location = location,
                    OwnerName = "Register controlled",
                    DueAtUtc = vehicle.LicenseDiscExpiryDate.Value,
                    Status = vehicle.Status,
                    DetailUrl = detailUrl
                });
            }
        }
    }

    private async Task AddEquipmentRowsAsync(
        int companyId,
        bool isSenior,
        IReadOnlyCollection<int> assignedAreaIds,
        DateTime cutoff,
        DateTime today,
        List<ExpiryPressureItem> rows)
    {
        var query = _db.EquipmentItems
            .AsNoTracking()
            .Include(equipment => equipment.CurrentOperationalArea)
            .Where(equipment =>
                equipment.CompanyId == companyId &&
                equipment.Status != "Deleted" &&
                equipment.NextServiceDate.HasValue &&
                equipment.NextServiceDate.Value <= cutoff);

        if (!isSenior)
        {
            query = query.Where(equipment =>
                equipment.CurrentOperationalAreaId.HasValue &&
                assignedAreaIds.Contains(equipment.CurrentOperationalAreaId.Value));
        }

        var equipmentItems = await query.ToListAsync();
        foreach (var equipment in equipmentItems)
        {
            AddRow(rows, today, new ExpiryPressureItem
            {
                AssetType = "Equipment service",
                AssetLabel = equipment.Name + (string.IsNullOrWhiteSpace(equipment.SerialOrAssetId) ? string.Empty : $" / {equipment.SerialOrAssetId}"),
                Source = "Equipment register",
                Location = BuildLocation(equipment.CurrentOperationalArea?.Name, equipment.CurrentLocationDetail),
                OwnerName = "Register controlled",
                DueAtUtc = equipment.NextServiceDate!.Value,
                Status = equipment.Status,
                DetailUrl = BuildUrl("/EditEquipmentItem", "equipmentItemId", equipment.Id, "/ExpiryNotifications")
            });
        }
    }

    private async Task AddStockRowsAsync(
        int companyId,
        bool isSenior,
        IReadOnlyCollection<int> assignedAreaIds,
        DateTime cutoff,
        DateTime today,
        List<ExpiryPressureItem> rows)
    {
        var query = _db.StockItems
            .AsNoTracking()
            .Include(stock => stock.CurrentOperationalArea)
            .Where(stock =>
                stock.CompanyId == companyId &&
                stock.Status != "Deleted" &&
                stock.ExpiryDate.HasValue &&
                stock.ExpiryDate.Value <= cutoff);

        if (!isSenior)
        {
            query = query.Where(stock =>
                stock.CurrentOperationalAreaId.HasValue &&
                assignedAreaIds.Contains(stock.CurrentOperationalAreaId.Value));
        }

        var stockItems = await query.ToListAsync();
        foreach (var stock in stockItems)
        {
            AddRow(rows, today, new ExpiryPressureItem
            {
                AssetType = "Stock expiry",
                AssetLabel = stock.ItemName + (string.IsNullOrWhiteSpace(stock.BatchNumber) ? string.Empty : $" / {stock.BatchNumber}"),
                Source = "Stock register",
                Location = BuildLocation(stock.CurrentOperationalArea?.Name, stock.Location),
                OwnerName = $"Quantity: {stock.Quantity}",
                DueAtUtc = stock.ExpiryDate!.Value,
                Status = stock.Status,
                DetailUrl = BuildUrl("/EditStockItem", "stockItemId", stock.Id, "/ExpiryNotifications")
            });
        }
    }

    private async Task AddMedicationRowsAsync(
        int companyId,
        bool isSenior,
        IReadOnlyCollection<int> assignedAreaIds,
        DateTime cutoff,
        DateTime today,
        List<ExpiryPressureItem> rows)
    {
        var query = _db.MedicationItems
            .AsNoTracking()
            .Include(medication => medication.CurrentOperationalArea)
            .Where(medication =>
                medication.CompanyId == companyId &&
                medication.Status != "Deleted" &&
                medication.ExpiryDate.HasValue &&
                medication.ExpiryDate.Value <= cutoff);

        if (!isSenior)
        {
            query = query.Where(medication =>
                medication.CurrentOperationalAreaId.HasValue &&
                assignedAreaIds.Contains(medication.CurrentOperationalAreaId.Value));
        }

        var medicationItems = await query.ToListAsync();
        foreach (var medication in medicationItems)
        {
            AddRow(rows, today, new ExpiryPressureItem
            {
                AssetType = "Medication expiry",
                AssetLabel = medication.Name + (string.IsNullOrWhiteSpace(medication.BatchNumber) ? string.Empty : $" / {medication.BatchNumber}"),
                Source = "Medication register",
                Location = BuildLocation(medication.CurrentOperationalArea?.Name, medication.StorageLocation),
                OwnerName = medication.Quantity.HasValue ? $"Quantity: {medication.Quantity.Value}" : "Quantity not set",
                DueAtUtc = medication.ExpiryDate!.Value,
                Status = medication.Status,
                DetailUrl = BuildUrl("/EditMedicationItem", "medicationItemId", medication.Id, "/ExpiryNotifications")
            });
        }
    }

    private async Task AddStaffComplianceRowsAsync(
        int companyId,
        bool isSenior,
        IReadOnlyCollection<int> assignedAreaIds,
        DateTime cutoff,
        DateTime today,
        List<ExpiryPressureItem> rows)
    {
        var query = _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AssignedOperationalArea)
            .Include(user => user.AppRole)
            .Where(user =>
                user.CompanyId == companyId &&
                user.Status != "Deleted" &&
                ((user.AnnualLicenseExpiryDate.HasValue && user.AnnualLicenseExpiryDate.Value <= cutoff) ||
                    (user.CpdComplianceExpiryDate.HasValue && user.CpdComplianceExpiryDate.Value <= cutoff)));

        if (!isSenior)
        {
            query = query.Where(user =>
                user.AssignedOperationalAreaId.HasValue &&
                assignedAreaIds.Contains(user.AssignedOperationalAreaId.Value));
        }

        var users = await query.ToListAsync();
        foreach (var user in users)
        {
            AddStaffRows(user, cutoff, today, rows);
        }
    }

    private async Task AddOwnStaffComplianceRowsAsync(
        AppUser currentUser,
        DateTime cutoff,
        DateTime today,
        List<ExpiryPressureItem> rows)
    {
        var user = await _db.AppUsers
            .AsNoTracking()
            .Include(item => item.AssignedOperationalArea)
            .Include(item => item.AppRole)
            .FirstOrDefaultAsync(item =>
                item.Id == currentUser.Id &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (user is not null)
        {
            AddStaffRows(user, cutoff, today, rows);
        }
    }

    private static void AddStaffRows(AppUser user, DateTime cutoff, DateTime today, List<ExpiryPressureItem> rows)
    {
        var location = user.AssignedOperationalArea?.Name ?? "Unassigned";
        var label = user.FullName + (string.IsNullOrWhiteSpace(user.StaffIdentifier) ? string.Empty : $" / {user.StaffIdentifier}");
        var detailUrl = BuildUrl("/EditStaffProfile", "staffUserId", user.Id, "/ExpiryNotifications");

        if (user.AnnualLicenseExpiryDate.HasValue && user.AnnualLicenseExpiryDate.Value <= cutoff)
        {
            AddRow(rows, today, new ExpiryPressureItem
            {
                AssetType = "Staff licence",
                AssetLabel = label,
                Source = "Staff register",
                Location = location,
                OwnerName = user.AppRole?.Name ?? "Staff profile",
                DueAtUtc = user.AnnualLicenseExpiryDate.Value,
                Status = user.Status,
                DetailUrl = detailUrl
            });
        }

        if (user.CpdComplianceExpiryDate.HasValue && user.CpdComplianceExpiryDate.Value <= cutoff)
        {
            AddRow(rows, today, new ExpiryPressureItem
            {
                AssetType = "Staff CPD",
                AssetLabel = label,
                Source = "Staff register",
                Location = location,
                OwnerName = string.IsNullOrWhiteSpace(user.CpdComplianceStatus) ? "CPD status not set" : user.CpdComplianceStatus,
                DueAtUtc = user.CpdComplianceExpiryDate.Value,
                Status = user.Status,
                DetailUrl = detailUrl
            });
        }
    }

    private static void AddRow(List<ExpiryPressureItem> rows, DateTime today, ExpiryPressureItem row)
    {
        var daysUntilDue = (row.DueAtUtc.Date - today).Days;
        row.DaysUntilDue = daysUntilDue;
        row.AlertBand = BuildAlertBand(daysUntilDue);
        row.ActionState = BuildActionState(daysUntilDue);
        rows.Add(row);
    }

    private static string BuildAlertBand(int daysUntilDue)
    {
        if (daysUntilDue < 0)
        {
            return "Overdue / expired";
        }

        if (daysUntilDue <= 15)
        {
            return "Due within 15 days";
        }

        if (daysUntilDue <= 30)
        {
            return "Due within 30 days";
        }

        return "Due within 60 days";
    }

    private static string BuildActionState(int daysUntilDue)
    {
        if (daysUntilDue < 0)
        {
            return $"Overdue by {Math.Abs(daysUntilDue)} day(s).";
        }

        if (daysUntilDue == 0)
        {
            return "Due today.";
        }

        return $"Due in {daysUntilDue} day(s).";
    }

    private static string BuildVehicleLabel(Vehicle vehicle)
    {
        return $"{vehicle.RegistrationNumber} / {vehicle.Callsign} ({VehicleTaxonomyService.DisplayClassification(vehicle)})";
    }

    private static string BuildLocation(string? area, string? detail)
    {
        if (!string.IsNullOrWhiteSpace(area) && !string.IsNullOrWhiteSpace(detail))
        {
            return $"{area} - {detail}";
        }

        return string.IsNullOrWhiteSpace(area)
            ? string.IsNullOrWhiteSpace(detail) ? "Unallocated" : detail.Trim()
            : area.Trim();
    }

    private static string BuildUrl(string page, string idKey, int id, string returnUrl)
    {
        return $"{page}?{idKey}={id}&returnUrl={Uri.EscapeDataString(returnUrl)}";
    }
}

public sealed class ExpiryPressureItem
{
    public string AssetType { get; set; } = string.Empty;
    public string AssetLabel { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime DueAtUtc { get; set; }
    public int DaysUntilDue { get; set; }
    public string AlertBand { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ActionState { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
}
