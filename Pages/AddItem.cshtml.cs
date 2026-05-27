using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AddItemModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public AddItemModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string Type { get; set; } = "equipment";
    [BindProperty] public string? PrimaryName { get; set; }
    [BindProperty] public string? ReferenceNumber { get; set; }
    [BindProperty] public string? SerialOrBatch { get; set; }
    [BindProperty] public string? MakeModelType { get; set; }
    [BindProperty] public string? Schedule { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string? Status { get; set; }
    [BindProperty] public int? Quantity { get; set; }
    [BindProperty] public DateTime? ExpiryOrReviewDate { get; set; }
    [BindProperty] public string? Notes { get; set; }

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    public string ItemLabel => NormalizedType switch
    {
        "vehicle" => "Vehicle",
        "stock" => "Stock Item",
        "staff" => "Staff Profile",
        "medication" => "Medication Item",
        _ => "Equipment Item"
    };

    public string PrimaryLabel => NormalizedType switch
    {
        "vehicle" => "Vehicle callsign / name",
        "staff" => "Staff member name",
        "medication" => "Medication name",
        "stock" => "Stock item name",
        _ => "Equipment name"
    };

    public string PrimaryPlaceholder => NormalizedType switch
    {
        "vehicle" => "Callsign or vehicle name",
        "staff" => "Full name",
        "medication" => "Medication name",
        "stock" => "Stock item name",
        _ => "Equipment name"
    };

    public bool ShowMedicationSchedule => NormalizedType == "medication";

    public string ReferenceLabel => NormalizedType switch
    {
        "medication" => "Medication code / reference",
        "stock" => "Stock code / reference",
        _ => "ID / reference number"
    };

    public string TypeLabel => NormalizedType switch
    {
        "medication" => "Medication type / form",
        "stock" => "Stock type / size",
        _ => "Make / model / type"
    };

    private string NormalizedType => NormalizeItemType(Type);

    private static string NormalizeItemType(string? type)
    {
        return type?.Trim().ToLowerInvariant() switch
        {
            "vehicle" => "vehicle",
            "stock" => "stock",
            "staff" => "staff",
            "medication" => "medication",
            _ => "equipment"
        };
    }

    private string RequestType => NormalizeItemType(Request.Query["type"].ToString());

    private bool HasRequestType => !string.IsNullOrWhiteSpace(Request.Query["type"].ToString());

    private void ApplyRequestType()
    {
        Type = HasRequestType ? RequestType : NormalizedType;
    }

    public void OnGet()
    {
        ApplyRequestType();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ApplyRequestType();

        if (string.IsNullOrWhiteSpace(PrimaryName))
        {
            StatusMessage = $"Enter the {PrimaryLabel.ToLowerInvariant()} before saving.";
            return Page();
        }

        if (Type is "vehicle" or "equipment")
        {
            var currentUser = await _currentUser.GetCurrentUserAsync();
            if (currentUser is null)
            {
                return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
            }

            if (Type == "vehicle")
            {
                return await SaveVehicleAsync(currentUser);
            }

            return await SaveEquipmentAsync(currentUser);
        }

        if (Type == "medication")
        {
            var currentUser = await _currentUser.GetCurrentUserAsync();
            if (currentUser is null)
            {
                return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
            }

            var now = DateTime.UtcNow;
            var medication = new MedicationItem
            {
                CompanyId = currentUser.CompanyId,
                CreatedByUserId = currentUser.Id,
                Name = PrimaryName.Trim(),
                MedicationCode = string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(),
                BatchNumber = string.IsNullOrWhiteSpace(SerialOrBatch) ? null : SerialOrBatch.Trim(),
                MedicationType = string.IsNullOrWhiteSpace(MakeModelType) ? null : MakeModelType.Trim(),
                Schedule = string.IsNullOrWhiteSpace(Schedule) ? null : Schedule.Trim(),
                StorageLocation = string.IsNullOrWhiteSpace(Location) ? null : Location.Trim(),
                Status = string.IsNullOrWhiteSpace(Status) ? "Active" : Status.Trim(),
                Quantity = Quantity,
                ExpiryDate = ExpiryOrReviewDate,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                CreatedAtUtc = now
            };

            _db.MedicationItems.Add(medication);
            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new AuditLog
            {
                CompanyId = currentUser.CompanyId,
                AppUserId = currentUser.Id,
                Action = "Medication added",
                EntityType = "MedicationItem",
                EntityId = medication.Id,
                Details = $"Medication item added: {medication.Name}.",
                CreatedAtUtc = now
            });

            await _db.SaveChangesAsync();

            ActionSaved = true;
            StatusMessage = $"{ItemLabel} saved to the medication register.";
            return Page();
        }

        if (Type == "stock")
        {
            var currentUser = await _currentUser.GetCurrentUserAsync();
            if (currentUser is null)
            {
                return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
            }

            var now = DateTime.UtcNow;
            var stockItem = new StockItem
            {
                CompanyId = currentUser.CompanyId,
                CreatedByUserId = currentUser.Id,
                LastMovedByUserId = currentUser.Id,
                ItemName = PrimaryName.Trim(),
                ItemType = string.IsNullOrWhiteSpace(MakeModelType) ? null : MakeModelType.Trim(),
                BatchNumber = string.IsNullOrWhiteSpace(SerialOrBatch) ? null : SerialOrBatch.Trim(),
                Location = string.IsNullOrWhiteSpace(Location) ? null : Location.Trim(),
                Status = string.IsNullOrWhiteSpace(Status) ? "Active" : Status.Trim(),
                Quantity = Quantity ?? 0,
                LastMovementType = "Manual register entry",
                LastMovementAtUtc = now,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                CreatedAtUtc = now
            };

            _db.StockItems.Add(stockItem);
            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new AuditLog
            {
                CompanyId = currentUser.CompanyId,
                AppUserId = currentUser.Id,
                Action = "Stock item added",
                EntityType = "StockItem",
                EntityId = stockItem.Id,
                Details = $"Stock item added: {stockItem.ItemName}.",
                CreatedAtUtc = now
            });

            await _db.SaveChangesAsync();

            ActionSaved = true;
            StatusMessage = $"{ItemLabel} saved to the stock register.";
            return Page();
        }

        ActionSaved = true;
        StatusMessage = $"{ItemLabel} ready to save. This manual add action will later create a database record and audit entry, and can be assigned as a task with limited access.";
        return Page();
    }

    private async Task<IActionResult> SaveVehicleAsync(AppUser currentUser)
    {
        if (string.IsNullOrWhiteSpace(ReferenceNumber))
        {
            StatusMessage = "Enter the registration number before saving the vehicle.";
            return Page();
        }

        var registration = ReferenceNumber.Trim();
        var duplicateExists = await _db.Vehicles.AnyAsync(vehicle =>
            vehicle.CompanyId == currentUser.CompanyId &&
            vehicle.RegistrationNumber == registration);

        if (duplicateExists)
        {
            StatusMessage = "A vehicle with this registration number already exists.";
            return Page();
        }

        var now = DateTime.UtcNow;
        var area = await FindAreaByNameAsync(currentUser.CompanyId, Location);
        var vehicle = new Vehicle
        {
            CompanyId = currentUser.CompanyId,
            RegistrationNumber = registration,
            Callsign = PrimaryName!.Trim(),
            VehicleType = string.IsNullOrWhiteSpace(MakeModelType) ? "Vehicle" : MakeModelType.Trim(),
            CurrentOperationalAreaId = area?.Id,
            CurrentLocationDetail = area is null ? NormalizeOptional(Location) : null,
            NextServiceDate = ExpiryOrReviewDate,
            Status = string.IsNullOrWhiteSpace(Status) ? "Active" : Status.Trim(),
            Notes = NormalizeOptional(Notes),
            CreatedAtUtc = now
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Vehicle added",
            EntityType = "Vehicle",
            EntityId = vehicle.Id,
            Details = $"Vehicle added: {vehicle.RegistrationNumber} / {vehicle.Callsign}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = $"{ItemLabel} saved to the vehicle register.";
        return Page();
    }

    private async Task<IActionResult> SaveEquipmentAsync(AppUser currentUser)
    {
        var now = DateTime.UtcNow;
        var area = await FindAreaByNameAsync(currentUser.CompanyId, Location);
        var equipment = new EquipmentItem
        {
            CompanyId = currentUser.CompanyId,
            Name = PrimaryName!.Trim(),
            EquipmentType = NormalizeOptional(MakeModelType),
            Model = NormalizeOptional(MakeModelType),
            SerialOrAssetId = NormalizeOptional(SerialOrBatch) ?? NormalizeOptional(ReferenceNumber),
            CurrentOperationalAreaId = area?.Id,
            CurrentLocationDetail = area is null ? NormalizeOptional(Location) : null,
            NextServiceDate = ExpiryOrReviewDate,
            BatteryRequired = false,
            Status = string.IsNullOrWhiteSpace(Status) ? "Active" : Status.Trim(),
            Notes = NormalizeOptional(Notes),
            CreatedAtUtc = now
        };

        _db.EquipmentItems.Add(equipment);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Equipment added",
            EntityType = "EquipmentItem",
            EntityId = equipment.Id,
            Details = $"Equipment added: {equipment.Name}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = $"{ItemLabel} saved to the equipment register.";
        return Page();
    }

    private async Task<OperationalArea?> FindAreaByNameAsync(int companyId, string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return null;
        }

        var name = location.Trim();
        return await _db.OperationalAreas
            .FirstOrDefaultAsync(area => area.CompanyId == companyId && area.Name == name && area.Status == "Active");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
