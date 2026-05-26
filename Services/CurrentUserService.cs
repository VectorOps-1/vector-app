using Microsoft.EntityFrameworkCore;
using vector_app_local.Data;
using vector_app_local.Models;

namespace vector_app_local.Services;

public class CurrentUserService
{
    public const string StaffAccess = "staff";
    public const string OperationalManagementAccess = "operational-management";
    public const string SeniorManagementAccess = "senior-management";

    public const string UserIdSessionKey = "Vector.CurrentUserId";
    public const string CompanyIdSessionKey = "Vector.CompanyId";
    public const string FullNameSessionKey = "Vector.FullName";
    public const string RoleNameSessionKey = "Vector.RoleName";
    public const string AccessViewSessionKey = "Vector.AccessView";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly VectorDbContext _db;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, VectorDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public int? CurrentUserId => _httpContextAccessor.HttpContext?.Session.GetInt32(UserIdSessionKey);

    public string? CurrentAccessView => _httpContextAccessor.HttpContext?.Session.GetString(AccessViewSessionKey);

    public async Task<AppUser?> GetCurrentUserAsync()
    {
        var userId = CurrentUserId;
        if (!userId.HasValue)
        {
            return null;
        }

        return await _db.AppUsers
            .Include(user => user.AppRole)
            .Include(user => user.Company)
            .FirstOrDefaultAsync(user => user.Id == userId.Value && user.Status == "Active");
    }

    public void SignIn(AppUser user, string accessView)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session is null)
        {
            return;
        }

        session.SetInt32(UserIdSessionKey, user.Id);
        session.SetInt32(CompanyIdSessionKey, user.CompanyId);
        session.SetString(FullNameSessionKey, user.FullName);
        session.SetString(RoleNameSessionKey, user.AppRole?.Name ?? string.Empty);
        session.SetString(AccessViewSessionKey, NormalizeAccessView(accessView));
    }

    public void SignOut()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.Remove(UserIdSessionKey);
        session?.Remove(CompanyIdSessionKey);
        session?.Remove(FullNameSessionKey);
        session?.Remove(RoleNameSessionKey);
        session?.Remove(AccessViewSessionKey);
    }

    public static string NormalizeAccessView(string? access)
    {
        return access switch
        {
            StaffAccess => StaffAccess,
            SeniorManagementAccess => SeniorManagementAccess,
            _ => OperationalManagementAccess
        };
    }

    public static string AccessViewToClientRole(string? accessView)
    {
        return NormalizeAccessView(accessView) switch
        {
            StaffAccess => "staff",
            SeniorManagementAccess => "senior",
            _ => "manager"
        };
    }

    public static bool AccessAllowsRole(string accessView, string? roleName)
    {
        return NormalizeAccessView(accessView) switch
        {
            StaffAccess => string.Equals(roleName, "Staff", StringComparison.OrdinalIgnoreCase),
            SeniorManagementAccess => IsSeniorAccessRole(roleName),
            _ => string.Equals(roleName, "Operational Management", StringComparison.OrdinalIgnoreCase)
        };
    }

    public static bool CanSendTasks(string? roleName)
    {
        return string.Equals(roleName, "Operational Management", StringComparison.OrdinalIgnoreCase)
            || IsSeniorAccessRole(roleName);
    }

    public static bool IsSeniorAccessRole(string? roleName)
    {
        return string.Equals(roleName, "Senior Management", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, "Company Owner", StringComparison.OrdinalIgnoreCase);
    }
}
