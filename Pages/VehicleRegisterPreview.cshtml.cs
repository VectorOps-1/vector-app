using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class VehicleRegisterPreviewModel : PageModel
{
    public string FileName { get; private set; } = "Uploaded vehicle register";
    public IReadOnlyList<VehicleSchematicDefinition> PublishedSchematics => VehicleSchematicLibrary.Published;

    public void OnGet(string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            FileName = fileName;
        }
    }
}
