using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class StockRegisterPreviewModel : PageModel
{
    public string FileName { get; private set; } = "Uploaded stock register";

    public void OnGet(string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            FileName = fileName;
        }
    }
}
