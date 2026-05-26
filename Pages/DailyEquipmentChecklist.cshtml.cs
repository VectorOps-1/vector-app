using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class DailyEquipmentChecklistModel : PageModel
{
    [BindProperty] public string? AssetId { get; set; }
    [BindProperty] public string? SerialNumber { get; set; }
    [BindProperty] public string? EquipmentName { get; set; }
    [BindProperty] public string? AssignedLocation { get; set; }
    [BindProperty] public string? BatteryState { get; set; }
    [BindProperty] public string? EquipmentStatus { get; set; }
    [BindProperty] public string? ChecklistNotes { get; set; }
    public string? StatusMessage { get; private set; }

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(AssetId) && string.IsNullOrWhiteSpace(SerialNumber) && string.IsNullOrWhiteSpace(EquipmentName))
        {
            StatusMessage = "Enter an asset ID, serial number, or equipment name before saving.";
            return Page();
        }

        StatusMessage = "Equipment checklist ready to save. Database storage, signed-in profile linkage, and audit logging will be connected in the production data phase.";
        return Page();
    }
}
