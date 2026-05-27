using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
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

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

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
                || (vehicle.QualificationLevel != null && vehicle.QualificationLevel.Contains(search))
                || (vehicle.SchematicType != null && vehicle.SchematicType.Contains(search))
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
            .OrderBy(vehicle => vehicle.RegistrationNumber)
            .ThenBy(vehicle => vehicle.Callsign)
            .Select(vehicle => new VehicleRegisterItem
            {
                Id = vehicle.Id,
                RegistrationNumber = vehicle.RegistrationNumber,
                Callsign = vehicle.Callsign,
                VehicleType = vehicle.VehicleType,
                QualificationLevel = vehicle.QualificationLevel,
                SchematicType = vehicle.SchematicType,
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
                    .Count(assignment => assignment.VehicleId == vehicle.Id && assignment.Status != "Deleted")
            })
            .ToListAsync();

        return Page();
    }

    public sealed class VehicleRegisterItem
    {
        public int Id { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string? QualificationLevel { get; set; }
        public string? SchematicType { get; set; }
        public DateTime? NextServiceDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CurrentLocation { get; set; }
        public DateTime? LastMovedAtUtc { get; set; }
        public string? LastMovedByName { get; set; }
        public DateTime? LastReadinessAtUtc { get; set; }
        public string? LastReadinessStatus { get; set; }
        public int AssignedEquipmentCount { get; set; }
    }
}
