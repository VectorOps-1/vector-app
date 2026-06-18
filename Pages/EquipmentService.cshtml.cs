using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EquipmentServiceModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public EquipmentServiceModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public int? EquipmentId { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    [BindProperty] public List<int> SelectedEquipmentItemIds { get; set; } = new();
    [BindProperty] public DateTime? NewNextServiceDate { get; set; }
    [BindProperty] public string? ServiceNote { get; set; }
    [BindProperty] public string? PostedReturnUrl { get; set; }

    public List<ServiceEquipmentRow> EquipmentItems { get; private set; } = new();
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }
    public string SafeReturnUrl { get; private set; } = "/EquipmentRegister?view=register";

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        SafeReturnUrl = NormalizeReturnUrl(ReturnUrl);
        if (EquipmentId.HasValue)
        {
            SelectedEquipmentItemIds = new List<int> { EquipmentId.Value };
        }

        await LoadEquipmentItemsAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        SafeReturnUrl = NormalizeReturnUrl(PostedReturnUrl ?? ReturnUrl);

        SelectedEquipmentItemIds = SelectedEquipmentItemIds
            .Distinct()
            .Where(id => id > 0)
            .ToList();

        if (SelectedEquipmentItemIds.Count == 0)
        {
            StatusMessage = "Select at least one equipment item to update.";
            await LoadEquipmentItemsAsync(currentUser.CompanyId);
            return Page();
        }

        if (NewNextServiceDate is null)
        {
            StatusMessage = "Enter the new next service date.";
            await LoadEquipmentItemsAsync(currentUser.CompanyId);
            return Page();
        }

        var equipmentItems = await _db.EquipmentItems
            .Where(item =>
                item.CompanyId == currentUser.CompanyId &&
                item.Status != "Deleted" &&
                SelectedEquipmentItemIds.Contains(item.Id))
            .OrderBy(item => item.Name)
            .ThenBy(item => item.SerialOrAssetId)
            .ToListAsync();

        if (equipmentItems.Count == 0)
        {
            StatusMessage = "No matching equipment items were found.";
            await LoadEquipmentItemsAsync(currentUser.CompanyId);
            return Page();
        }

        var now = DateTime.UtcNow;
        var note = NormalizeOptional(ServiceNote);
        foreach (var equipment in equipmentItems)
        {
            var previousDate = equipment.NextServiceDate;
            equipment.NextServiceDate = NewNextServiceDate;
            equipment.UpdatedAtUtc = now;

            _db.AuditLogs.Add(new AuditLog
            {
                CompanyId = currentUser.CompanyId,
                AppUserId = currentUser.Id,
                Action = "Equipment service date updated",
                EntityType = "EquipmentItem",
                EntityId = equipment.Id,
                Details = $"{equipment.Name} ({equipment.SerialOrAssetId ?? "no S/N / ID"}) next service date changed from {FormatDate(previousDate)} to {FormatDate(NewNextServiceDate)}.{(note is null ? string.Empty : " Note: " + note)}",
                CreatedAtUtc = now
            });
        }

        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = equipmentItems.Count == 1
            ? "Equipment service date updated."
            : $"{equipmentItems.Count} equipment service dates updated.";

        SearchTerm = null;
        EquipmentId = null;
        SelectedEquipmentItemIds = new List<int>();
        NewNextServiceDate = null;
        ServiceNote = null;
        await LoadEquipmentItemsAsync(currentUser.CompanyId);
        return Page();
    }

    private async Task LoadEquipmentItemsAsync(int companyId)
    {
        var selectedIds = SelectedEquipmentItemIds.ToHashSet();
        var query = _db.EquipmentItems
            .AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.Status != "Deleted");

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
                || item.VehicleAssignments.Any(assignment =>
                    assignment.Status != "Deleted"
                    && assignment.Vehicle != null
                    && (assignment.Vehicle.Callsign.Contains(search)
                        || assignment.Vehicle.RegistrationNumber.Contains(search))));
        }

        EquipmentItems = await query
            .OrderBy(item => item.Name)
            .ThenBy(item => item.SerialOrAssetId)
            .Select(item => new ServiceEquipmentRow
            {
                Id = item.Id,
                Name = item.Name,
                EquipmentType = item.EquipmentType,
                Model = item.Model,
                SerialOrAssetId = item.SerialOrAssetId,
                NextServiceDate = item.NextServiceDate,
                Status = item.Status,
                CurrentLocation = item.CurrentOperationalArea == null
                    ? item.CurrentLocationDetail
                    : item.CurrentLocationDetail == null
                        ? item.CurrentOperationalArea.Name
                        : item.CurrentOperationalArea.Name + " - " + item.CurrentLocationDetail,
                AssignedVehicle = item.VehicleAssignments
                    .Where(assignment => assignment.Status != "Deleted" && assignment.Vehicle != null)
                    .OrderBy(assignment => assignment.SortOrder)
                    .Select(assignment => assignment.Vehicle!.Callsign + " / " + assignment.Vehicle.RegistrationNumber)
                    .FirstOrDefault(),
                IsSelected = selectedIds.Contains(item.Id)
            })
            .ToListAsync();
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/EquipmentRegister?view=register";
        }

        var trimmed = returnUrl.Trim();
        return trimmed.StartsWith('/') && !trimmed.StartsWith("//", StringComparison.Ordinal)
            ? trimmed
            : "/EquipmentRegister?view=register";
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string FormatDate(DateTime? value)
    {
        return value?.ToString("yyyy-MM-dd") ?? "not set";
    }

    public class ServiceEquipmentRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? EquipmentType { get; set; }
        public string? Model { get; set; }
        public string? SerialOrAssetId { get; set; }
        public DateTime? NextServiceDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CurrentLocation { get; set; }
        public string? AssignedVehicle { get; set; }
        public bool IsSelected { get; set; }
    }
}
