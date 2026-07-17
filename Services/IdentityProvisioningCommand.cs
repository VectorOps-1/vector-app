using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace vector_app_local.Services;

public sealed class IdentityProvisioningCommand
{
    public const string CommandName = "--provision-login-identities";
    private const string InventoryOption = "--inventory";
    private const string ManifestOption = "--manifest";
    private const string ExecuteOption = "--execute";
    private const string ConfirmationOption = "--confirm-sha256";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private readonly IdentityProvisioningService _service;

    public IdentityProvisioningCommand(IdentityProvisioningService service)
    {
        _service = service;
    }

    public async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        var inventory = HasOption(args, InventoryOption);
        var execute = HasOption(args, ExecuteOption);
        var manifestPath = GetOptionValue(args, ManifestOption);
        var confirmedHash = GetOptionValue(args, ConfirmationOption);

        if (inventory)
        {
            if (execute || manifestPath is not null || confirmedHash is not null)
            {
                await error.WriteLineAsync("Inventory mode cannot be combined with manifest or execute options.");
                return 2;
            }

            try
            {
                var rows = await _service.GetInventoryAsync(cancellationToken);
                await output.WriteLineAsync(JsonSerializer.Serialize(rows, JsonOptions));
                return 0;
            }
            catch (Exception ex)
            {
                await error.WriteLineAsync($"Identity inventory failed without making changes: {ex.GetType().Name}.");
                return 1;
            }
        }

        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            await error.WriteLineAsync("Specify --inventory or --manifest <path>. Provisioning defaults to dry-run.");
            return 2;
        }

        if (!File.Exists(manifestPath))
        {
            await error.WriteLineAsync("The provisioning manifest does not exist.");
            return 2;
        }

        byte[] manifestBytes;
        IdentityProvisioningManifest? manifest;
        try
        {
            manifestBytes = await File.ReadAllBytesAsync(manifestPath, cancellationToken);
            manifest = JsonSerializer.Deserialize<IdentityProvisioningManifest>(manifestBytes, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            await error.WriteLineAsync($"The provisioning manifest could not be read: {ex.GetType().Name}.");
            return 2;
        }

        if (manifest is null)
        {
            await error.WriteLineAsync("The provisioning manifest is empty.");
            return 2;
        }

        var actualHash = Convert.ToHexString(SHA256.HashData(manifestBytes)).ToLowerInvariant();
        if (execute && !string.Equals(actualHash, confirmedHash, StringComparison.OrdinalIgnoreCase))
        {
            await error.WriteLineAsync("Execution requires --confirm-sha256 with the exact manifest SHA-256.");
            return 2;
        }

        IdentityProvisioningResult result;
        try
        {
            result = await _service.ProvisionAsync(
                manifest,
                Environment.GetEnvironmentVariable,
                execute,
                cancellationToken);
        }
        catch (Exception ex)
        {
            await error.WriteLineAsync($"Identity provisioning validation failed without making changes: {ex.GetType().Name}.");
            return 1;
        }

        await output.WriteLineAsync($"Manifest SHA-256: {actualHash}");
        await output.WriteLineAsync(execute ? "Mode: execute" : "Mode: dry-run (no writes)");
        await output.WriteLineAsync(JsonSerializer.Serialize(result.Accounts, JsonOptions));
        if (!result.Succeeded)
        {
            foreach (var message in result.Errors)
            {
                await error.WriteLineAsync(message);
            }

            return 1;
        }

        await output.WriteLineAsync(result.Executed
            ? "Provisioning completed. Temporary passwords were not printed."
            : "Dry-run passed. No database records were changed.");
        return 0;
    }

    private static bool HasOption(IReadOnlyList<string> args, string option) =>
        args.Any(arg => string.Equals(arg, option, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string option)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
