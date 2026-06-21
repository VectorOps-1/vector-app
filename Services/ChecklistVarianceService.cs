using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class ChecklistVarianceService
{
    private readonly VectorDbContext _db;

    public ChecklistVarianceService(VectorDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateEquipmentVarianceAlertsAsync(int reportId, int performedByUserId)
    {
        var performedByCompanyId = await _db.AppUsers
            .AsNoTracking()
            .Where(user => user.Id == performedByUserId && user.Status == "Active")
            .Select(user => (int?)user.CompanyId)
            .FirstOrDefaultAsync();

        if (!performedByCompanyId.HasValue)
        {
            return 0;
        }

        var report = await _db.DailyVehicleReadinessReports
            .Include(item => item.Vehicle)
            .Include(item => item.PerformedByUser)
                .ThenInclude(user => user!.AppRole)
            .Include(item => item.EquipmentChecks)
                .ThenInclude(check => check.EquipmentItem)
            .FirstOrDefaultAsync(item =>
                item.Id == reportId &&
                item.CompanyId == performedByCompanyId.Value &&
                item.PerformedByUserId == performedByUserId);

        if (report is null || report.EquipmentChecks.Count == 0)
        {
            return 0;
        }

        var previousReport = await _db.DailyVehicleReadinessReports
            .AsNoTracking()
            .Include(item => item.EquipmentChecks)
            .Where(item =>
                item.CompanyId == report.CompanyId &&
                item.VehicleId == report.VehicleId &&
                item.PerformedByUserId != performedByUserId &&
                item.InspectionDateUtc < report.InspectionDateUtc)
            .OrderByDescending(item => item.InspectionDateUtc)
            .FirstOrDefaultAsync();

        if (previousReport is null || previousReport.EquipmentChecks.Count == 0)
        {
            return 0;
        }

        var assignedToUserId = await ResolveSuperiorUserIdAsync(report);
        var createdCount = 0;

        foreach (var check in report.EquipmentChecks)
        {
            var previous = FindMatchingPreviousCheck(check, previousReport.EquipmentChecks);
            if (previous is null)
            {
                continue;
            }

            createdCount += await AddAlertIfChangedAsync(
                report,
                check,
                assignedToUserId,
                "serialOrAssetId",
                "Equipment serial / asset ID changed",
                previous.SerialOrAssetId,
                check.SerialOrAssetId,
                check.SerialOrAssetId,
                requiresRegisterUpdate: true);

            createdCount += await AddAlertIfChangedAsync(
                report,
                check,
                assignedToUserId,
                "nextService",
                "Equipment next service date changed",
                previous.NextServiceDateAtCheck?.ToString("yyyy-MM-dd"),
                check.NextServiceDateAtCheck?.ToString("yyyy-MM-dd"),
                check.EquipmentItem?.NextServiceDate?.ToString("yyyy-MM-dd"),
                requiresRegisterUpdate: true);

            createdCount += await AddAlertIfChangedAsync(
                report,
                check,
                assignedToUserId,
                "battery",
                "Equipment battery status changed",
                previous.BatteryStatus,
                check.BatteryStatus,
                null,
                requiresRegisterUpdate: false);

            createdCount += await AddAlertIfChangedAsync(
                report,
                check,
                assignedToUserId,
                "operational",
                "Equipment operational status changed",
                previous.IsOperational ? "Yes" : "No",
                check.IsOperational ? "Yes" : "No",
                null,
                requiresRegisterUpdate: false);
        }

        if (createdCount > 0)
        {
            await _db.SaveChangesAsync();
        }

        return createdCount;
    }

    public async Task<bool> ReviewVarianceAlertAsync(int alertId, AppUser reviewer, bool approve, string? reviewNote)
    {
        var alert = await _db.ChecklistVarianceAlerts
            .Include(item => item.DailyVehicleReadinessReport)
                .ThenInclude(report => report!.Vehicle)
            .Include(item => item.DailyVehicleEquipmentCheck)
                .ThenInclude(check => check!.EquipmentItem)
            .FirstOrDefaultAsync(item =>
                item.Id == alertId &&
                item.CompanyId == reviewer.CompanyId &&
                item.Status == "Open");

        if (alert is null)
        {
            return false;
        }

        var reviewerIsSenior = CurrentUserService.IsSeniorAccessRole(reviewer.AppRole?.Name);
        if (!reviewerIsSenior && alert.AssignedToUserId != reviewer.Id)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        alert.Status = approve ? "Approved" : "Rejected";
        alert.ReviewedByUserId = reviewer.Id;
        alert.ReviewedAtUtc = now;
        alert.ReviewNote = NormalizeOptional(reviewNote);

        if (approve && alert.RequiresRegisterUpdate)
        {
            ApplyRegisterUpdate(alert, reviewer, now);
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = reviewer.CompanyId,
            AppUserId = reviewer.Id,
            Action = approve ? "Checklist variance approved" : "Checklist variance rejected",
            EntityType = "ChecklistVarianceAlert",
            EntityId = alert.Id,
            Details = $"{reviewer.FullName} {(approve ? "approved" : "rejected")} {alert.AlertType} for {alert.AssetLabel}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
        return true;
    }

    private void ApplyRegisterUpdate(ChecklistVarianceAlert alert, AppUser reviewer, DateTime now)
    {
        var check = alert.DailyVehicleEquipmentCheck;
        var equipment = check?.EquipmentItem;
        var vehicle = alert.DailyVehicleReadinessReport?.Vehicle;
        if (check is null || equipment is null)
        {
            alert.Status = "RegisterUpdateSkipped";
            return;
        }

        var changedRegister = false;
        if (string.Equals(alert.FieldKey, "nextService", StringComparison.OrdinalIgnoreCase) &&
            check.NextServiceDateAtCheck.HasValue &&
            equipment.NextServiceDate?.Date != check.NextServiceDateAtCheck.Value.Date)
        {
            equipment.NextServiceDate = check.NextServiceDateAtCheck;
            changedRegister = true;
        }

        if (vehicle?.CurrentOperationalAreaId is not null)
        {
            var oldAreaId = equipment.CurrentOperationalAreaId;
            var oldLocation = equipment.CurrentLocationDetail;
            var newLocation = $"Vehicle {vehicle.Callsign} ({vehicle.RegistrationNumber})";
            if (oldAreaId != vehicle.CurrentOperationalAreaId ||
                !string.Equals(oldLocation, newLocation, StringComparison.OrdinalIgnoreCase))
            {
                equipment.CurrentOperationalAreaId = vehicle.CurrentOperationalAreaId;
                equipment.CurrentLocationDetail = newLocation;
                equipment.LastMovedByUserId = reviewer.Id;
                equipment.LastMovedAtUtc = now;
                changedRegister = true;

                _db.AssetMovements.Add(new AssetMovement
                {
                    CompanyId = alert.CompanyId,
                    AssetType = "equipment",
                    AssetId = equipment.Id,
                    AssetLabel = $"{equipment.Name} {equipment.SerialOrAssetId}".Trim(),
                    FromOperationalAreaId = oldAreaId,
                    ToOperationalAreaId = vehicle.CurrentOperationalAreaId.Value,
                    FromLocationText = oldLocation,
                    ToLocationText = newLocation,
                    MovementReason = "Approved checklist variance",
                    MovedByUserId = reviewer.Id,
                    CreatedAtUtc = now
                });
            }
        }

        if (changedRegister)
        {
            equipment.UpdatedAtUtc = now;
            alert.RegisterUpdatedAtUtc = now;
        }
        else
        {
            alert.Status = "RegisterAlreadyCurrent";
        }
    }

    private async Task<int> AddAlertIfChangedAsync(
        DailyVehicleReadinessReport report,
        DailyVehicleEquipmentCheck check,
        int? assignedToUserId,
        string fieldKey,
        string alertType,
        string? previousValue,
        string? newValue,
        string? registerValue,
        bool requiresRegisterUpdate)
    {
        if (string.Equals(NormalizeForCompare(previousValue), NormalizeForCompare(newValue), StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var duplicateExists = await _db.ChecklistVarianceAlerts.AnyAsync(item =>
            item.CompanyId == report.CompanyId &&
            item.DailyVehicleEquipmentCheckId == check.Id &&
            item.FieldKey == fieldKey &&
            item.Status == "Open");

        if (duplicateExists)
        {
            return 0;
        }

        _db.ChecklistVarianceAlerts.Add(new ChecklistVarianceAlert
        {
            CompanyId = report.CompanyId,
            DailyVehicleReadinessReportId = report.Id,
            DailyVehicleEquipmentCheckId = check.Id,
            VehicleId = report.VehicleId,
            DetectedForUserId = report.PerformedByUserId,
            AssignedToUserId = assignedToUserId,
            AlertType = alertType,
            FieldKey = fieldKey,
            AssetLabel = $"{check.EquipmentName} {check.SerialOrAssetId}".Trim(),
            PreviousValue = NormalizeOptional(previousValue),
            NewValue = NormalizeOptional(newValue),
            RegisterValue = NormalizeOptional(registerValue),
            Severity = requiresRegisterUpdate ? "Manager review" : "Review",
            Status = "Open",
            RequiresRegisterUpdate = requiresRegisterUpdate,
            CreatedAtUtc = DateTime.UtcNow
        });

        return 1;
    }

    private async Task<int?> ResolveSuperiorUserIdAsync(DailyVehicleReadinessReport report)
    {
        var user = report.PerformedByUser;
        var roleName = user?.AppRole?.Name;
        if (user is null)
        {
            return null;
        }

        if (string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            var areaId = user.AssignedOperationalAreaId ?? report.Vehicle?.CurrentOperationalAreaId;
            if (areaId is not null)
            {
                var assignedManager = await _db.ManagerOperationalAreaAssignments
                    .AsNoTracking()
                    .Include(item => item.ManagerUser)
                        .ThenInclude(manager => manager!.AppRole)
                    .Where(item =>
                        item.CompanyId == report.CompanyId &&
                        item.OperationalAreaId == areaId &&
                        item.Status == "Active" &&
                        item.ManagerUser != null &&
                        item.ManagerUser.Status == "Active")
                    .Select(item => item.ManagerUser)
                    .FirstOrDefaultAsync();

                if (assignedManager is not null)
                {
                    return assignedManager.Id;
                }
            }
        }

        if (!CurrentUserService.IsSeniorAccessRole(roleName))
        {
            return await _db.AppUsers
                .AsNoTracking()
                .Include(item => item.AppRole)
                .Where(item =>
                    item.CompanyId == report.CompanyId &&
                    item.Status == "Active" &&
                    item.AppRole != null &&
                    (item.AppRole.Name == "Senior Management" || item.AppRole.Name == "Company Owner"))
                .OrderByDescending(item => item.AppRole!.Name == "Company Owner")
                .Select(item => (int?)item.Id)
                .FirstOrDefaultAsync();
        }

        return await _db.AppUsers
            .AsNoTracking()
            .Include(item => item.AppRole)
            .Where(item =>
                item.CompanyId == report.CompanyId &&
                item.Status == "Active" &&
                item.Id != user.Id &&
                item.AppRole != null &&
                item.AppRole.Name == "Company Owner")
            .Select(item => (int?)item.Id)
            .FirstOrDefaultAsync();
    }

    private static DailyVehicleEquipmentCheck? FindMatchingPreviousCheck(
        DailyVehicleEquipmentCheck current,
        IEnumerable<DailyVehicleEquipmentCheck> previousChecks)
    {
        return previousChecks
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault(item =>
                (current.VehicleEquipmentAssignmentId is not null && item.VehicleEquipmentAssignmentId == current.VehicleEquipmentAssignmentId) ||
                (current.EquipmentItemId is not null && item.EquipmentItemId == current.EquipmentItemId) ||
                (!string.IsNullOrWhiteSpace(current.SerialOrAssetId) && item.SerialOrAssetId == current.SerialOrAssetId) ||
                (!string.IsNullOrWhiteSpace(current.EquipmentName) && item.EquipmentName == current.EquipmentName));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeForCompare(string? value)
    {
        return NormalizeOptional(value) ?? string.Empty;
    }
}
