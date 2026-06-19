namespace vector_app_local.Services;

public static class ChecklistDisplayService
{
    public static string TemplateName(string? checklistName)
    {
        if (string.IsNullOrWhiteSpace(checklistName))
        {
            return string.Empty;
        }

        return checklistName.Contains("Full Audit", StringComparison.OrdinalIgnoreCase)
            ? "Full Audit"
            : checklistName.Trim();
    }
}
