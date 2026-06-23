using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class AddItemModel : PageModel
{
    public const string CustomSubtypeValue = "__custom";

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;
    private readonly IFileStorageService _fileStorage;
    private readonly CustomDropdownOptionService _customDropdownOptions;
    private readonly VehicleStructureSetupService _vehicleStructure;

    public AddItemModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        LocationOptionService locationOptions,
        IFileStorageService fileStorage,
        CustomDropdownOptionService customDropdownOptions,
        VehicleStructureSetupService vehicleStructure)
    {
        _db = db;
        _currentUser = currentUser;
        _locationOptions = locationOptions;
        _fileStorage = fileStorage;
        _customDropdownOptions = customDropdownOptions;
        _vehicleStructure = vehicleStructure;
    }

    private static readonly HashSet<string> AllowedStaffFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tif", ".tiff",
        ".doc", ".docx", ".rtf", ".txt",
        ".xls", ".xlsx", ".csv"
    };

    [BindProperty(SupportsGet = true)] public string Type { get; set; } = "equipment";
    [BindProperty] public string? PrimaryName { get; set; }
    [BindProperty] public string? ReferenceNumber { get; set; }
    [BindProperty] public string? SerialOrBatch { get; set; }
    [BindProperty] public string? MakeModelType { get; set; }
    [BindProperty] public string? Schedule { get; set; }
    [BindProperty] public string? StaffEmail { get; set; }
    [BindProperty] public string? NationalId { get; set; }
    [BindProperty] public string? CellNumber { get; set; }
    [BindProperty] public string? StaffQualificationFunction { get; set; }
    [BindProperty] public string? StaffPractitionerNumber { get; set; }
    [BindProperty] public DateTime? StaffAnnualLicenseExpiryDate { get; set; }
    [BindProperty] public string? StaffCpdComplianceStatus { get; set; }
    [BindProperty] public DateTime? StaffCpdComplianceExpiryDate { get; set; }
    [BindProperty] public string? VehicleFunction { get; set; }
    [BindProperty] public string? VehicleSubtype { get; set; }
    [BindProperty] public string? CustomVehicleSubtype { get; set; }
    [BindProperty] public string? VinNumber { get; set; }
    [BindProperty] public string? ChassisNumber { get; set; }
    [BindProperty] public string? LicenseNumber { get; set; }
    [BindProperty] public DateTime? LicenseDiscExpiryDate { get; set; }
    [BindProperty] public DateTime? LastServiceDate { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string? Status { get; set; }
    [BindProperty] public int? Quantity { get; set; }
    [BindProperty] public DateTime? ExpiryOrReviewDate { get; set; }
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public string StaffFileCategory { get; set; } = "Personal Documents";
    [BindProperty] public string? StaffFileCategoryOther { get; set; }
    [BindProperty] public string? StaffFileNotes { get; set; }
    [BindProperty] public List<IFormFile> StaffFiles { get; set; } = new();

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public List<SelectListItem> LocationOptions { get; private set; } = new();
    public List<SelectListItem> VehicleFunctionOptions { get; private set; } = new();
    public List<VehicleSubtypeSetupOption> VehicleSubtypeOptions { get; private set; } = new();
    public List<SelectListItem> StaffFileCategoryOptions { get; private set; } = new();
    public bool IsStaffProfile => NormalizedType == "staff";
    public bool IsVehicleEntry => NormalizedType == "vehicle";

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
        "vehicle" => "Callsign",
        "staff" => "Staff member name",
        "medication" => "Medication name",
        "stock" => "Stock item name",
        _ => "Equipment name"
    };

    public string PrimaryPlaceholder => NormalizedType switch
    {
        "vehicle" => "Assigned callsign",
        "staff" => "Full name",
        "medication" => "Medication name",
        "stock" => "Stock item name",
        _ => "Equipment name"
    };

    public bool ShowMedicationSchedule => NormalizedType == "medication";

    public string PageSubtitle => NormalizedType switch
    {
        "staff" => "Add one staff profile without uploading a full staff register.",
        "vehicle" => "Add one vehicle record without uploading a full vehicle register.",
        _ => "Add one register record without uploading a full register."
    };

    public string ReferenceLabel => NormalizedType switch
    {
        "medication" => "Medication code / reference",
        "stock" => "Stock code / reference",
        "staff" => "Staff ID",
        "vehicle" => "Registration number",
        _ => "ID / reference number"
    };

    public string ReferencePlaceholder => NormalizedType switch
    {
        "vehicle" => "Registration number",
        "stock" => "Stock code or internal reference",
        "medication" => "Medication code or internal reference",
        "staff" => "Staff ID or employee number",
        _ => "Asset ID or internal reference"
    };

    public string TypeLabel => NormalizedType switch
    {
        "medication" => "Medication type / form",
        "stock" => "Stock type / size",
        "vehicle" => "Vehicle type",
        _ => "Make / model / type"
    };

    public string NotesPlaceholder => NormalizedType switch
    {
        "staff" => "Staff notes, access notes, onboarding note, or manager instruction",
        "vehicle" => "Vehicle notes, allocation note, service note, or manager instruction",
        _ => "Additional details, acquisition note, supplier, or manager instruction"
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

    public async Task<IActionResult> OnGetAsync()
    {
        ApplyRequestType();
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadOptionsAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ApplyRequestType();

        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadOptionsAsync(currentUser.CompanyId);

        if (string.IsNullOrWhiteSpace(PrimaryName))
        {
            StatusMessage = $"Enter the {PrimaryLabel.ToLowerInvariant()} before saving.";
            return Page();
        }

        if (Type == "staff")
        {
            return await SaveStaffAsync(currentUser);
        }

        if (Type is "vehicle" or "equipment")
        {
            if (Type == "vehicle")
            {
                return await SaveVehicleAsync(currentUser);
            }

            return await SaveEquipmentAsync(currentUser);
        }

        if (Type == "medication")
        {
            var now = DateTime.UtcNow;
            var area = await _locationOptions.FindOperationalAreaAsync(currentUser.CompanyId, Location);
            var selectedLocation = LocationOptionService.NormalizeSelectedLocation(Location);
            var medication = new MedicationItem
            {
                CompanyId = currentUser.CompanyId,
                CreatedByUserId = currentUser.Id,
                Name = PrimaryName.Trim(),
                MedicationCode = string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(),
                BatchNumber = string.IsNullOrWhiteSpace(SerialOrBatch) ? null : SerialOrBatch.Trim(),
                MedicationType = string.IsNullOrWhiteSpace(MakeModelType) ? null : MakeModelType.Trim(),
                Schedule = string.IsNullOrWhiteSpace(Schedule) ? null : Schedule.Trim(),
                StorageLocation = selectedLocation,
                CurrentOperationalAreaId = area?.Id,
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
            var now = DateTime.UtcNow;
            var area = await _locationOptions.FindOperationalAreaAsync(currentUser.CompanyId, Location);
            var selectedLocation = LocationOptionService.NormalizeSelectedLocation(Location);
            var stockItem = new StockItem
            {
                CompanyId = currentUser.CompanyId,
                CreatedByUserId = currentUser.Id,
                LastMovedByUserId = currentUser.Id,
                ItemName = PrimaryName.Trim(),
                ItemType = string.IsNullOrWhiteSpace(MakeModelType) ? null : MakeModelType.Trim(),
                BatchNumber = string.IsNullOrWhiteSpace(SerialOrBatch) ? null : SerialOrBatch.Trim(),
                Location = selectedLocation,
                CurrentOperationalAreaId = area?.Id,
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
        var area = await _locationOptions.FindOperationalAreaAsync(currentUser.CompanyId, Location);
        var vehicleSubtype = ResolveSubmittedVehicleSubtype();
        if (vehicleSubtype is null)
        {
            StatusMessage = "Select a configured subtype before saving the vehicle.";
            PrepareVehicleSubtypeSelection();
            return Page();
        }

        var function = NormalizeOptional(VehicleFunction);
        if (function is null)
        {
            StatusMessage = "Select a configured vehicle function before saving.";
            PrepareVehicleSubtypeSelection();
            return Page();
        }

        if (!VehicleSubtypeOptions.Any(option =>
                string.Equals(option.FunctionName, function, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(option.Name, vehicleSubtype, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "Select a subtype that belongs to the selected vehicle function.";
            PrepareVehicleSubtypeSelection();
            return Page();
        }

        var vehicleType = vehicleSubtype;
        var vehicle = new Vehicle
        {
            CompanyId = currentUser.CompanyId,
            RegistrationNumber = registration,
            Callsign = PrimaryName!.Trim(),
            VehicleType = vehicleType,
            VehicleFunction = function,
            VehicleSubtype = vehicleSubtype,
            VinNumber = NormalizeOptional(VinNumber),
            ChassisNumber = NormalizeOptional(ChassisNumber),
            LicenseNumber = NormalizeOptional(LicenseNumber),
            LicenseDiscExpiryDate = LicenseDiscExpiryDate,
            LastServiceDate = LastServiceDate,
            CurrentOperationalAreaId = area?.Id,
            CurrentLocationDetail = area is null ? LocationOptionService.NormalizeSelectedLocation(Location) : null,
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
        VehicleSubtype = vehicleSubtype;
        CustomVehicleSubtype = null;
        await LoadOptionsAsync(currentUser.CompanyId);
        PrepareVehicleSubtypeSelection();
        return Page();
    }

    private async Task<IActionResult> SaveEquipmentAsync(AppUser currentUser)
    {
        var now = DateTime.UtcNow;
        var area = await _locationOptions.FindOperationalAreaAsync(currentUser.CompanyId, Location);
        var equipment = new EquipmentItem
        {
            CompanyId = currentUser.CompanyId,
            Name = PrimaryName!.Trim(),
            EquipmentType = NormalizeOptional(MakeModelType),
            Model = NormalizeOptional(MakeModelType),
            SerialOrAssetId = NormalizeOptional(SerialOrBatch) ?? NormalizeOptional(ReferenceNumber),
            CurrentOperationalAreaId = area?.Id,
            CurrentLocationDetail = area is null ? LocationOptionService.NormalizeSelectedLocation(Location) : null,
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

    private async Task<IActionResult> SaveStaffAsync(AppUser currentUser)
    {
        if (string.IsNullOrWhiteSpace(StaffEmail))
        {
            StatusMessage = "Enter the staff member's email before saving the staff profile.";
            return Page();
        }

        var unsupportedFile = StaffFiles.FirstOrDefault(file =>
            file.Length > 0 && !AllowedStaffFileExtensions.Contains(Path.GetExtension(file.FileName)));
        if (unsupportedFile is not null)
        {
            StatusMessage = $"Unsupported staff file type: {unsupportedFile.FileName}.";
            return Page();
        }

        foreach (var file in StaffFiles.Where(file => file.Length > 0))
        {
            try
            {
                await _fileStorage.ValidateAsync(file, FileStorageValidationOptions.StaffDocument);
            }
            catch (FileStorageValidationException ex)
            {
                StatusMessage = ex.Message;
                return Page();
            }
        }

        var email = StaffEmail.Trim();
        var duplicateExists = await _db.AppUsers.AnyAsync(user =>
            user.CompanyId == currentUser.CompanyId &&
            user.Email == email);

        if (duplicateExists)
        {
            StatusMessage = "A staff profile with this email already exists.";
            return Page();
        }

        var staffRole = await _db.AppRoles.FirstOrDefaultAsync(role => role.Name == "Staff");
        if (staffRole is null)
        {
            StatusMessage = "Staff role is missing. Create the Staff role before adding staff profiles.";
            return Page();
        }

        var now = DateTime.UtcNow;
        var assignedArea = await _locationOptions.FindOperationalAreaAsync(currentUser.CompanyId, Location);
        var staffProfile = new AppUser
        {
            CompanyId = currentUser.CompanyId,
            AppRoleId = staffRole.Id,
            FullName = PrimaryName!.Trim(),
            Email = email,
            StaffIdentifier = NormalizeOptional(ReferenceNumber),
            NationalId = NormalizeOptional(NationalId),
            CellNumber = NormalizeOptional(CellNumber),
            QualificationFunction = NormalizeOptional(StaffQualificationFunction),
            PractitionerNumber = NormalizeOptional(StaffPractitionerNumber),
            AnnualLicenseExpiryDate = StaffAnnualLicenseExpiryDate,
            CpdComplianceStatus = NormalizeOptional(StaffCpdComplianceStatus),
            CpdComplianceExpiryDate = StaffCpdComplianceExpiryDate,
            AssignedOperationalAreaId = assignedArea?.Id,
            Status = string.IsNullOrWhiteSpace(Status) ? "Active" : Status.Trim(),
            CreatedAtUtc = now
        };

        _db.AppUsers.Add(staffProfile);
        await _db.SaveChangesAsync();

        var savedFileCount = 0;
        var hasFilesToSave = StaffFiles.Any(file => file.Length > 0);
        var fileCategory = "Personal Documents";

        if (hasFilesToSave)
        {
            var resolvedCategory = await _customDropdownOptions.ResolveSelectionAsync(
                currentUser.CompanyId,
                currentUser.Id,
                CustomDropdownOptionService.StaffFileCategoryKey,
                StaffFileCategory,
                StaffFileCategoryOther,
                "Personal Documents");

            if (resolvedCategory is null)
            {
                StatusMessage = "Name the Other folder / category before saving staff files.";
                return Page();
            }

            fileCategory = resolvedCategory;
        }

        foreach (var file in StaffFiles.Where(file => file.Length > 0))
        {
            var storedFile = await _fileStorage.SaveAsync(
                file,
                currentUser.CompanyId,
                $"staff-{staffProfile.Id}",
                FileStorageValidationOptions.StaffDocument);
            _db.AssetFiles.Add(new AssetFile
            {
                CompanyId = currentUser.CompanyId,
                UploadedByUserId = currentUser.Id,
                LinkedEntityType = "Staff",
                LinkedEntityId = staffProfile.Id,
                Category = fileCategory,
                OriginalFileName = storedFile.OriginalFileName,
                ContentType = storedFile.ContentType,
                StorageProvider = storedFile.ProviderName,
                StoragePath = storedFile.StoragePath,
                SizeBytes = storedFile.SizeBytes,
                Notes = NormalizeOptional(StaffFileNotes),
                UploadedAtUtc = now
            });

            savedFileCount++;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Staff profile added",
            EntityType = "AppUser",
            EntityId = staffProfile.Id,
            Details = $"Staff profile added: {staffProfile.FullName}.",
            CreatedAtUtc = now
        });

        if (savedFileCount > 0)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                CompanyId = currentUser.CompanyId,
                AppUserId = currentUser.Id,
                Action = "Staff files uploaded",
                EntityType = "AppUser",
                EntityId = staffProfile.Id,
                Details = $"{savedFileCount} staff file(s) uploaded into {fileCategory} while creating {staffProfile.FullName}.",
                CreatedAtUtc = now
            });
        }

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = savedFileCount == 0
            ? "Staff profile saved to the staff register."
            : $"Staff profile saved with {savedFileCount} linked file(s).";
        return Page();
    }

    private async Task LoadOptionsAsync(int companyId)
    {
        LocationOptions = Type is "vehicle" or "staff"
            ? await _locationOptions.GetOperationalAreaOptionsAsync(companyId)
            : await _locationOptions.GetAssetLocationOptionsAsync(companyId);

        if (Type == "vehicle")
        {
            await LoadVehicleSubtypeOptionsAsync(companyId);
            PrepareVehicleSubtypeSelection();
        }

        if (Type == "staff")
        {
            StaffFileCategoryOptions = await _customDropdownOptions.BuildOptionsAsync(
                companyId,
                CustomDropdownOptionService.StaffFileCategoryKey,
                CustomDropdownOptionService.StaffFileCategoryDefaults,
                StaffFileCategory);
        }
    }

    private async Task LoadVehicleSubtypeOptionsAsync(int companyId)
    {
        var snapshot = await _vehicleStructure.GetSnapshotAsync(companyId);
        VehicleFunctionOptions = snapshot.Functions
            .Select(option => new SelectListItem
            {
                Value = option.Name,
                Text = option.Name,
                Selected = string.Equals(VehicleFunction, option.Name, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();

        VehicleSubtypeOptions = snapshot.Subtypes.ToList();
    }

    private string? ResolveSubmittedVehicleSubtype()
    {
        return string.Equals(VehicleSubtype, CustomSubtypeValue, StringComparison.OrdinalIgnoreCase)
            ? NormalizeOptional(CustomVehicleSubtype)
            : NormalizeOptional(VehicleSubtype);
    }

    private void PrepareVehicleSubtypeSelection()
    {
        var subtype = NormalizeOptional(VehicleSubtype);
        if (subtype is null)
        {
            return;
        }

        if (string.Equals(subtype, CustomSubtypeValue, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (VehicleSubtypeOptions.Any(option => string.Equals(option.Name, subtype, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        CustomVehicleSubtype = subtype;
        VehicleSubtype = CustomSubtypeValue;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

}
