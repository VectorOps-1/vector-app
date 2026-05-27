using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class CompanyNameModel : PageModel
{
    private readonly IWebHostEnvironment _environment;

    public CompanyNameModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [BindProperty]
    public string CompanyName { get; set; } = string.Empty;

    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    public void OnGet()
    {
        CompanyName = CompanyBranding.GetCompanyName(_environment);
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            StatusMessage = "Enter a company name before saving.";
            CompanyName = CompanyBranding.GetCompanyName(_environment);
            return Page();
        }

        CompanyBranding.SaveCompanyName(_environment, CompanyName);
        StatusMessage = "Company name saved.";
        ActionSaved = true;
        CompanyName = CompanyBranding.GetCompanyName(_environment);
        return Page();
    }
}
