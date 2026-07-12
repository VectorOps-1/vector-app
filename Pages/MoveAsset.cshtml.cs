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
    private readonly IUserActionAuthorizationService _authorization;

    public MoveAssetModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        IUserActionAuthorizationService authorization)
    {
        _db = db;
        _currentUser = currentUser;
        _authorization = authorization;
    }

    [BindProperty(SupportsGet = true)] public string AssetType { get; set; } = AssetTypes.Vehicle;
    [BindProperty(SupportsGet = true)] public int? TaskId { get; set; }
    [BindProperty(SupportsGet = true)] public bool TaskAccess { get; set; }
    [BindProperty(SupportsGet = true)] public int AssetId { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty] public string? DestinationKey { get; set; }
    [BindProperty] public int ToOperationalAreaId { get; set; }
    [BindProperty] public string? LocationDetail { get; set; }
    [BindProperty] public int? QuantityMoved { get; set; }
    [BindProperty] public string? MovementReason { get; set; }
    [BindProperty] public bool SendAsTask { get; set; }
    [BindProperty] public int AssignedToUserId { get; set; }
    [BindProperty] public DateTime? ExpiresAtLocal { get; set; }

    public List<SelectListItem> AssetOptions { get; private set; } = new();
    public List<SelectListItem> OperationalAreaOptions { get; private set; } = new();
    public List<SelectListItem> StorageLocationOptions { get; private set; } = new();
    public List<SelectListItem> DestinationOptions { get; private set; } = new();
    public List<SelectListItem> Recipients { get; private set; } = new();
    public List<MovementRecord> RecentMovements { get; private set; } = new();

    public string AssetDisplayName => AssetTypes.DisplayName(AssetType);
    public string PageTitle => $"Move / Reallocate {AssetDisplayName}";
    public bool IsQuantityAsset => NormalizedAssetType == AssetTypes.Stock || NormalizedAssetType == AssetTypes.Medication;
    public bool ActionSaved { get; private set; }
    public string? StatusMessage { get; private set; }
    public string SafeReturnUrl { get; private set; } = "/MoveAsset?asset=vehicle";

    private string NormalizedAssetType => AssetTypes.Normalize(AssetType);

    public async Task<IActionResult> OnGetAsync()
    {
        ApplyRequestedAssetType();
        SafeReturnUrl = NormalizeReturnUrl(ReturnUrl, AssetType);
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (TaskId.HasValue)
        {
            await ApplyTaskReferenceAsync(currentUser);
        }

        if (!await CanMoveRequestedAssetAsync(currentUser, allowAssignedTask: true))
        {
            StatusMessage = "You do not have permission to move or reallocate this asset.";
            await LoadPageDataAsync(currentUser.CompanyId);
            return Page();
        }

        await LoadPageDataAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string submitAction)
    {
        AssetType = NormalizedAssetType;
        SafeReturnUrl = NormalizeReturnUrl(ReturnUrl, AssetType);
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        if (!await CanMoveRequestedAssetAsync(currentUser, allowAssignedTask: true))
        {
            StatusMessage = "You do not have permission to move or reallocate this asset.";
            await LoadPageDataAsync(currentUser.CompanyId);
            return Page();
        }

        var destination = await ResolveDestinationAsync(currentUser.CompanyId);

        if (destination is null)
        {
            var destinationHint = NormalizedAssetType == AssetTypes.Vehicle
                ? "Select an active destination from Master Setup."
                : "Select an active destination from Master Setup or a registered vehicle.";
            ModelState.AddModelError(nameof(DestinationKey), destinationHint);
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

        var sendAsTask = SendAsTask && string.Equals(submitAction, "send-task", StringComparison.OrdinalIgnoreCase);
        if (sendAsTask && AssignedToUserId <= 0)
        {
            ModelState.AddModelError(nameof(AssignedToUserId), "Select the person who must complete this movement task.");
        }

        if (sendAsTask && !await _authorization.HasPermissionAsync(currentUser, UserActionPermissions.TasksSend))
        {
            ModelState.AddModelError(nameof(AssignedToUserId), "You do not have permission to send movement tasks.");
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
                    CompanyId = currentUser.CompanyId,
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
            DestinationKey = BuildAreaDestinationKey(destinationId);
        }
        else
        {
            DestinationKey = parts[2];
        }

        if (parts.Length > 3 && int.TryParse(parts[3], out var quantity))
        {
            QuantityMoved = quantity;
        }
    }

    private async Task<bool> CanMoveRequestedAssetAsync(AppUser currentUser, bool allowAssignedTask)
    {
        if (AssetId <= 0)
        {
            return await _authorization.HasPermissionAsync(currentUser, UserActionPermissions.AssetsMove);
        }

        if (allowAssignedTask &&
            await _authorization.CanCompleteMovementTaskAsync(currentUser, TaskId, AssetType, AssetId))
        {
            return true;
        }

        var operationalAreaId = await GetAssetOperationalAreaIdAsync(currentUser.CompanyId, AssetId, AssetType);
        return await _authorization.CanManageAreaScopedRecordAsync(
            currentUser,
            UserActionPermissions.AssetsMove,
            operationalAreaId);
    }

    private async Task<int?> GetAssetOperationalAreaIdAsync(int companyId, int assetId, string assetType)
    {
        return AssetTypes.Normalize(assetType) switch
        {
            AssetTypes.Equipment => await _db.EquipmentItems
                .AsNoTracking()
                .Where(item => item.Id == assetId && item.CompanyId == companyId && item.Status != "Deleted")
                .Select(item => item.CurrentOperationalAreaId)
                .FirstOrDefaultAsync(),
            AssetTypes.Stock => await _db.StockItems
                .AsNoTracking()
                .Where(item => item.Id == assetId && item.CompanyId == companyId && item.Status != "Deleted")
                .Select(item => item.CurrentOperationalAreaId)
                .FirstOrDefaultAsync(),
            AssetTypes.Medication => await _db.MedicationItems
                .AsNoTracking()
                .Where(item => item.Id == assetId && item.CompanyId == companyId && item.Status != "Deleted")
                .Select(item => item.CurrentOperationalAreaId)
                .FirstOrDefaultAsync(),
            _ => await _db.Vehicles
                .AsNoTracking()
                .Where(item => item.Id == assetId && item.CompanyId == companyId && item.Status != "Deleted")
                .Select(item => item.CurrentOperationalAreaId)
                .FirstOrDefaultAsync()
        };
    }

    private async Task CreateMovementTaskAsync(AppUser currentUser, MovementDestination destination, string assetLabel)
    {
        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            CompanyId = currentUser.CompanyId,
            AssignedToUserId = AssignedToUserId,
            AssignedByUserId = currentUser.Id,
            ActionType = AssetTypes.TaskAction(AssetType),
            RelatedItemReference = BuildTaskReference(destination),
            InstructionMessage = BuildTaskInstruction(assetLabel, destination),
            Status = "Open",
            CreatedAtUtc = now,
            ExpiresAtUtc = ExpiresAtLocal.HasValue ? DateTime.SpecifyKind(ExpiresAtLocal.Value, DateTimeKind.Local).ToUniversalTime() : null
        };

        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        _db.TaskEvents.Add(new TaskEvent
        {
            CompanyId = currentUser.CompanyId,
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
            Details = $"{task.ActionType}: {assetLabel} to {destination.LocationText}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
    }

    private async Task<AssetMovement?> ApplyMovementAsync(AppUser currentUser, MovementDestination destination)
    {
        return AssetType switch
        {
            AssetTypes.Equipment => await MoveEquipmentAsync(currentUser, destination),
            AssetTypes.Stock => await MoveStockAsync(currentUser, destination),
            AssetTypes.Medication => await MoveMedicationAsync(currentUser, destination),
            _ => await MoveVehicleAsync(currentUser, destination)
        };
    }

    private async Task<AssetMovement?> MoveVehicleAsync(AppUser currentUser, MovementDestination destination)
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
        var toText = BuildDestinationLocation(destination);
        var label = $"{vehicle.RegistrationNumber} / {vehicle.Callsign}";

        vehicle.CurrentOperationalAreaId = destination.OperationalAreaId;
        vehicle.CurrentLocationDetail = NormalizeOptional(LocationDetail);
        vehicle.LastMovedByUserId = currentUser.Id;
        vehicle.LastMovedAtUtc = DateTime.UtcNow;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        return await AddMovementAsync(currentUser, destination.OperationalAreaId, fromAreaId, fromText, toText, label);
    }

    private async Task<AssetMovement?> MoveEquipmentAsync(AppUser currentUser, MovementDestination destination)
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
        var toText = BuildDestinationLocation(destination);
        var label = string.IsNullOrWhiteSpace(equipment.SerialOrAssetId)
            ? equipment.Name
            : $"{equipment.Name} / {equipment.SerialOrAssetId}";

        equipment.CurrentOperationalAreaId = destination.OperationalAreaId;
        equipment.CurrentLocationDetail = BuildDestinationDetail(destination);
        equipment.LastMovedByUserId = currentUser.Id;
        equipment.LastMovedAtUtc = DateTime.UtcNow;
        equipment.UpdatedAtUtc = DateTime.UtcNow;

        return await AddMovementAsync(currentUser, destination.OperationalAreaId, fromAreaId, fromText, toText, label);
    }

    private async Task<AssetMovement?> MoveStockAsync(AppUser currentUser, MovementDestination destination)
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
        var toText = BuildDestinationLocation(destination);
        var label = string.IsNullOrWhiteSpace(stock.BatchNumber)
            ? stock.ItemName
            : $"{stock.ItemName} / Batch {stock.BatchNumber}";

        stock.CurrentOperationalAreaId = destination.OperationalAreaId;
        stock.Location = toText;
        stock.LastMovedByUserId = currentUser.Id;
        stock.LastMovementType = "Move / Reallocate";
        stock.LastMovementAtUtc = DateTime.UtcNow;
        stock.UpdatedAtUtc = DateTime.UtcNow;

        return await AddMovementAsync(currentUser, destination.OperationalAreaId, fromAreaId, fromText, toText, label);
    }

    private async Task<AssetMovement?> MoveMedicationAsync(AppUser currentUser, MovementDestination destination)
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
        var toText = BuildDestinationLocation(destination);
        var label = string.IsNullOrWhiteSpace(medication.BatchNumber)
            ? medication.Name
            : $"{medication.Name} / Batch {medication.BatchNumber}";

        medication.CurrentOperationalAreaId = destination.OperationalAreaId;
        medication.StorageLocation = toText;
        medication.LastAllocatedByUserId = currentUser.Id;
        medication.LastAllocationLocation = toText;
        medication.LastAllocatedAtUtc = DateTime.UtcNow;
        medication.UpdatedAtUtc = DateTime.UtcNow;

        return await AddMovementAsync(currentUser, destination.OperationalAreaId, fromAreaId, fromText, toText, label);
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

    private async Task<MovementDestination?> ResolveDestinationAsync(int companyId)
    {
        var destinationKey = NormalizeDestinationKey(DestinationKey, ToOperationalAreaId);
        DestinationKey = destinationKey;

        if (string.IsNullOrWhiteSpace(destinationKey))
        {
            return null;
        }

        var parts = destinationKey.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var destinationId))
        {
            return null;
        }

        if (string.Equals(parts[0], "area", StringComparison.OrdinalIgnoreCase))
        {
            var area = await _db.OperationalAreas
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == destinationId &&
                    item.CompanyId == companyId &&
                    item.Status == "Active");

            if (area is null)
            {
                return null;
            }

            ToOperationalAreaId = area.Id;
            return new MovementDestination(
                BuildAreaDestinationKey(area.Id),
                "Area",
                area.Id,
                null,
                area.Name,
                area.Name,
                area.Name);
        }

        if (string.Equals(parts[0], "storage", StringComparison.OrdinalIgnoreCase))
        {
            var storage = await _db.StorageLocations
                .AsNoTracking()
                .Include(item => item.OperationalArea)
                .FirstOrDefaultAsync(item =>
                    item.Id == destinationId &&
                    item.CompanyId == companyId &&
                    item.Status == "Active" &&
                    item.OperationalArea != null &&
                    item.OperationalArea.Status == "Active");

            if (storage?.OperationalArea is null)
            {
                return null;
            }

            ToOperationalAreaId = storage.OperationalAreaId;
            return new MovementDestination(
                BuildStorageDestinationKey(storage.Id),
                "Storage",
                storage.OperationalAreaId,
                null,
                storage.Name,
                storage.OperationalArea.Name,
                BuildExistingLocation(storage.OperationalArea.Name, storage.Name) ?? storage.Name);
        }

        if (!string.Equals(parts[0], "vehicle", StringComparison.OrdinalIgnoreCase) ||
            NormalizedAssetType == AssetTypes.Vehicle)
        {
            return null;
        }

        var vehicle = await _db.Vehicles
            .AsNoTracking()
            .Include(item => item.CurrentOperationalArea)
            .FirstOrDefaultAsync(item =>
                item.Id == destinationId &&
                item.CompanyId == companyId &&
                item.Status != "Deleted" &&
                item.CurrentOperationalAreaId.HasValue &&
                item.CurrentOperationalArea != null &&
                item.CurrentOperationalArea.Status == "Active");

        if (vehicle is null || !vehicle.CurrentOperationalAreaId.HasValue)
        {
            return null;
        }

        var areaName = vehicle.CurrentOperationalArea?.Name ?? "Unallocated";
        var vehicleLabel = BuildVehicleLabel(vehicle);
        ToOperationalAreaId = vehicle.CurrentOperationalAreaId.Value;

        return new MovementDestination(
            BuildVehicleDestinationKey(vehicle.Id),
            "Vehicle",
            vehicle.CurrentOperationalAreaId.Value,
            vehicle.Id,
            vehicleLabel,
            areaName,
            BuildExistingLocation(areaName, vehicleLabel) ?? vehicleLabel);
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

        StorageLocationOptions = await _db.StorageLocations
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
            .Select(location => new SelectListItem
            {
                Value = location.Id.ToString(),
                Text = $"{location.OperationalArea!.Name} - {location.Name} ({location.StorageType})"
            })
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(DestinationKey) && ToOperationalAreaId > 0)
        {
            DestinationKey = BuildAreaDestinationKey(ToOperationalAreaId);
        }

        DestinationOptions = BuildDestinationOptions();

        if (NormalizedAssetType is AssetTypes.Equipment or AssetTypes.Stock or AssetTypes.Medication)
        {
            DestinationOptions.AddRange(await LoadVehicleDestinationOptionsAsync(companyId));
        }

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

    private List<SelectListItem> BuildDestinationOptions()
    {
        var areaGroup = new SelectListGroup { Name = "Bases / operational areas" };
        var storageGroup = new SelectListGroup { Name = "Storage spaces" };
        var options = OperationalAreaOptions
            .Select(area => new SelectListItem
            {
                Value = BuildAreaDestinationKey(int.Parse(area.Value)),
                Text = area.Text,
                Group = areaGroup
            })
            .ToList();

        options.AddRange(StorageLocationOptions.Select(storage => new SelectListItem
        {
            Value = BuildStorageDestinationKey(int.Parse(storage.Value)),
            Text = storage.Text,
            Group = storageGroup
        }));

        return options;
    }

    private async Task<List<SelectListItem>> LoadVehicleDestinationOptionsAsync(int companyId)
    {
        var vehicleGroup = new SelectListGroup { Name = "Vehicles" };
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.CurrentOperationalArea)
            .Where(vehicle =>
                vehicle.CompanyId == companyId &&
                vehicle.Status != "Deleted" &&
                vehicle.CurrentOperationalAreaId.HasValue &&
                vehicle.CurrentOperationalArea != null &&
                vehicle.CurrentOperationalArea.Status == "Active")
            .OrderBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .ToListAsync();

        return vehicles
            .Select(vehicle => new SelectListItem
            {
                Value = BuildVehicleDestinationKey(vehicle.Id),
                Text = $"{BuildVehicleLabel(vehicle)} - {vehicle.CurrentOperationalArea?.Name ?? "Unallocated"}",
                Group = vehicleGroup
            })
            .ToList();
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

    private string BuildTaskReference(MovementDestination destination)
    {
        return string.Join('|', AssetType, AssetId, destination.Key, QuantityMoved?.ToString() ?? string.Empty);
    }

    private string BuildTaskInstruction(string assetLabel, MovementDestination destination)
    {
        var instruction = $"Move / reallocate {assetLabel} to {BuildDestinationLocation(destination)}.";
        if (!string.IsNullOrWhiteSpace(MovementReason))
        {
            instruction += $" Reason: {MovementReason.Trim()}";
        }

        return instruction;
    }

    private string BuildDestinationLocation(MovementDestination destination)
    {
        var detail = BuildDestinationDetail(destination);
        return string.IsNullOrWhiteSpace(detail) ? destination.LocationText : BuildExistingLocation(destination.AreaName, detail) ?? destination.LocationText;
    }

    private string? BuildDestinationDetail(MovementDestination destination)
    {
        var detail = NormalizeOptional(LocationDetail);
        if (destination.VehicleId.HasValue)
        {
            return string.IsNullOrWhiteSpace(detail) ? destination.Name : $"{destination.Name} - {detail}";
        }

        return detail;
    }

    private static string? BuildExistingLocation(string? areaName, string? detail)
    {
        if (string.IsNullOrWhiteSpace(areaName))
        {
            return NormalizeOptional(detail);
        }

        return string.IsNullOrWhiteSpace(detail) ? areaName : $"{areaName} - {detail}";
    }

    private static string BuildVehicleLabel(Vehicle vehicle)
    {
        return string.IsNullOrWhiteSpace(vehicle.Callsign)
            ? vehicle.RegistrationNumber
            : $"{vehicle.Callsign} / {vehicle.RegistrationNumber}";
    }

    private static string BuildAreaDestinationKey(int areaId)
    {
        return $"area:{areaId}";
    }

    private static string BuildVehicleDestinationKey(int vehicleId)
    {
        return $"vehicle:{vehicleId}";
    }

    private static string BuildStorageDestinationKey(int storageLocationId)
    {
        return $"storage:{storageLocationId}";
    }

    private static string? NormalizeDestinationKey(string? destinationKey, int areaId)
    {
        if (!string.IsNullOrWhiteSpace(destinationKey))
        {
            var trimmed = destinationKey.Trim();
            if (trimmed.StartsWith("area:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("vehicle:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("storage:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            if (int.TryParse(trimmed, out var parsedAreaId))
            {
                return BuildAreaDestinationKey(parsedAreaId);
            }

            return trimmed;
        }

        return areaId > 0 ? BuildAreaDestinationKey(areaId) : null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeReturnUrl(string? returnUrl, string assetType)
    {
        var fallback = $"/MoveAsset?asset={AssetTypes.Normalize(assetType)}";
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return fallback;
        }

        var trimmed = returnUrl.Trim();
        return trimmed.StartsWith('/') && !trimmed.StartsWith("//", StringComparison.Ordinal)
            ? trimmed
            : fallback;
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

    private sealed record MovementDestination(
        string Key,
        string Type,
        int OperationalAreaId,
        int? VehicleId,
        string Name,
        string AreaName,
        string LocationText);
}
