using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class OperationalAreasModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public OperationalAreasModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty] public string? Name { get; set; }
    [BindProperty] public string AreaType { get; set; } = "Base";
    [BindProperty] public string? Address { get; set; }
    [BindProperty] public string Status { get; set; } = "Active";
    [BindProperty] public string? Notes { get; set; }

    public List<OperationalAreaRecord> Areas { get; private set; } = new();
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        await LoadAreasAsync(currentUser.CompanyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.SeniorManagementAccess });
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError(nameof(Name), "Enter the base or area name.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAreasAsync(currentUser.CompanyId);
            return Page();
        }

        var trimmedName = Name!.Trim();
        var duplicateExists = await _db.OperationalAreas.AnyAsync(area =>
            area.CompanyId == currentUser.CompanyId &&
            area.Name == trimmedName);

        if (duplicateExists)
        {
            ModelState.AddModelError(nameof(Name), "That operational area already exists.");
            await LoadAreasAsync(currentUser.CompanyId);
            return Page();
        }

        var now = DateTime.UtcNow;
        var area = new OperationalArea
        {
            CompanyId = currentUser.CompanyId,
            Name = trimmedName,
            AreaType = string.IsNullOrWhiteSpace(AreaType) ? "Base" : AreaType.Trim(),
            Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
            Status = string.IsNullOrWhiteSpace(Status) ? "Active" : Status.Trim(),
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            CreatedAtUtc = now
        };

        _db.OperationalAreas.Add(area);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = "Operational area created",
            EntityType = "OperationalArea",
            EntityId = area.Id,
            Details = $"{area.AreaType} created: {area.Name}.",
            CreatedAtUtc = now
        });
        await _db.SaveChangesAsync();

        ActionSaved = true;
        StatusMessage = $"{area.Name} saved as an allocation destination.";
        Name = null;
        Address = null;
        Notes = null;
        AreaType = "Base";
        Status = "Active";

        await LoadAreasAsync(currentUser.CompanyId);
        return Page();
    }

    private async Task LoadAreasAsync(int companyId)
    {
        Areas = await _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == companyId)
            .OrderBy(area => area.AreaType)
            .ThenBy(area => area.Name)
            .Select(area => new OperationalAreaRecord
            {
                Id = area.Id,
                Name = area.Name,
                AreaType = area.AreaType,
                Address = area.Address,
                Status = area.Status,
                Notes = area.Notes
            })
            .ToListAsync();
    }

    public sealed class OperationalAreaRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AreaType { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
