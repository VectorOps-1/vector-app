using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ChecklistTemplateViewModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;

    public ChecklistTemplateViewModel(VectorDbContext db, CurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [BindProperty(SupportsGet = true)]
    public int TemplateId { get; set; }

    public ChecklistTemplate? Template { get; private set; }
    public List<ChecklistSectionPreview> Sections { get; private set; } = new();
    public string ChecklistRoute => IsFullAuditTemplateName(Template?.Name)
        ? "full-audit"
        : "daily-vehicle";
    public string TemplateDisplayName => IsFullAuditTemplateName(Template?.Name)
        ? "Full Audit"
        : Template?.Name ?? "Checklist";

    private static bool IsFullAuditTemplateName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) &&
            name.Contains("Full Audit", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.OperationalManagementAccess });
        }

        Template = await _db.ChecklistTemplates
            .AsNoTracking()
            .Include(template => template.Sections)
            .ThenInclude(section => section.Items)
            .ThenInclude(item => item.ColumnDefinitions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(template =>
                template.CompanyId == currentUser.CompanyId &&
                template.Id == TemplateId &&
                template.Status != "Deleted");

        if (Template is null)
        {
            return RedirectToPage("/EditChecklist", new { view = "register" });
        }

        Sections = Template.Sections
            .OrderBy(section => section.DisplayOrder)
            .Select(BuildSectionPreview)
            .ToList();

        return Page();
    }

    private static ChecklistSectionPreview BuildSectionPreview(ChecklistSection section)
    {
        var orderedItems = section.Items
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Prompt)
            .ToList();

        var rows = orderedItems
            .Where(item => item.ParentChecklistItemId is null)
            .Select(item => new ChecklistItemPreview(
                item.Id,
                item.Prompt,
                item.ResponseType,
                item.ItemKind,
                item.IsRequired,
                item.IsReadinessCritical,
                item.ColumnDefinitions
                    .OrderBy(column => column.SortOrder)
                    .Select(column => new ChecklistColumnPreview(
                        column.Heading,
                        column.ResponseType,
                        column.RegisterSource,
                        column.IsRequired,
                        column.IsEditable,
                        column.PullsFromRegister,
                        column.AffectsReadiness))
                    .ToList(),
                orderedItems
                    .Where(subItem => subItem.ParentChecklistItemId == item.Id)
                    .OrderBy(subItem => subItem.DisplayOrder)
                    .Select(subItem => new ChecklistItemPreview(
                        subItem.Id,
                        subItem.Prompt,
                        subItem.ResponseType,
                        subItem.ItemKind,
                        subItem.IsRequired,
                        subItem.IsReadinessCritical,
                        subItem.ColumnDefinitions
                            .OrderBy(column => column.SortOrder)
                            .Select(column => new ChecklistColumnPreview(
                                column.Heading,
                                column.ResponseType,
                                column.RegisterSource,
                                column.IsRequired,
                                column.IsEditable,
                                column.PullsFromRegister,
                                column.AffectsReadiness))
                            .ToList(),
                        new List<ChecklistItemPreview>()))
                    .ToList()))
            .ToList();

        return new ChecklistSectionPreview(section.Name, rows);
    }
}

public record ChecklistSectionPreview(string Name, IReadOnlyList<ChecklistItemPreview> Items)
{
    public bool IsEquipmentTable => Items.Any(item =>
        item.ItemKind.Contains("Equipment", StringComparison.OrdinalIgnoreCase) ||
        item.ResponseType.Contains("Equipment", StringComparison.OrdinalIgnoreCase));
}

public record ChecklistItemPreview(
    int Id,
    string Prompt,
    string ResponseType,
    string ItemKind,
    bool IsRequired,
    bool IsReadinessCritical,
    IReadOnlyList<ChecklistColumnPreview> Columns,
    IReadOnlyList<ChecklistItemPreview> SubItems)
{
    public bool IsSchematicBlock => ItemKind.Equals("SchematicBlock", StringComparison.OrdinalIgnoreCase) ||
        ResponseType.Contains("Schematic", StringComparison.OrdinalIgnoreCase);
}

public record ChecklistColumnPreview(
    string Heading,
    string ResponseType,
    string? RegisterSource,
    bool IsRequired,
    bool IsEditable,
    bool PullsFromRegister,
    bool AffectsReadiness);
