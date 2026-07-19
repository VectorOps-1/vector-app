namespace vector_app_local.Services;

public sealed record ProductComplianceAuthorizationResult(
    bool IsAuthorized,
    string ActorIdentifier,
    string? DenialReason = null);

public interface IProductComplianceAuthorization
{
    Task<ProductComplianceAuthorizationResult> AuthorizeAsync(
        string operation,
        CancellationToken cancellationToken = default);
}

public sealed class DenyProductComplianceAuthorization : IProductComplianceAuthorization
{
    public Task<ProductComplianceAuthorizationResult> AuthorizeAsync(
        string operation,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ProductComplianceAuthorizationResult(
            false,
            "none",
            "Product-owned compliance governance is not available to tenant roles."));
    }
}
