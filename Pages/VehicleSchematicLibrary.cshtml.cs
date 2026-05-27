using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class VehicleSchematicLibraryModel : PageModel
{
    public IReadOnlyList<VehicleSchematicDefinition> Schematics => VehicleSchematicLibrary.All;
    public int PublishedCount => Schematics.Count(schematic => schematic.IsPublished);
}
