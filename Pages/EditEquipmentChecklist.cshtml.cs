using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class EditEquipmentChecklistModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/EditVehicleChecklist", new { checklist = "daily-vehicle", mode = "build" });
    }
}
