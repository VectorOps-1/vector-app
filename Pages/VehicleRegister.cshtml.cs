using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class VehicleRegisterModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public VehicleRegisterModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
    [BindProperty(SupportsGet = true)] public int? OperationalAreaId { get; set; }

    public List<SelectListItem> OperationalAreaOptions { get; private set; } = new();
    public List<VehicleRegisterItem> Vehicles { get; private set; } = new();
    public string? StatusMessage { get; private set; }
    public string CurrentReturnUrl => "/VehicleRegister" + BuildQueryString();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        StatusMessage = TempData["SuccessMessage"] as string;

        OperationalAreaOptions = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == currentUser.CompanyId && area.Status == "Active")
            .OrderBy(area => area.AreaType)
            .ThenBy(area => area.Name)
            .Select(area => new SelectListItem
            {
                Value = area.Id.ToString(),
                Text = area.Address == null ? $"{area.Name} ({area.AreaType})" : $"{area.Name} ({area.AreaType}) - {area.Address}"
            })
            .ToListAsync();

        var query = _db.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.CompanyId == currentUser.CompanyId && vehicle.Status != "Deleted");

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(vehicle =>
                vehicle.RegistrationNumber.Contains(search)
                || vehicle.Callsign.Contains(search)
                || vehicle.VehicleType.Contains(search)
                || (vehicle.VehicleFunction != null && vehicle.VehicleFunction.Contains(search))
                || (vehicle.VehicleSubtype != null && vehicle.VehicleSubtype.Contains(search))
                || (vehicle.QualificationLevel != null && vehicle.QualificationLevel.Contains(search))
                || (vehicle.SchematicType != null && vehicle.SchematicType.Contains(search))
                || (vehicle.VinNumber != null && vehicle.VinNumber.Contains(search))
                || (vehicle.ChassisNumber != null && vehicle.ChassisNumber.Contains(search))
                || (vehicle.LicenseNumber != null && vehicle.LicenseNumber.Contains(search))
                || (vehicle.LicenseDiscExpiryDate.HasValue && vehicle.LicenseDiscExpiryDate.Value.ToString("yyyy-MM-dd").Contains(search))
                || vehicle.Status.Contains(search)
                || (vehicle.CurrentLocationDetail != null && vehicle.CurrentLocationDetail.Contains(search))
                || (vehicle.CurrentOperationalArea != null && vehicle.CurrentOperationalArea.Name.Contains(search))
                || (vehicle.LastMovedByUser != null && vehicle.LastMovedByUser.FullName.Contains(search))
                || _db.DailyVehicleReadinessReports.Any(report =>
                    report.VehicleId == vehicle.Id
                    && report.ReadinessStatus.Contains(search))
                || (vehicle.Notes != null && vehicle.Notes.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(StatusFilter))
        {
            var status = StatusFilter.Trim();
            query = query.Where(vehicle => vehicle.Status == status);
        }

        if (OperationalAreaId.HasValue)
        {
            query = query.Where(vehicle => vehicle.CurrentOperationalAreaId == OperationalAreaId.Value);
        }

        Vehicles = await query
            .OrderBy(vehicle => vehicle.VehicleFunction == null || vehicle.VehicleFunction == "" ? "Unassigned" : vehicle.VehicleFunction)
            .ThenBy(vehicle => vehicle.VehicleSubtype == null || vehicle.VehicleSubtype == "" ? "Unassigned" : vehicle.VehicleSubtype)
            .ThenBy(vehicle => vehicle.RegistrationNumber)
            .ThenBy(vehicle => vehicle.Callsign)
            .Select(vehicle => new VehicleRegisterItem
            {
                Id = vehicle.Id,
                RegistrationNumber = vehicle.RegistrationNumber,
                Callsign = vehicle.Callsign,
                VehicleType = vehicle.VehicleType,
                VehicleFunction = vehicle.VehicleFunction,
                VehicleSubtype = vehicle.VehicleSubtype,
                QualificationLevel = vehicle.QualificationLevel,
                SchematicType = vehicle.SchematicType,
                VinNumber = vehicle.VinNumber,
                ChassisNumber = vehicle.ChassisNumber,
                LicenseNumber = vehicle.LicenseNumber,
                LicenseDiscExpiryDate = vehicle.LicenseDiscExpiryDate,
                LastServiceDate = vehicle.LastServiceDate,
                NextServiceDate = vehicle.NextServiceDate,
                Status = vehicle.Status,
                CurrentLocation = vehicle.CurrentOperationalArea == null
                    ? vehicle.CurrentLocationDetail
                    : vehicle.CurrentLocationDetail == null
                        ? vehicle.CurrentOperationalArea.Name
                        : vehicle.CurrentOperationalArea.Name + " - " + vehicle.CurrentLocationDetail,
                LastMovedAtUtc = vehicle.LastMovedAtUtc,
                LastMovedByName = vehicle.LastMovedByUser == null ? null : vehicle.LastMovedByUser.FullName,
                LastReadinessAtUtc = _db.DailyVehicleReadinessReports
                    .Where(report => report.VehicleId == vehicle.Id)
                    .OrderByDescending(report => report.InspectionDateUtc)
                    .Select(report => (DateTime?)report.InspectionDateUtc)
                    .FirstOrDefault(),
                LastReadinessStatus = _db.DailyVehicleReadinessReports
                    .Where(report => report.VehicleId == vehicle.Id)
                    .OrderByDescending(report => report.InspectionDateUtc)
                    .Select(report => report.ReadinessStatus)
                    .FirstOrDefault(),
                AssignedEquipmentCount = _db.VehicleEquipmentAssignments
                    .Count(assignment => assignment.VehicleId == vehicle.Id && assignment.Status != "Deleted"),
                Notes = vehicle.Notes
            })
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int vehicleId, string? returnUrl)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(item =>
                item.Id == vehicleId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (vehicle is null)
        {
            TempData["SuccessMessage"] = "Vehicle was not found.";
            return RedirectBack(returnUrl);
        }

        var now = DateTime.UtcNow;
        vehicle.Status = "Deleted";
        vehicle.UpdatedAtUtc = now;

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Vehicle deleted",
            EntityType = "Vehicle",
            EntityId = vehicle.Id,
            Details = $"Vehicle deleted from register: {vehicle.RegistrationNumber} / {vehicle.Callsign}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Vehicle deleted.";
        return RedirectBack(returnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/VehicleRegister");
    }

    private string BuildQueryString()
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            query.Add("SearchTerm=" + Uri.EscapeDataString(SearchTerm.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(StatusFilter))
        {
            query.Add("StatusFilter=" + Uri.EscapeDataString(StatusFilter.Trim()));
        }

        if (OperationalAreaId.HasValue)
        {
            query.Add("OperationalAreaId=" + OperationalAreaId.Value);
        }

        return query.Count == 0 ? string.Empty : "?" + string.Join("&", query);
    }

    public sealed class VehicleRegisterItem
    {
        public int Id { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string? VehicleFunction { get; set; }
        public string? VehicleSubtype { get; set; }
        public string? QualificationLevel { get; set; }
        public string? SchematicType { get; set; }
        public string? VinNumber { get; set; }
        public string? ChassisNumber { get; set; }
        public string? LicenseNumber { get; set; }
        public DateTime? LicenseDiscExpiryDate { get; set; }
        public DateTime? LastServiceDate { get; set; }
        public DateTime? NextServiceDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CurrentLocation { get; set; }
        public DateTime? LastMovedAtUtc { get; set; }
        public string? LastMovedByName { get; set; }
        public DateTime? LastReadinessAtUtc { get; set; }
        public string? LastReadinessStatus { get; set; }
        public int AssignedEquipmentCount { get; set; }
        public string? Notes { get; set; }
    }
}
