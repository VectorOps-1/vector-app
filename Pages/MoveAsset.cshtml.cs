using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class MoveAssetModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public MoveAssetModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string AssetType { get; set; } = AssetTypes.Vehicle;
    [BindProperty(SupportsGet = true)] public int? TaskId { get; set; }
    [BindProperty(SupportsGet = true)] public bool TaskAccess { get; set; }
    [BindProperty] public int AssetId { get; set; }
    [BindProperty] public int ToOperationalAreaId { get; set; }
    [BindProperty] public string? LocationDetail { get; set; }
    [BindProperty] public int? QuantityMoved { get; set; }
    [BindProperty] public string? MovementReason { get; set; }
    [BindProperty] public int AssignedToUserId { get; set; }
    [BindProperty] public DateTime? ExpiresAtLocal { get; set; }

    public List<SelectListItem> AssetOptions { get; private set; } = new();
    public List<SelectListItem> OperationalAreaOptions { get; private set; } = new();
    public List<SelectListItem> Recipients { get; private set; } = new();
    public List<MovementRecord> RecentMovements { get; private set; } = new();

    public string AssetDisplayName => AssetTypes.DisplayName(AssetType);
    public string PageTitle => $"Move / Reallocate {AssetDisplayName}";
    public bool IsQuantityAsset => NormalizedAssetType == AssetTypes.Stock || NormalizedAssetType == AssetTypes.Medication;
    public bool ActionSaved { get; private set; }
    public string? StatusMessage { get; private set; }

    private string NormalizedAssetType => AssetTypes.Normalize(AssetType);

    public async Task<IActionResult> OnGetAsync()
    {
        ApplyRequestedAssetType();
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await DevelopmentDatabase.RepairSqliteDevelopmentSchemaAsync(_db);

        if (TaskId.HasValue)
        {
            await ApplyTaskReferenceAsync(currentUser);
        }

        await LoadPageDataAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string submitAction)
    {
        AssetType = NormalizedAssetType;
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await DevelopmentDatabase.RepairSqliteDevelopmentSchemaAsync(_db);

        var destination = await _db.OperationalAreas
            .FirstOrDefaultAsync(area =>
                area.Id == ToOperationalAreaId &&
                area.CompanyId == currentUser.CompanyId &&
                area.Status == "Active");

        if (destination is null)
        {
            ModelState.AddModelError(nameof(ToOperationalAreaId), "Select an active destination from Master Setup.");
        }

        if (AssetId <= 0)
        {
            ModelState.AddModelError(nameof(AssetId), $"Select the {AssetDisplayName.ToLowerInvariant()} to move.");
        }

        if (QuantityMoved.HasValue && QuantityMoved.Value < 0)
        {
            ModelState.AddModelError(nameof(QuantityMoved), "Quantity cannot be negative.");
        }

        var assetLabel = AssetId <= 0
            ? null
            : await GetAssetLabelAsync(currentUser.CompanyId, AssetId, AssetType);

        if (AssetId > 0 && assetLabel is null)
        {
            ModelState.AddModelError(nameof(AssetId), $"Selected {AssetDisplayName.ToLowerInvariant()} was not found.");
        }

        var sendAsTask = string.Equals(submitAction, "send-task", StringComparison.OrdinalIgnoreCase);
        if (sendAsTask && AssignedToUserId <= 0)
        {
            ModelState.AddModelError(nameof(AssignedToUserId), "Select the person who must complete this movement task.");
        }

        if (sendAsTask && AssignedToUserId > 0)
        {
            var recipientExists = await _db.AppUsers.AnyAsync(user =>
                user.Id == AssignedToUserId &&
                user.CompanyId == currentUser.CompanyId &&
                user.Status == "Active");

            if (!recipientExists)
            {
                ModelState.AddModelError(nameof(AssignedToUserId), "Select an active recipient in this company.");
            }
        }

        if (!ModelState.IsValid || destination is null || assetLabel is null)
        {
            await LoadPageDataAsync(currentUser.CompanyId);
            return Page();
        }

        if (sendAsTask)
        {
            await CreateMovementTaskAsync(currentUser, destination, assetLabel);
            ActionSaved = true;
            StatusMessage = $"{AssetDisplayName} movement task sent.";
            await LoadPageDataAsync(currentUser.CompanyId);
            return Page();
        }

        var movement = await ApplyMovementAsync(currentUser, destination);
        if (movement is null)
        {
            ModelState.AddModelError(nameof(AssetId), $"Selected {AssetDisplayName.ToLowerInvariant()} was not found.");
            await LoadPageDataAsync(currentUser.CompanyId);
            return Page();
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = $"{AssetDisplayName} moved",
            EntityType = "AssetMovement",
            EntityId = movement.Id,
            Details = $"{movement.AssetLabel} moved from {movement.FromLocationText ?? "Unallocated"} to {movement.ToLocationText}.",
            CreatedAtUtc = DateTime.UtcNow
        });

        if (TaskId.HasValue)
        {
            var task = await _db.TaskItems.FirstOrDefaultAsync(taskItem =>
                taskItem.Id == TaskId.Value &&
                taskItem.CompanyId == currentUser.CompanyId &&
                taskItem.AssignedToUserId == currentUser.Id &&
                taskItem.Status == "Open");

            if (task is not null)
            {
                task.Status = "Completed";
                task.CompletedAtUtc = DateTime.UtcNow;

                _db.TaskEvents.Add(new TaskEvent
                {
                    TaskItemId = task.Id,
                    PerformedByUserId = currentUser.Id,
                    EventType = "Completed",
                    Notes = $"Movement completed: {movement.AssetLabel} to {movement.ToLocationText}.",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = $"{movement.AssetLabel} moved to {movement.ToLocationText}.";
        await LoadPageDataAsync(currentUser.CompanyId);
        return Page();
    }

    private void ApplyRequestedAssetType()
    {
        var assetQuery = Request.Query["asset"].ToString();
        AssetType = string.IsNullOrWhiteSpace(assetQuery)
            ? NormalizedAssetType
            : AssetTypes.Normalize(assetQuery);
    }

    private async Task ApplyTaskReferenceAsync(AppUser currentUser)
    {
        if (!TaskId.HasValue)
        {
            return;
        }

        var taskId = TaskId.Value;
        var task = await _db.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(taskItem =>
                taskItem.Id == taskId &&
                taskItem.CompanyId == currentUser.CompanyId &&
                taskItem.AssignedToUserId == currentUser.Id &&
                taskItem.Status == "Open");

        if (task is null || string.IsNullOrWhiteSpace(task.RelatedItemReference))
        {
            return;
        }

        var parts = task.RelatedItemReference.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return;
        }

        AssetType = AssetTypes.Normalize(parts[0]);

        if (int.TryParse(parts[1], out var assetId))
        {
            AssetId = assetId;
        }

        if (int.TryParse(parts[2], out var destinationId))
        {
            ToOperationalAreaId = destinationId;
        }

        if (parts.Length > 3 && int.TryParse(parts[3], out var quantity))
        {
            QuantityMoved = quantity;
        }
    }

    private async Task CreateMovementTaskAsync(AppUser currentUser, OperationalArea destination, string assetLabel)
    {
        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            CompanyId = currentUser.CompanyId,
            AssignedToUserId = AssignedToUserId,
            AssignedByUserId = currentUser.Id,
            ActionType = AssetTypes.TaskAction(AssetType),
            RelatedItemReference = BuildTaskReference(destination.Id),
            InstructionMessage = BuildTaskInstruction(assetLabel, destination),
            Status = "Open",
            CreatedAtUtc = now,
            ExpiresAtUtc = ExpiresAtLocal.HasValue ? DateTime.SpecifyKind(ExpiresAtLocal.Value, DateTimeKind.Local).ToUniversalTime() : null
        };

        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        _db.TaskEvents.Add(new TaskEvent
        {
            TaskItemId = task.Id,
            PerformedByUserId = currentUser.Id,
            EventType = "Sent",
            Notes = task.InstructionMessage,
            CreatedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Movement task sent",
            EntityType = "TaskItem",
            EntityId = task.Id,
            Details = $"{task.ActionType}: {assetLabel} to {destination.Name}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
    }

    private async Task<AssetMovement?> ApplyMovementAsync(AppUser currentUser, OperationalArea destination)
    {
        return AssetType switch
        {
            AssetTypes.Equipment => await MoveEquipmentAsync(currentUser, destination),
            AssetTypes.Stock => await MoveStockAsync(currentUser, destination),
            AssetTypes.Medication => await MoveMedicationAsync(currentUser, destination),
            _ => await MoveVehicleAsync(currentUser, destination)
        };
    }

    private async Task<AssetMovement?> MoveVehicleAsync(AppUser currentUser, OperationalArea destination)
    {
        var vehicle = await _db.Vehicles
            .Include(item => item.CurrentOperationalArea)
            .FirstOrDefaultAsync(item => item.Id == AssetId && item.CompanyId == currentUser.CompanyId);

        if (vehicle is null)
        {
            return null;
        }

        var fromAreaId = vehicle.CurrentOperationalAreaId;
        var fromText = BuildExistingLocation(vehicle.CurrentOperationalArea?.Name, vehicle.CurrentLocationDetail);
        var toText = BuildDestinationLocation(destination.Name);
        var label = $"{vehicle.RegistrationNumber} / {vehicle.Callsign}";

        vehicle.CurrentOperationalAreaId = destination.Id;
        vehicle.CurrentLocationDetail = NormalizeOptional(LocationDetail);
        vehicle.LastMovedByUserId = currentUser.Id;
        vehicle.LastMovedAtUtc = DateTime.UtcNow;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        return await AddMovementAsync(currentUser, destination.Id, fromAreaId, fromText, toText, label);
    }

    private async Task<AssetMovement?> MoveEquipmentAsync(AppUser currentUser, OperationalArea destination)
    {
        var equipment = await _db.EquipmentItems
            .Include(item => item.CurrentOperationalArea)
            .FirstOrDefaultAsync(item => item.Id == AssetId && item.CompanyId == currentUser.CompanyId);

        if (equipment is null)
        {
            return null;
        }

        var fromAreaId = equipment.CurrentOperationalAreaId;
        var fromText = BuildExistingLocation(equipment.CurrentOperationalArea?.Name, equipment.CurrentLocationDetail);
        var toText = BuildDestinationLocation(destination.Name);
        var label = string.IsNullOrWhiteSpace(equipment.SerialOrAssetId)
            ? equipment.Name
            : $"{equipment.Name} / {equipment.SerialOrAssetId}";

        equipment.CurrentOperationalAreaId = destination.Id;
        equipment.CurrentLocationDetail = NormalizeOptional(LocationDetail);
        equipment.LastMovedByUserId = currentUser.Id;
        equipment.LastMovedAtUtc = DateTime.UtcNow;
        equipment.UpdatedAtUtc = DateTime.UtcNow;

        return await AddMovementAsync(currentUser, destination.Id, fromAreaId, fromText, toText, label);
    }

    private async Task<AssetMovement?> MoveStockAsync(AppUser currentUser, OperationalArea destination)
    {
        var stock = await _db.StockItems
            .Include(item => item.CurrentOperationalArea)
            .FirstOrDefaultAsync(item => item.Id == AssetId && item.CompanyId == currentUser.CompanyId);

        if (stock is null)
        {
            return null;
        }

        var fromAreaId = stock.CurrentOperationalAreaId;
        var fromText = BuildExistingLocation(stock.CurrentOperationalArea?.Name, stock.Location);
        var toText = BuildDestinationLocation(destination.Name);
        var label = string.IsNullOrWhiteSpace(stock.BatchNumber)
            ? stock.ItemName
            : $"{stock.ItemName} / Batch {stock.BatchNumber}";

        stock.CurrentOperationalAreaId = destination.Id;
        stock.Location = toText;
        stock.LastMovedByUserId = currentUser.Id;
        stock.LastMovementType = "Move / Reallocate";
        stock.LastMovementAtUtc = DateTime.UtcNow;
        stock.UpdatedAtUtc = DateTime.UtcNow;

        return await AddMovementAsync(currentUser, destination.Id, fromAreaId, fromText, toText, label);
    }

    private async Task<AssetMovement?> MoveMedicationAsync(AppUser currentUser, OperationalArea destination)
    {
        var medication = await _db.MedicationItems
            .Include(item => item.CurrentOperationalArea)
            .FirstOrDefaultAsync(item => item.Id == AssetId && item.CompanyId == currentUser.CompanyId);

        if (medication is null)
        {
            return null;
        }

        var fromAreaId = medication.CurrentOperationalAreaId;
        var fromText = BuildExistingLocation(medication.CurrentOperationalArea?.Name, medication.StorageLocation);
        var toText = BuildDestinationLocation(destination.Name);
        var label = string.IsNullOrWhiteSpace(medication.BatchNumber)
            ? medication.Name
            : $"{medication.Name} / Batch {medication.BatchNumber}";

        medication.CurrentOperationalAreaId = destination.Id;
        medication.StorageLocation = toText;
        medication.LastAllocatedByUserId = currentUser.Id;
        medication.LastAllocationLocation = toText;
        medication.LastAllocatedAtUtc = DateTime.UtcNow;
        medication.UpdatedAtUtc = DateTime.UtcNow;

        return await AddMovementAsync(currentUser, destination.Id, fromAreaId, fromText, toText, label);
    }

    private async Task<AssetMovement> AddMovementAsync(
        AppUser currentUser,
        int destinationId,
        int? fromAreaId,
        string? fromText,
        string toText,
        string assetLabel)
    {
        var movement = new AssetMovement
        {
            CompanyId = currentUser.CompanyId,
            AssetType = AssetType,
            AssetId = AssetId,
            AssetLabel = assetLabel,
            FromOperationalAreaId = fromAreaId,
            ToOperationalAreaId = destinationId,
            FromLocationText = fromText,
            ToLocationText = toText,
            QuantityMoved = QuantityMoved,
            MovementReason = NormalizeOptional(MovementReason),
            MovedByUserId = currentUser.Id,
            TaskItemId = TaskId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.AssetMovements.Add(movement);
        await _db.SaveChangesAsync();
        return movement;
    }

    private async Task<string?> GetAssetLabelAsync(int companyId, int assetId, string assetType)
    {
        return assetType switch
        {
            AssetTypes.Equipment => await _db.EquipmentItems
                .Where(item => item.CompanyId == companyId && item.Id == assetId)
                .Select(item => item.SerialOrAssetId == null || item.SerialOrAssetId == string.Empty ? item.Name : item.Name + " / " + item.SerialOrAssetId)
                .FirstOrDefaultAsync(),
            AssetTypes.Stock => await _db.StockItems
                .Where(item => item.CompanyId == companyId && item.Id == assetId)
                .Select(item => item.BatchNumber == null || item.BatchNumber == string.Empty ? item.ItemName : item.ItemName + " / Batch " + item.BatchNumber)
                .FirstOrDefaultAsync(),
            AssetTypes.Medication => await _db.MedicationItems
                .Where(item => item.CompanyId == companyId && item.Id == assetId)
                .Select(item => item.BatchNumber == null || item.BatchNumber == string.Empty ? item.Name : item.Name + " / Batch " + item.BatchNumber)
                .FirstOrDefaultAsync(),
            _ => await _db.Vehicles
                .Where(item => item.CompanyId == companyId && item.Id == assetId)
                .Select(item => item.RegistrationNumber + " / " + item.Callsign)
                .FirstOrDefaultAsync()
        };
    }

    private async Task LoadPageDataAsync(int companyId)
    {
        OperationalAreaOptions = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == companyId && area.Status == "Active")
            .OrderBy(area => area.AreaType)
            .ThenBy(area => area.Name)
            .Select(area => new SelectListItem
            {
                Value = area.Id.ToString(),
                Text = area.Address == null ? $"{area.Name} ({area.AreaType})" : $"{area.Name} ({area.AreaType}) - {area.Address}"
            })
            .ToListAsync();

        AssetOptions = AssetType switch
        {
            AssetTypes.Equipment => await LoadEquipmentOptionsAsync(companyId),
            AssetTypes.Stock => await LoadStockOptionsAsync(companyId),
            AssetTypes.Medication => await LoadMedicationOptionsAsync(companyId),
            _ => await LoadVehicleOptionsAsync(companyId)
        };

        Recipients = await _db.AppUsers
            .AsNoTracking()
            .Include(user => user.AppRole)
            .Where(user => user.CompanyId == companyId && user.Status == "Active")
            .OrderBy(user => user.FullName)
            .Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = user.AppRole == null ? user.FullName : $"{user.FullName} ({user.AppRole.Name})"
            })
            .ToListAsync();

        RecentMovements = await _db.AssetMovements
            .AsNoTracking()
            .Include(movement => movement.MovedByUser)
            .Where(movement => movement.CompanyId == companyId && movement.AssetType == AssetType)
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .Take(8)
            .Select(movement => new MovementRecord
            {
                AssetLabel = movement.AssetLabel,
                FromLocation = movement.FromLocationText,
                ToLocation = movement.ToLocationText,
                QuantityMoved = movement.QuantityMoved,
                MovedByName = movement.MovedByUser == null ? null : movement.MovedByUser.FullName,
                CreatedAtUtc = movement.CreatedAtUtc
            })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> LoadVehicleOptionsAsync(int companyId)
    {
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle => vehicle.CompanyId == companyId && vehicle.Status != "Deleted")
            .OrderBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync();

        return vehicles
            .Select(vehicle => new SelectListItem
            {
                Value = vehicle.Id.ToString(),
                Text = vehicle.RegistrationNumber + " / " + vehicle.Callsign + " - " + (vehicle.CurrentOperationalArea != null ? vehicle.CurrentOperationalArea.Name : (vehicle.CurrentLocationDetail ?? "Unallocated"))
            })
            .ToList();
    }

    private async Task<List<SelectListItem>> LoadEquipmentOptionsAsync(int companyId)
    {
        var equipmentItems = await _db.EquipmentItems
            .AsNoTracking()
            .Include(equipment => equipment.CurrentOperationalArea)
            .Where(equipment => equipment.CompanyId == companyId && equipment.Status != "Deleted")
            .OrderBy(equipment => equipment.Name)
            .ToListAsync();

        return equipmentItems
            .Select(equipment => new SelectListItem
            {
                Value = equipment.Id.ToString(),
                Text = equipment.Name + (equipment.SerialOrAssetId == null ? string.Empty : " / " + equipment.SerialOrAssetId) + " - " + (equipment.CurrentOperationalArea != null ? equipment.CurrentOperationalArea.Name : (equipment.CurrentLocationDetail ?? "Unallocated"))
            })
            .ToList();
    }

    private async Task<List<SelectListItem>> LoadStockOptionsAsync(int companyId)
    {
        var stockItems = await _db.StockItems
            .AsNoTracking()
            .Include(stock => stock.CurrentOperationalArea)
            .Where(stock => stock.CompanyId == companyId && stock.Status != "Deleted")
            .OrderBy(stock => stock.ItemName)
            .ToListAsync();

        return stockItems
            .Select(stock => new SelectListItem
            {
                Value = stock.Id.ToString(),
                Text = stock.ItemName + (stock.BatchNumber == null ? string.Empty : " / Batch " + stock.BatchNumber) + " / Qty " + stock.Quantity + " - " + (stock.CurrentOperationalArea != null ? stock.CurrentOperationalArea.Name : (stock.Location ?? "Unallocated"))
            })
            .ToList();
    }

    private async Task<List<SelectListItem>> LoadMedicationOptionsAsync(int companyId)
    {
        var medicationItems = await _db.MedicationItems
            .AsNoTracking()
            .Where(medication => medication.CompanyId == companyId && medication.Status != "Deleted")
            .OrderBy(medication => medication.Name)
            .Select(medication => new
            {
                medication.Id,
                medication.Name,
                medication.BatchNumber,
                medication.Quantity,
                medication.StorageLocation,
                CurrentOperationalAreaName = medication.CurrentOperationalArea == null ? null : medication.CurrentOperationalArea.Name
            })
            .ToListAsync();

        return medicationItems
            .Select(medication => new SelectListItem
            {
                Value = medication.Id.ToString(),
                Text = medication.Name + (medication.BatchNumber == null ? string.Empty : " / Batch " + medication.BatchNumber) + " / Qty " + (medication.Quantity.HasValue ? medication.Quantity.Value.ToString() : "n/a") + " - " + (medication.CurrentOperationalAreaName ?? medication.StorageLocation ?? "Unallocated")
            })
            .ToList();
    }

    private string BuildTaskReference(int destinationId)
    {
        return string.Join('|', AssetType, AssetId, destinationId, QuantityMoved?.ToString() ?? string.Empty);
    }

    private string BuildTaskInstruction(string assetLabel, OperationalArea destination)
    {
        var instruction = $"Move / reallocate {assetLabel} to {BuildDestinationLocation(destination.Name)}.";
        if (!string.IsNullOrWhiteSpace(MovementReason))
        {
            instruction += $" Reason: {MovementReason.Trim()}";
        }

        return instruction;
    }

    private string BuildDestinationLocation(string destinationName)
    {
        var detail = NormalizeOptional(LocationDetail);
        return string.IsNullOrWhiteSpace(detail) ? destinationName : $"{destinationName} - {detail}";
    }

    private static string? BuildExistingLocation(string? areaName, string? detail)
    {
        if (string.IsNullOrWhiteSpace(areaName))
        {
            return NormalizeOptional(detail);
        }

        return string.IsNullOrWhiteSpace(detail) ? areaName : $"{areaName} - {detail}";
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class MovementRecord
    {
        public string AssetLabel { get; set; } = string.Empty;
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public int? QuantityMoved { get; set; }
        public string? MovedByName { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
