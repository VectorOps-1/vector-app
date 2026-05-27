using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EquipmentRegisterModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public EquipmentRegisterModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }

    public List<EquipmentRegisterItem> EquipmentItems { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var query = _db.EquipmentItems
            .AsNoTracking()
            .Where(item => item.CompanyId == currentUser.CompanyId && item.Status != "Deleted");

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var search = SearchTerm.Trim();
            query = query.Where(item =>
                item.Name.Contains(search)
                || (item.EquipmentType != null && item.EquipmentType.Contains(search))
                || (item.Model != null && item.Model.Contains(search))
                || (item.SerialOrAssetId != null && item.SerialOrAssetId.Contains(search))
                || item.Status.Contains(search)
                || (item.CurrentLocationDetail != null && item.CurrentLocationDetail.Contains(search))
                || (item.CurrentOperationalArea != null && item.CurrentOperationalArea.Name.Contains(search))
                || (item.LastMovedByUser != null && item.LastMovedByUser.FullName.Contains(search))
                || item.VehicleAssignments.Any(assignment =>
                    assignment.Status != "Deleted"
                    && ((assignment.Vehicle != null && assignment.Vehicle.Callsign.Contains(search))
                        || (assignment.Vehicle != null && assignment.Vehicle.RegistrationNumber.Contains(search))
                        || assignment.ExpectedEquipmentName.Contains(search)
                        || (assignment.ExpectedEquipmentType != null && assignment.ExpectedEquipmentType.Contains(search))
                        || (assignment.ExpectedModel != null && assignment.ExpectedModel.Contains(search)))));
        }

        EquipmentItems = await query
            .OrderBy(item => item.Name)
            .ThenBy(item => item.SerialOrAssetId)
            .Select(item => new EquipmentRegisterItem
            {
                Id = item.Id,
                Name = item.Name,
                EquipmentType = item.EquipmentType,
                Model = item.Model,
                SerialOrAssetId = item.SerialOrAssetId,
                NextServiceDate = item.NextServiceDate,
                BatteryRequired = item.BatteryRequired,
                Status = item.Status,
                CurrentLocation = item.CurrentOperationalArea == null
                    ? item.CurrentLocationDetail
                    : item.CurrentLocationDetail == null
                        ? item.CurrentOperationalArea.Name
                        : item.CurrentOperationalArea.Name + " - " + item.CurrentLocationDetail,
                LastMovedAtUtc = item.LastMovedAtUtc,
                LastMovedByName = item.LastMovedByUser == null ? null : item.LastMovedByUser.FullName,
                AssignedVehicle = item.VehicleAssignments
                    .Where(assignment => assignment.Status != "Deleted" && assignment.Vehicle != null)
                    .OrderBy(assignment => assignment.SortOrder)
                    .Select(assignment => assignment.Vehicle!.Callsign + " / " + assignment.Vehicle.RegistrationNumber)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Page();
    }

    public class EquipmentRegisterItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? EquipmentType { get; set; }
        public string? Model { get; set; }
        public string? SerialOrAssetId { get; set; }
        public DateTime? NextServiceDate { get; set; }
        public bool BatteryRequired { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CurrentLocation { get; set; }
        public DateTime? LastMovedAtUtc { get; set; }
        public string? LastMovedByName { get; set; }
        public string? AssignedVehicle { get; set; }
    }
}
