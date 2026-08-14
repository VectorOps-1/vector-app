using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class PilotEntitlementModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly PilotEntitlementService _entitlements;

    public PilotEntitlementModel(CurrentUserService currentUser, PilotEntitlementService entitlements)
    {
        _currentUser = currentUser;
        _entitlements = entitlements;
    }

    public PilotEntitlementSnapshot Snapshot { get; private set; } = new(null, 0, SubscriptionTiers.Base,
        "NotConfigured", null, null, FeatureAccessMode.Unavailable, null, string.Empty);
    public IReadOnlyList<PilotEntitlementEvent> History { get; private set; } = Array.Empty<PilotEntitlementEvent>();

    [BindProperty] public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    [BindProperty] public DateTime NewExpiryDate { get; set; }
    [BindProperty] public string? Note { get; set; }
    [BindProperty] public string? RevocationReason { get; set; }

    [TempData] public string? StatusMessage { get; set; }
    [TempData] public bool StatusSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var actor = await RequireManagerAsync();
        if (actor is null) return RedirectToPage("/Home", new { permissionDenied = "true" });
        await LoadAsync(actor.CompanyId, cancellationToken);
        if (Snapshot.ExpiresAtUtc.HasValue) NewExpiryDate = Snapshot.ExpiresAtUtc.Value.AddMonths(1).Date;
        return Page();
    }

    public async Task<IActionResult> OnPostActivateAsync(CancellationToken cancellationToken)
    {
        var actor = await RequireManagerAsync();
        if (actor is null) return RedirectToPage("/Home", new { permissionDenied = "true" });
        var result = await _entitlements.ActivateAsync(actor, StartDate, Note, cancellationToken);
        SetStatus(result);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExtendAsync(CancellationToken cancellationToken)
    {
        var actor = await RequireManagerAsync();
        if (actor is null) return RedirectToPage("/Home", new { permissionDenied = "true" });
        var result = await _entitlements.ExtendAsync(actor, NewExpiryDate, Note, cancellationToken);
        SetStatus(result);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAsync(CancellationToken cancellationToken)
    {
        var actor = await RequireManagerAsync();
        if (actor is null) return RedirectToPage("/Home", new { permissionDenied = "true" });
        var result = await _entitlements.RevokeAsync(actor, RevocationReason ?? string.Empty, cancellationToken);
        SetStatus(result);
        return RedirectToPage();
    }

    private async Task<AppUser?> RequireManagerAsync()
    {
        var actor = await _currentUser.GetCurrentUserAsync();
        return actor is not null && CurrentUserService.IsSeniorAccessRole(actor.AppRole?.Name) ? actor : null;
    }

    private async Task LoadAsync(int companyId, CancellationToken cancellationToken)
    {
        Snapshot = await _entitlements.GetAsync(companyId, cancellationToken);
        History = await _entitlements.GetHistoryAsync(companyId, cancellationToken);
    }

    private void SetStatus(EntitlementOperationResult result)
    {
        StatusSuccess = result.Success;
        StatusMessage = result.Message;
    }
}
