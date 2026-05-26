using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class StaffFilesPreviewModel : PageModel
{
    public int FileCount { get; private set; }

    public void OnGet(int count = 0)
    {
        FileCount = count;
    }
}
