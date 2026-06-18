using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class WorkspaceModel : PageModel
{
    public string CompanyLoginUrl { get; private set; } = "/CompanyLogin";

    public void OnGet(string? workspaceSlug)
    {
        if (!string.IsNullOrWhiteSpace(workspaceSlug))
        {
            CompanyLoginUrl = $"/CompanyLogin/{Uri.EscapeDataString(workspaceSlug.Trim())}";
        }
    }
}
