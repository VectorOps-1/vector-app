using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;
using vector_app_local.Services;

namespace vector_app_local.Pages;

public class EditStaffProfileModel : PageModel
{
    private readonly VectorDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly StaffStructureSetupService _staffStructure;
    private readonly IUserActionAuthorizationService _authorization;

    public EditStaffProfileModel(
        VectorDbContext db,
        CurrentUserService currentUser,
        StaffStructureSetupService staffStructure,
        IUserActionAuthorizationService authorization)
    {
        _db = db;
        _currentUser = currentUser;
        _staffStructure = staffStructure;
        _authorization = authorization;
    }

    [BindProperty(SupportsGet = true)] public int? StaffUserId { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    [BindProperty] public string FullName { get; set; } = string.Empty;
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string? NationalId { get; set; }
    [BindProperty] public string? CellNumber { get; set; }
    [BindProperty] public string? StaffIdentifier { get; set; }
    [BindProperty] public string? QualificationFunction { get; set; }
    [BindProperty] public string? PractitionerNumber { get; set; }
    [BindProperty] public DateTime? AnnualLicenseExpiryDate { get; set; }
    [BindProperty] public string? CpdComplianceStatus { get; set; }
    [BindProperty] public DateTime? CpdComplianceExpiryDate { get; set; }
    [BindProperty] public int? AssignedOperationalAreaId { get; set; }
    [BindProperty] public string Status { get; set; } = "Active";

    public string RoleName { get; private set; } = string.Empty;
    public string ScopeLabel { get; private set; } = string.Empty;
    public bool CanEditManagerFields { get; private set; }
    public bool IsSelfEdit { get; private set; }
    public string? StatusMessage { get; private set; }
    public List<SelectListItem> AreaOptions { get; private set; } = new();
    public List<SelectListItem> StatusOptions { get; private set; } = new()
    {
        new("Active", "Active"),
        new("Inactive", "Inactive"),
        new("Suspended", "Suspended")
    };
    public List<SelectListItem> CpdStatusOptions { get; private set; } = new()
    {
        new("N/A", "N/A"),
        new("Compliant", "Compliant"),
        new("Pending", "Pending"),
        new("Non-compliant", "Non-compliant"),
        new("Expired", "Expired"),
        new("Under review", "Under review")
    };
    public List<SelectListItem> StaffQualificationOptions { get; private set; } = new();
    public StaffStructureSetupSnapshot? StaffStructureSnapshot { get; private set; }
    public bool StaffPractitionerNumberRequired => StaffStructureSnapshot?.PractitionerNumberRequired ?? false;
    public bool StaffAnnualLicenseExpiryRequired => StaffStructureSnapshot?.AnnualLicenseExpiryRequired ?? false;
    public bool StaffCpdTrackingRequired => StaffStructureSnapshot?.CpdTrackingRequired ?? false;
    public string? StaffIdFormat => StaffStructureSnapshot?.StaffIdFormat;

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        var targetUser = await LoadTargetUserAsync(currentUser, StaffUserId ?? currentUser.Id);
        if (targetUser is null)
        {
            return RedirectToPage("/Home");
        }

        await LoadPageStateAsync(currentUser, targetUser);
        LoadForm(targetUser);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUser = await _currentUser.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return RedirectToPage("/RoleLogin", new { access = CurrentUserService.StaffAccess });
        }

        var targetUser = await LoadTargetUserAsync(currentUser, StaffUserId ?? currentUser.Id, tracking: true);
        if (targetUser is null)
        {
            return RedirectToPage("/Home");
        }

        await LoadPageStateAsync(currentUser, targetUser);

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ModelState.AddModelError(nameof(FullName), "Enter the staff member's name.");
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError(nameof(Email), "Enter an email address.");
        }

        var email = Email.Trim();
        var duplicateEmail = await _db.AppUsers.AnyAsync(user =>
            user.CompanyId == currentUser.CompanyId &&
            user.Id != targetUser.Id &&
            user.Email == email &&
            user.Status != "Deleted");

        if (duplicateEmail)
        {
            ModelState.AddModelError(nameof(Email), "Another staff profile already uses this email address.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var now = DateTime.UtcNow;
        targetUser.FullName = FullName.Trim();
        targetUser.Email = email;
        targetUser.NationalId = NormalizeOptional(NationalId);
        targetUser.CellNumber = NormalizeOptional(CellNumber);

        if (CanEditManagerFields)
        {
            var clinicalScope = NormalizeOptional(QualificationFunction);
            if (clinicalScope is null)
            {
                ModelState.AddModelError(nameof(QualificationFunction), "Select the clinical qualification / scope.");
                return Page();
            }

            if (clinicalScope != "N/A" &&
                StaffQualificationOptions.All(option => !string.Equals(option.Value, clinicalScope, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(QualificationFunction), "Select a configured clinical qualification / scope.");
                return Page();
            }

            var isClinicalScope = !string.Equals(clinicalScope, "N/A", StringComparison.OrdinalIgnoreCase);
            if (isClinicalScope && StaffPractitionerNumberRequired && string.IsNullOrWhiteSpace(PractitionerNumber))
            {
                ModelState.AddModelError(nameof(PractitionerNumber), "Practitioner number is required by the company staff setup rules.");
                return Page();
            }

            if (isClinicalScope && StaffAnnualLicenseExpiryRequired && !AnnualLicenseExpiryDate.HasValue)
            {
                ModelState.AddModelError(nameof(AnnualLicenseExpiryDate), "Annual licence expiry is required by the company staff setup rules.");
                return Page();
            }

            if (isClinicalScope && StaffCpdTrackingRequired && (string.IsNullOrWhiteSpace(CpdComplianceStatus) || !CpdComplianceExpiryDate.HasValue))
            {
                ModelState.AddModelError(nameof(CpdComplianceStatus), "CPD compliance status and CPD valid-until date are required by the company staff setup rules.");
                return Page();
            }

            targetUser.StaffIdentifier = NormalizeOptional(StaffIdentifier);
            targetUser.QualificationFunction = clinicalScope;
            targetUser.PractitionerNumber = NormalizeOptional(PractitionerNumber);
            targetUser.AnnualLicenseExpiryDate = AnnualLicenseExpiryDate;
            targetUser.CpdComplianceStatus = NormalizeOptional(CpdComplianceStatus);
            targetUser.CpdComplianceExpiryDate = CpdComplianceExpiryDate;
            targetUser.AssignedOperationalAreaId = AssignedOperationalAreaId;
            targetUser.Status = string.IsNullOrWhiteSpace(Status) ? "Active" : Status.Trim();
        }

        _db.AuditLogs.Add(new AuditLog
        {
            CompanyId = currentUser.CompanyId,
            AppUserId = currentUser.Id,
            Action = IsSelfEdit ? "Own staff profile updated" : "Staff profile updated",
            EntityType = "AppUser",
            EntityId = targetUser.Id,
            Details = IsSelfEdit
                ? $"{targetUser.FullName} updated their own profile."
                : $"{currentUser.FullName} updated staff profile: {targetUser.FullName}.",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        if (targetUser.Id == currentUser.Id)
        {
            HttpContext.Session.SetString(CurrentUserService.FullNameSessionKey, targetUser.FullName);
        }

        StatusMessage = "Staff profile updated.";
        await LoadPageStateAsync(currentUser, targetUser);
        LoadForm(targetUser);
        return Page();
    }

    private async Task<AppUser?> LoadTargetUserAsync(AppUser currentUser, int targetUserId, bool tracking = false)
    {
        var query = _db.AppUsers
            .Include(user => user.AppRole)
            .Include(user => user.AssignedOperationalArea)
            .Where(user =>
                user.CompanyId == currentUser.CompanyId &&
                user.Id == targetUserId &&
                user.Status != "Deleted");

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        var targetUser = await query.FirstOrDefaultAsync();
        if (targetUser is null)
        {
            return null;
        }

        return await CanAccessTargetAsync(currentUser, targetUser) ? targetUser : null;
    }

    private async Task LoadPageStateAsync(AppUser currentUser, AppUser targetUser)
    {
        IsSelfEdit = targetUser.Id == currentUser.Id;
        RoleName = targetUser.AppRole?.Name ?? "Unassigned";

        var isOperationalManager = string.Equals(currentUser.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase);
        var targetIsStaff = string.Equals(targetUser.AppRole?.Name, "Staff", StringComparison.OrdinalIgnoreCase);
        var assignedAreaIds = await LoadAssignedAreaIdsAsync(currentUser);
        var targetInManagerScope = targetUser.AssignedOperationalAreaId.HasValue &&
            assignedAreaIds.Contains(targetUser.AssignedOperationalAreaId.Value);
        var canEditStaffRegister = await _authorization.HasPermissionAsync(currentUser, UserActionPermissions.RegistersStaffEdit);
        var isSenior = CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name);

        CanEditManagerFields = canEditStaffRegister &&
            (isSenior || (isOperationalManager && targetIsStaff && targetInManagerScope));
        ScopeLabel = CanEditManagerFields
            ? "Manager edit: register and personal details"
            : "Self edit: personal profile details";

        await LoadAreaOptionsAsync(currentUser, targetUser, assignedAreaIds, isSenior);
        await LoadStaffStructureOptionsAsync(currentUser.CompanyId, targetUser.QualificationFunction);
    }

    private void LoadForm(AppUser targetUser)
    {
        StaffUserId = targetUser.Id;
        FullName = targetUser.FullName;
        Email = targetUser.Email;
        NationalId = targetUser.NationalId;
        CellNumber = targetUser.CellNumber;
        StaffIdentifier = targetUser.StaffIdentifier;
        QualificationFunction = targetUser.QualificationFunction;
        PractitionerNumber = targetUser.PractitionerNumber;
        AnnualLicenseExpiryDate = targetUser.AnnualLicenseExpiryDate;
        CpdComplianceStatus = targetUser.CpdComplianceStatus;
        CpdComplianceExpiryDate = targetUser.CpdComplianceExpiryDate;
        AssignedOperationalAreaId = targetUser.AssignedOperationalAreaId;
        Status = targetUser.Status;
        EnsureCpdStatusOption(CpdComplianceStatus);
    }

    private async Task<bool> CanAccessTargetAsync(AppUser currentUser, AppUser targetUser)
    {
        if (targetUser.Id == currentUser.Id)
        {
            return true;
        }

        if (CurrentUserService.IsSeniorAccessRole(currentUser.AppRole?.Name))
        {
            return true;
        }

        if (!string.Equals(currentUser.AppRole?.Name, "Operational Management", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var assignedAreaIds = await LoadAssignedAreaIdsAsync(currentUser);
        return targetUser.AssignedOperationalAreaId.HasValue &&
            assignedAreaIds.Contains(targetUser.AssignedOperationalAreaId.Value);
    }

    private async Task LoadAreaOptionsAsync(AppUser currentUser, AppUser targetUser, IReadOnlyCollection<int> assignedAreaIds, bool isSenior)
    {
        var areaQuery = _db.OperationalAreas
            .AsNoTracking()
            .Where(area => area.CompanyId == currentUser.CompanyId && area.Status == "Active");

        if (!isSenior)
        {
            areaQuery = areaQuery.Where(area => assignedAreaIds.Contains(area.Id));
        }

        AreaOptions = await areaQuery
            .OrderBy(area => area.AreaType)
            .ThenBy(area => area.Name)
            .Select(area => new SelectListItem
            {
                Value = area.Id.ToString(),
                Text = area.AreaType + " - " + area.Name
            })
            .ToListAsync();

        if (isSenior)
        {
            AreaOptions.Insert(0, new SelectListItem("Unassigned", string.Empty));
        }
        else if (targetUser.AssignedOperationalAreaId.HasValue &&
            AreaOptions.All(option => option.Value != targetUser.AssignedOperationalAreaId.Value.ToString()))
        {
            AreaOptions.Add(new SelectListItem(targetUser.AssignedOperationalArea?.Name ?? "Current assigned area", targetUser.AssignedOperationalAreaId.Value.ToString()));
        }
    }

    private async Task<List<int>> LoadAssignedAreaIdsAsync(AppUser currentUser)
    {
        var assignedAreaIds = await _db.ManagerOperationalAreaAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.CompanyId == currentUser.CompanyId &&
                assignment.ManagerUserId == currentUser.Id &&
                assignment.Status == "Active")
            .Select(assignment => assignment.OperationalAreaId)
            .ToListAsync();

        if (currentUser.AssignedOperationalAreaId.HasValue &&
            !assignedAreaIds.Contains(currentUser.AssignedOperationalAreaId.Value))
        {
            assignedAreaIds.Add(currentUser.AssignedOperationalAreaId.Value);
        }

        return assignedAreaIds;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void EnsureCpdStatusOption(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (CpdStatusOptions.Any(option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        CpdStatusOptions.Add(new SelectListItem(value, value));
    }

    private async Task LoadStaffStructureOptionsAsync(int companyId, string? selectedValue)
    {
        StaffStructureSnapshot = await _staffStructure.GetSnapshotAsync(companyId);
        StaffQualificationOptions = new List<SelectListItem>
        {
            new() { Value = "N/A", Text = "N/A", Selected = string.Equals(selectedValue, "N/A", StringComparison.OrdinalIgnoreCase) }
        };

        StaffQualificationOptions.AddRange(StaffStructureSnapshot.Qualifications.Select(option => new SelectListItem
        {
            Value = option.Name,
            Text = option.Name,
            Selected = string.Equals(selectedValue, option.Name, StringComparison.OrdinalIgnoreCase)
        }));

        if (!string.IsNullOrWhiteSpace(selectedValue) &&
            StaffQualificationOptions.All(option => !string.Equals(option.Value, selectedValue, StringComparison.OrdinalIgnoreCase)))
        {
            StaffQualificationOptions.Add(new SelectListItem(selectedValue, selectedValue, selected: true));
        }
    }
}
