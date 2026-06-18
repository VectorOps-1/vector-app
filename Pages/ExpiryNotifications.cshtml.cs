using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class ExpiryNotificationsModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly ExpiryPressureService _expiryPressure;

    public ExpiryNotificationsModel(CurrentUserService currentUser, ExpiryPressureService expiryPressure)
    {
        _currentUser = currentUser;
        _expiryPressure = expiryPressure;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public string ScopeLabel { get; private set; } = string.Empty;
    public IReadOnlyList<ExpiryPressureItem> Rows { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        ScopeLabel = BuildScopeLabel(currentUser);
        Rows = await _expiryPressure.LoadForUserAsync(currentUser);
        ApplySearchFilter();

        return Page();
    }

    private void ApplySearchFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            return;
        }

        var search = SearchTerm.Trim();
        Rows = Rows
            .Where(row =>
                Contains(search, row.AssetType) ||
                Contains(search, row.AssetLabel) ||
                Contains(search, row.Source) ||
                Contains(search, row.Location) ||
                Contains(search, row.OwnerName) ||
                Contains(search, row.AlertBand) ||
                Contains(search, row.Status) ||
                Contains(search, row.ActionState))
            .ToList();
    }

    private static bool Contains(string search, string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildScopeLabel(AppUser currentUser)
    {
        if (CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return "Company-wide expiry and service pressure";
        }

        if (string.Equals(currentUser.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase))
        {
            return "Assigned operational areas only";
        }

        return "Your own licence and CPD compliance";
    }
}
