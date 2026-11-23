namespace MitMediator.AppAuthorize;

/// <summary>
/// Authenticated user context for identity and access-related data.
/// </summary>
public interface IAuthenticationContext
{
    /// <summary>
    /// Indicates whether the current identity is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// User id.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Username.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// User roles.
    /// </summary>
    string[]? UserRoles { get; }

    /// <summary>
    /// User claims grouped by type, preserving repeated values.
    /// </summary>
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Claims { get; }

    /// <summary>
    /// Tenant identifiers.
    /// </summary>
    string[]? TenantIds { get; }

    /// <summary>
    /// Checks whether the user has the specified role. The method is case-sensitive.
    /// </summary>
    /// <param name="role">Role to check.</param>
    /// <returns>True if the user has the specified role.</returns>
    public sealed bool IsInRole(string role) =>
        HasAuthenticatedUser &&
        (UserRoles?.Contains(role) ?? false);

    /// <summary>
    /// Checks whether the user has one of the specified roles. The method is case-sensitive.
    /// </summary>
    /// <param name="roles">Roles to check.</param>
    /// <returns>True if the user has one of the specified roles.</returns>
    public sealed bool IsInOneOfRoles(string[] roles) =>
        HasAuthenticatedUser &&
        (UserRoles?.Intersect(roles).Any() ?? false);

    /// <summary>
    /// Checks whether the user has the specified tenant. The method is case-sensitive.
    /// </summary>
    /// <param name="tenantId">Tenant id to check.</param>
    /// <returns>True if the user is in tenant</returns>
    public sealed bool IsInTenant(string tenantId) =>
        HasAuthenticatedUser &&
        (TenantIds?.Contains(tenantId) ?? false);

    /// <summary>
    /// Checks whether the user has one of the specified tenants. The method is case-sensitive.
    /// </summary>
    /// <param name="tenantIds">Tenant ids to check.</param>
    /// <returns>True if the user has one of the specified tenants.</returns>
    public sealed bool IsInOneOfTenants(string[] tenantIds) =>
        HasAuthenticatedUser &&
        (TenantIds?.Intersect(tenantIds).Any() ?? false);

    /// <summary>
    /// Gets all values for a claim type.
    /// </summary>
    /// <param name="key">Claim key.</param>
    /// <returns>All values associated with the claim type.</returns>
    public IReadOnlyList<string> GetClaimValues(string key) =>
        HasAuthenticatedUser &&
        Claims?.TryGetValue(key, out var values) == true
            ? values
            : [];

    /// <summary>
    /// Checks whether the user has the specified claim with the exact value. The method is case-sensitive.
    /// </summary>
    /// <param name="key">Claim key.</param>
    /// <param name="value">Expected claim value.</param>
    /// <returns>True if the claim exists and matches the value.</returns>
    public bool HasClaim(string key, string value) =>
        GetClaimValues(key).Contains(value);

    private bool HasAuthenticatedUser =>
        IsAuthenticated && !string.IsNullOrWhiteSpace(UserId);
}
