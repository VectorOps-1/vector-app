using Microsoft.AspNetCore.Mvc.RazorPages;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class MasterSetupModel : PageModel
{
    private readonly CurrentUserService _currentUser;
    private readonly IFeatureAccessService _features;

    public MasterSetupModel(CurrentUserService currentUser, IFeatureAccessService features)
    {
        _currentUser = currentUser;
        _features = features;
    }

    public bool GuidedImportAvailable { get; private set; }
    public bool GuidedImportReadOnly { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var user = await _currentUser.GetCurrentUserAsync();
        if (user is null) return;
        var access = await _features.GetFeatureAccessAsync(VectorFeatures.GuidedRegisterImport, cancellationToken);
        GuidedImportAvailable = access.IsFullAccess;
        GuidedImportReadOnly = access.IsReadOnlyExport;
    }
}
