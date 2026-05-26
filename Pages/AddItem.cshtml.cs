using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class AddItemModel : PageModel
{
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

    public IActionResult OnPost()
    {
        Type = NormalizedType;

        if (string.IsNullOrWhiteSpace(PrimaryName))
        {
            StatusMessage = $"Enter the {PrimaryLabel.ToLowerInvariant()} before saving.";
            return Page();
        }

        ActionSaved = true;
        StatusMessage = $"{ItemLabel} ready to save. This manual add action will later create a database record and audit entry, and can be assigned as a task with limited access.";
        return Page();
    }
}
