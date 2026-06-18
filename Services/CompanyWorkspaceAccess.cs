using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using vector_app_local.Models;

namespace vector_app_local.Services;

public static class CompanyWorkspaceAccess
{
    public static void EnsureWorkspaceAccess(Company company)
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(company.WorkspaceSlug))
        {
            company.WorkspaceSlug = GenerateWorkspaceSlug(company.Name, company.Id);
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

    private static string GenerateWorkspaceSlug(string companyName, int companyId)
    {
        var baseName = Regex.Replace(companyName.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "company";
        }

        return $"{baseName}-{companyId}-{RandomToken(5).ToLowerInvariant()}";
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
