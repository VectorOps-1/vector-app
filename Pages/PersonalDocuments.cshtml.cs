using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace vector_app_local.Pages;

public class PersonalDocumentsModel : PageModel
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tif", ".tiff",
        ".doc", ".docx", ".rtf", ".txt",
        ".xls", ".xlsx", ".csv"
    };

    [BindProperty]
    public string? StaffName { get; set; }

    [BindProperty]
    public string? StaffIdentifier { get; set; }

    [BindProperty]
    public string? DocumentType { get; set; }

    [BindProperty]
    public string? CertificateName { get; set; }

    [BindProperty]
    public DateTime? ExpiryDate { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    [BindProperty]
    public List<IFormFile> PersonalFiles { get; set; } = new();

    public string? StatusMessage { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(StaffName))
        {
            StatusMessage = "Enter the staff name before submitting.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(DocumentType))
        {
            StatusMessage = "Select a document type before submitting.";
            return Page();
        }

        if ((DocumentType is "Certificate" or "Accreditation" or "Professional Registration" or "Training Record") && string.IsNullOrWhiteSpace(CertificateName))
        {
            StatusMessage = "Enter the certificate, accreditation, registration, or training name before submitting.";
            return Page();
        }

        if (PersonalFiles.Count == 0)
        {
            StatusMessage = "Select one or more personal documents before submitting.";
            return Page();
        }

        var unsupportedFile = PersonalFiles.FirstOrDefault(file => !AllowedExtensions.Contains(Path.GetExtension(file.FileName)));
        if (unsupportedFile is not null)
        {
            StatusMessage = $"Unsupported file type: {unsupportedFile.FileName}.";
            return Page();
        }

        var documentName = string.IsNullOrWhiteSpace(CertificateName) ? DocumentType : CertificateName;
        StatusMessage = $"{PersonalFiles.Count} document(s) ready to save against {StaffName} for {documentName}. Database storage and audit logging will be connected in the production data phase.";
        return Page();
    }
}
