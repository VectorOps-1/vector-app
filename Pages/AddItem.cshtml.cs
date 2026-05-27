using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

    private string NormalizedType => Type?.Trim().ToLowerInvariant() switch
    {
        "vehicle" => "vehicle",
        "stock" => "stock",
        "staff" => "staff",
        "medication" => "medication",
        _ => "equipment"
    };

    public void OnGet()
    {
        Type = NormalizedType;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Type = NormalizedType;

        if (string.IsNullOrWhiteSpace(PrimaryName))
        {
            StatusMessage = $"Enter the {PrimaryLabel.ToLowerInvariant()} before saving.";
            return Page();
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

        ActionSaved = true;
        StatusMessage = $"{ItemLabel} ready to save. This manual add action will later create a database record and audit entry, and can be assigned as a task with limited access.";
        return Page();
    }
}
