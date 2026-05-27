using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class LogoUploadModel : PageModel
{
    private readonly IWebHostEnvironment _environment;
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg"];
    private static readonly string[] AllowedContentTypes = ["image/png", "image/jpeg"];

    public LogoUploadModel(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [BindProperty]
    public IFormFile? LogoFile { get; set; }

    public string? ExistingLogoPath { get; private set; }
    public string? StatusMessage { get; private set; }
    public bool ActionSaved { get; private set; }

    public void OnGet()
    {
        ExistingLogoPath = GetExistingLogoPath();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (LogoFile is null || LogoFile.Length == 0)
        {
            StatusMessage = "Choose a PNG or JPEG logo before saving.";
            ExistingLogoPath = GetExistingLogoPath();
            return Page();
        }

        var extension = Path.GetExtension(LogoFile.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(LogoFile.ContentType))
        {
            StatusMessage = "Only PNG and JPEG logo files are supported.";
            ExistingLogoPath = GetExistingLogoPath();
            return Page();
        }

        var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "company");
        Directory.CreateDirectory(uploadFolder);

        foreach (var existingFile in Directory.GetFiles(uploadFolder, "company-logo.*"))
        {
            System.IO.File.Delete(existingFile);
        }

        var fileName = $"company-logo{extension}";
        var filePath = Path.Combine(uploadFolder, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await LogoFile.CopyToAsync(stream);

        StatusMessage = "Logo saved.";
        ActionSaved = true;
        ExistingLogoPath = $"/uploads/company/{fileName}?v={DateTime.UtcNow.Ticks}";
        return Page();
    }

    private string? GetExistingLogoPath()
    {
        var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "company");
        if (!Directory.Exists(uploadFolder))
        {
            return null;
        }

        var existingFile = Directory.GetFiles(uploadFolder, "company-logo.*").FirstOrDefault();
        if (existingFile is null)
        {
            return null;
        }

        return $"/uploads/company/{Path.GetFileName(existingFile)}?v={DateTime.UtcNow.Ticks}";
    }
}
