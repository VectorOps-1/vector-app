using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ReassignVehicleCallsignModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ReassignVehicleCallsignModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public int? SourceVehicleId { get; set; }
    [BindProperty] public int TargetVehicleId { get; set; }
    [BindProperty] public string? CustomCallsign { get; set; }
    [BindProperty] public bool SwapPreviousCallsign { get; set; } = true;

    public List<SelectListItem> VehicleOptions { get; private set; } = new();
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadVehicleOptionsAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        await LoadVehicleOptionsAsync(currentUser.CompanyId);

        if (TargetVehicleId <= 0)
        {
            ModelState.AddModelError(nameof(TargetVehicleId), "Select the registration number that must receive the callsign.");
        }

        var customCallsign = NormalizeOptional(CustomCallsign);
        if (!SourceVehicleId.HasValue && customCallsign is null)
        {
            ModelState.AddModelError(string.Empty, "Select an existing callsign or enter a custom callsign.");
        }

        var target = await _db.Vehicles.FirstOrDefaultAsync(vehicle =>
            vehicle.Id == TargetVehicleId &&
            vehicle.CompanyId == currentUser.CompanyId &&
            vehicle.Status != "Deleted");

        if (target is null)
        {
            ModelState.AddModelError(nameof(TargetVehicleId), "Selected target vehicle was not found.");
        }

        Vehicle? source = null;
        if (SourceVehicleId.HasValue)
        {
            source = await _db.Vehicles.FirstOrDefaultAsync(vehicle =>
                vehicle.Id == SourceVehicleId.Value &&
                vehicle.CompanyId == currentUser.CompanyId &&
                vehicle.Status != "Deleted");

            if (source is null)
            {
                ModelState.AddModelError(nameof(SourceVehicleId), "Selected source vehicle was not found.");
            }
        }

        if (source is not null && target is not null && source.Id == target.Id && customCallsign is null)
        {
            ModelState.AddModelError(string.Empty, "Select a different target vehicle or enter a custom callsign.");
        }

        var reassignedCallsign = source is not null && (target is null || source.Id != target.Id)
            ? source.Callsign.Trim()
            : customCallsign;
        if (reassignedCallsign is not null)
        {
            var sourceId = source?.Id;
            var duplicateExists = await _db.Vehicles.AnyAsync(vehicle =>
                vehicle.CompanyId == currentUser.CompanyId &&
                vehicle.Status != "Deleted" &&
                vehicle.Id != TargetVehicleId &&
                (!sourceId.HasValue || vehicle.Id != sourceId.Value) &&
                vehicle.Callsign == reassignedCallsign);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(CustomCallsign), "That callsign is already assigned to another vehicle.");
            }
        }

        if (!ModelState.IsValid || target is null || reassignedCallsign is null)
        {
            return Page();
        }

        var now = DateTime.UtcNow;
        var targetPreviousCallsign = target.Callsign;

        target.Callsign = reassignedCallsign;
        target.UpdatedAtUtc = now;

        if (source is not null && source.Id != target.Id)
        {
            source.Callsign = SwapPreviousCallsign
                ? targetPreviousCallsign
                : $"Unassigned - {source.RegistrationNumber}";
            source.UpdatedAtUtc = now;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Vehicle callsign reassigned",
            EntityType = "Vehicle",
            EntityId = target.Id,
            Details = source is null
                ? $"{target.RegistrationNumber} callsign changed from {targetPreviousCallsign} to {target.Callsign}."
                : $"{source.RegistrationNumber} callsign {reassignedCallsign} reassigned to {target.RegistrationNumber}. Previous target callsign: {targetPreviousCallsign}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = $"{target.RegistrationNumber} now carries callsign {target.Callsign}.";
        await LoadVehicleOptionsAsync(currentUser.CompanyId);
        return Page();
    }

    private async Task LoadVehicleOptionsAsync(int companyId)
    {
        VehicleOptions = await _db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.CompanyId == companyId && vehicle.Status != "Deleted")
            .OrderBy(vehicle => vehicle.Callsign)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .Select(vehicle => new SelectListItem
            {
                Value = vehicle.Id.ToString(),
                Text = vehicle.Callsign + " / " + vehicle.RegistrationNumber + " / " + vehicle.VehicleType
            })
            .ToListAsync();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
