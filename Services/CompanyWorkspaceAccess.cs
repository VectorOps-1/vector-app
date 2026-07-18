using System.Security.Cryptography;
using System.Text;
using vector_app_local.Models;

namespace vector_app_local.Services;

public static class CompanyWorkspaceAccess
{
    public const string LastWorkspaceCookieName = "AcuityOps.LastWorkspace";

    public static void EnsureWorkspaceAccess(Company company)
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(company.WorkspaceSlug))
        {
            company.WorkspaceSlug = GenerateWorkspaceSlug(company.Id);
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(company.WorkspaceAccessCode))
        {
            company.WorkspaceAccessCode = GenerateAccessCode();
            changed = true;
        }

        if (changed)
        {
            company.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public static bool AccessCodeMatches(Company company, string? submittedCode)
    {
        return !string.IsNullOrWhiteSpace(company.WorkspaceAccessCode)
            && !string.IsNullOrWhiteSpace(submittedCode)
            && string.Equals(
                company.WorkspaceAccessCode.Trim(),
                submittedCode.Trim(),
                StringComparison.OrdinalIgnoreCase);
    }

    public static string? NormalizeWorkspaceSlug(string? workspaceLinkOrSlug)
    {
        if (string.IsNullOrWhiteSpace(workspaceLinkOrSlug)) return null;
        var value = workspaceLinkOrSlug.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            value = uri.AbsolutePath;
        value = value.Split('?', '#')[0].Trim('/');
        if (value.Contains('/')) value = value.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        value = Uri.UnescapeDataString(value).Trim();
        return value.Length is > 0 and <= 180 ? value : null;
    }

    private static string GenerateWorkspaceSlug(int companyId)
    {
        return $"acuityops-workspace-{companyId}-{RandomToken(5).ToLowerInvariant()}";
    }

    private static string GenerateAccessCode()
    {
        return $"ACUITY-{RandomToken(4)}-{RandomToken(4)}";
    }

    private static string RandomToken(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        var builder = new StringBuilder(byteCount * 2);
        foreach (var item in bytes)
        {
            builder.Append(item.ToString("X2"));
        }

        return builder.ToString();
    }
}
