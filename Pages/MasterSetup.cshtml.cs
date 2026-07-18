using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class MasterSetupModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly VectorDbContext _db;

    public MasterSetupModel(CurrentUserService currentUser, VectorDbContext db)
    {
        _currentUser = currentUser;
        _db = db;
    }

    public bool GuidedImportAvailable { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetCurrentUserAsync();
        if (user is null) return;
        var tier = await _db.Companies.AsNoTracking().Where(company => company.Id == user.CompanyId)
            .Select(company => company.SubscriptionTier).SingleOrDefaultAsync(cancellationToken);
        GuidedImportAvailable = SubscriptionTiers.IsAtLeast(tier, SubscriptionTiers.Pro);
    }
}
