using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditVehicleModel : PageModel
{
    public const string CustomSubtypeValue = "__custom";

    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly LocationOptionService _locationOptions;
    private readonly VehicleStructureSetupService _vehicleStructure;

    public EditVehicleModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        LocationOptionService locationOptions,
        VehicleStructureSetupService vehicleStructure)
    {
        _db = db;
        _currentUser = currentUser;
        _locationOptions = locationOptions;
        _vehicleStructure = vehicleStructure;
    }

    [BindProperty] public int VehicleId { get; set; }
    [BindProperty] public string RegistrationNumber { get; set; } = string.Empty;
    [BindProperty] public string Callsign { get; set; } = string.Empty;
    [BindProperty] public string? VehicleFunction { get; set; }
    [BindProperty] public string? VehicleSubtype { get; set; }
    [BindProperty] public string? CustomVehicleSubtype { get; set; }
    [BindProperty] public string? QualificationLevel { get; set; }
    [BindProperty] public string? VinNumber { get; set; }
    [BindProperty] public string? ChassisNumber { get; set; }
    [BindProperty] public string? LicenseNumber { get; set; }
    [BindProperty] public DateTime? LicenseDiscExpiryDate { get; set; }
    [BindProperty] public DateTime? LastServiceDate { get; set; }
    [BindProperty] public DateTime? NextServiceDate { get; set; }
    [BindProperty] public string? Location { get; set; }
    [BindProperty] public string Status { get; set; } = "Active";
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public string? ReturnUrl { get; set; }

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public List<SelectListItem> LocationOptions { get; private set; } = new();
    public List<SelectListItem> VehicleFunctionOptions { get; private set; } = new();
    public List<VehicleSubtypeSetupOption> VehicleSubtypeOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int vehicleId, string? returnUrl)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var vehicle = await _db.Vehicles
            .AsNoTracking()
            .Include(item => item.CurrentOperationalArea)
            .FirstOrDefaultAsync(item =>
                item.Id == vehicleId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (vehicle is null)
        {
            return RedirectToPage("/VehicleRegister");
        }

        LoadFromVehicle(vehicle, returnUrl);
        await LoadOptionsAsync(currentUser.CompanyId);
        PrepareSubtypeSelection();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadOptionsAsync(currentUser.CompanyId);

        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(item =>
                item.Id == VehicleId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (vehicle is null)
        {
            StatusMessage = "Vehicle record not found.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(RegistrationNumber))
        {
            StatusMessage = "Enter the registration number before saving.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Callsign))
        {
            StatusMessage = "Enter the callsign before saving.";
            return Page();
        }

        var registration = RegistrationNumber.Trim();
        var duplicateRegistration = await _db.Vehicles.AnyAsync(item =>
            item.CompanyId == currentUser.CompanyId &&
            item.Id != vehicle.Id &&
            item.RegistrationNumber == registration &&
            item.Status != "Deleted");

        if (duplicateRegistration)
        {
            StatusMessage = "Another vehicle already uses this registration number.";
            return Page();
        }

        var now = DateTime.UtcNow;
        var previousSummary = $"{vehicle.RegistrationNumber} / {vehicle.Callsign} / {vehicle.VehicleFunction ?? "No function"} / {vehicle.VehicleSubtype ?? vehicle.VehicleType}";
        var area = await _locationOptions.FindOperationalAreaAsync(currentUser.CompanyId, Location);
        var subtype = ResolveSubmittedSubtype();
        if (subtype is null)
        {
            StatusMessage = "Select an existing subtype or create a custom subtype before saving.";
            PrepareSubtypeSelection();
            return Page();
        }

        var function = NormalizeOptional(VehicleFunction);
        if (function is null)
        {
            StatusMessage = "Select a configured vehicle function before saving.";
            PrepareSubtypeSelection();
            return Page();
        }

        if (!VehicleSubtypeOptions.Any(option =>
                string.Equals(option.FunctionName, function, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(option.Name, subtype, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = "Select a subtype that belongs to the selected vehicle function.";
            PrepareSubtypeSelection();
            return Page();
        }

        var vehicleType = subtype ?? vehicle.VehicleType;

        vehicle.RegistrationNumber = registration;
        vehicle.Callsign = Callsign.Trim();
        vehicle.VehicleFunction = function;
        vehicle.VehicleSubtype = subtype;
        vehicle.VehicleType = string.IsNullOrWhiteSpace(vehicleType) ? "Vehicle" : vehicleType.Trim();
        vehicle.QualificationLevel = NormalizeOptional(QualificationLevel);
        vehicle.VinNumber = NormalizeOptional(VinNumber);
        vehicle.ChassisNumber = NormalizeOptional(ChassisNumber);
        vehicle.LicenseNumber = NormalizeOptional(LicenseNumber);
        vehicle.LicenseDiscExpiryDate = LicenseDiscExpiryDate;
        vehicle.LastServiceDate = LastServiceDate;
        vehicle.NextServiceDate = NextServiceDate;
        vehicle.CurrentOperationalAreaId = area?.Id;
        vehicle.CurrentLocationDetail = area is null ? LocationOptionService.NormalizeSelectedLocation(Location) : null;
        vehicle.Status = NormalizeOptional(Status) ?? "Active";
        vehicle.Notes = NormalizeOptional(Notes);
        vehicle.UpdatedAtUtc = now;

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Vehicle updated",
            EntityType = "Vehicle",
            EntityId = vehicle.Id,
            Details = $"Vehicle register updated from [{previousSummary}] to [{vehicle.RegistrationNumber} / {vehicle.Callsign} / {vehicle.VehicleFunction ?? "No function"} / {vehicle.VehicleSubtype ?? vehicle.VehicleType}].",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = "Vehicle register updated.";
        VehicleSubtype = subtype;
        CustomVehicleSubtype = null;
        await LoadOptionsAsync(currentUser.CompanyId);
        PrepareSubtypeSelection();
        return Page();
    }

    private async Task LoadOptionsAsync(int companyId)
    {
        LocationOptions = await _locationOptions.GetOperationalAreaOptionsAsync(companyId);

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

    private void LoadFromVehicle(Vehicle vehicle, string? returnUrl)
    {
        VehicleId = vehicle.Id;
        RegistrationNumber = vehicle.RegistrationNumber;
        Callsign = vehicle.Callsign;
        VehicleFunction = NormalizeOptional(vehicle.VehicleFunction) ?? VehicleTaxonomyService.InferFunction(vehicle.VehicleType);
        VehicleSubtype = NormalizeOptional(vehicle.VehicleSubtype) ?? VehicleTaxonomyService.InferSubtype(vehicle.VehicleType);
        QualificationLevel = vehicle.QualificationLevel;
        VinNumber = vehicle.VinNumber;
        ChassisNumber = vehicle.ChassisNumber;
        LicenseNumber = vehicle.LicenseNumber;
        LicenseDiscExpiryDate = vehicle.LicenseDiscExpiryDate;
        LastServiceDate = vehicle.LastServiceDate;
        NextServiceDate = vehicle.NextServiceDate;
        Location = vehicle.CurrentOperationalArea?.Name ?? vehicle.CurrentLocationDetail;
        Status = vehicle.Status;
        Notes = vehicle.Notes;
        ReturnUrl = returnUrl;
    }

    private string? ResolveSubmittedSubtype()
    {
        return string.Equals(VehicleSubtype, CustomSubtypeValue, StringComparison.OrdinalIgnoreCase)
            ? NormalizeOptional(CustomVehicleSubtype)
            : NormalizeOptional(VehicleSubtype);
    }

    private void PrepareSubtypeSelection()
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
