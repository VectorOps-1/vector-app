using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
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
    public string ViewMode { get; private set; } = "register";
    [BindProperty(SupportsGet = true)] public string GroupBy { get; set; } = "equipment-type";

    public List<EquipmentRegisterItem> EquipmentItems { get; private set; } = new();
    public List<EquipmentRegisterGroup> EquipmentGroups { get; private set; } = new();
    public bool IsRegisterView { get; private set; }
    public bool HasSearchTerm => !string.IsNullOrWhiteSpace(SearchTerm);
    public bool ShouldShowResults => IsRegisterView || HasSearchTerm;
    public string? StatusMessage { get; private set; }
    public string PageHeading => "Equipment Register";
    public string PageSubtitle => "Open the full grouped equipment register. Use the search field to narrow results, then expand a group and select an item to view full details.";
    public string CurrentReturnUrl => "/EquipmentRegister?view=register&GroupBy=" + Uri.EscapeDataString(NormalizeGroupBy(GroupBy)) + (HasSearchTerm ? "&SearchTerm=" + Uri.EscapeDataString(SearchTerm!.Trim()) : string.Empty);

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        ViewMode = NormalizeViewMode(Request.Query["view"].ToString());
        IsRegisterView = ViewMode == "register";
        GroupBy = NormalizeGroupBy(GroupBy);
        StatusMessage = TempData["SuccessMessage"] as string;

        if (!ShouldShowResults)
        {
            return Page();
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
            .OrderBy(item => item.EquipmentType)
            .ThenBy(item => item.Name)
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

        EquipmentGroups = EquipmentItems
            .GroupBy(item => BuildGroupLabel(item))
            .OrderBy(group => group.Key)
            .Select(group => new EquipmentRegisterGroup
            {
                Label = group.Key,
                Items = group
                    .OrderBy(item => item.Name)
                    .ThenBy(item => item.SerialOrAssetId)
                    .ToList()
            })
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int equipmentItemId, string? returnUrl)
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        var equipmentItem = await _db.EquipmentItems
            .FirstOrDefaultAsync(item =>
                item.Id == equipmentItemId &&
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted");

        if (equipmentItem is null)
        {
            TempData["SuccessMessage"] = "Equipment item was not found.";
            return RedirectBack(returnUrl);
        }

        var now = DateTime.UtcNow;
        equipmentItem.Status = "Deleted";
        equipmentItem.UpdatedAtUtc = now;

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Equipment item deleted",
            EntityType = "EquipmentItem",
            EntityId = equipmentItem.Id,
            Details = $"Equipment item deleted from register: {equipmentItem.Name}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Equipment item deleted.";
        return RedirectBack(returnUrl);
    }

    private IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/EquipmentRegister", new { view = "register" });
    }

    private static string NormalizeViewMode(string? viewMode)
    {
        return "register";
    }

    private static string NormalizeGroupBy(string? groupBy)
    {
        return groupBy switch
        {
            "equipment-name" => "equipment-name",
            "next-service" => "next-service",
            "location" => "location",
            "vehicle" => "vehicle",
            _ => "equipment-type"
        };
    }

    private string BuildGroupLabel(EquipmentRegisterItem item)
    {
        return GroupBy switch
        {
            "equipment-name" => string.IsNullOrWhiteSpace(item.Name) ? "Unnamed equipment" : item.Name,
            "next-service" => item.NextServiceDate.HasValue
                ? "Next service " + item.NextServiceDate.Value.ToString("yyyy-MM-dd")
                : "No next service date",
            "location" => string.IsNullOrWhiteSpace(item.CurrentLocation) ? "Unallocated location" : item.CurrentLocation,
            "vehicle" => string.IsNullOrWhiteSpace(item.AssignedVehicle) ? "No vehicle allocation" : item.AssignedVehicle,
            _ => string.IsNullOrWhiteSpace(item.EquipmentType) ? "Equipment type not set" : item.EquipmentType
        };
    }

    public class EquipmentRegisterGroup
    {
        public string Label { get; set; } = string.Empty;
        public List<EquipmentRegisterItem> Items { get; set; } = new();
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
