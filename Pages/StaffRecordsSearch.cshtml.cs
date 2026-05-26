using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class StaffRecordsSearchModel : PageModel
{
    public string SearchTerm { get; private set; } = string.Empty;

    public void OnGet(string? searchTerm)
    {
        SearchTerm = searchTerm ?? string.Empty;
    }
}
