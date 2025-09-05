namespace MitMediator.AppAuthorize;

/// <summary>
/// Authenticated user context for identity and access-related data.
/// </summary>
public interface IAuthenticationContext
{
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
    /// User flattened claims (key-value format).
    /// </summary>
    Dictionary<string, string>? Claims { get; }
    
    /// <summary>
    /// Tenant identifiers.
    /// </summary>
    string[]? TenantIds { get; }

    /// <summary>
    /// Checks whether the user has the specified role. The method is case-sensitive.
    /// </summary>
    /// <param name="role">Role to check.</param>
    /// <returns>True if the user has the specified role.</returns>
    public sealed bool IsInRole(string role) => UserRoles?.Contains(role) ?? false;
    
    /// <summary>
    /// Checks whether the user has one of the specified roles. The method is case-sensitive.
    /// </summary>
    /// <param name="roles">Roles to check.</param>
    /// <returns>True if the user has one of the specified roles.</returns>
    public sealed bool IsInOneOfRoles(string[] roles) => UserRoles?.Intersect(roles).Any() ?? false;
    
    /// <summary>
    /// Checks whether the user has the specified tenant. The method is case-sensitive.
    /// </summary>
    /// <param name="tenantId">Tenant id to check.</param>
    /// <returns>True if the user is in tenant</returns>
    public sealed bool IsInTenant(string tenantId) => TenantIds?.Contains(tenantId) ?? false;
    
    /// <summary>
    /// Checks whether the user has one of the specified tenants. The method is case-sensitive.
    /// </summary>
    /// <param name="tenantIds">Tenant ids to check.</param>
    /// <returns>True if the user has one of the specified tenants.</returns>
    public sealed bool IsInOneOfTenants(string[] tenantIds) => TenantIds?.Intersect(tenantIds).Any() ?? false;
    
    /// <summary>
    /// Checks whether the user has the specified claim with the exact value.  The method is case-sensitive.
    /// </summary>
    /// <param name="key">Claim key.</param>
    /// <param name="value">Expected claim value.</param>
    /// <returns>True if the claim exists and matches the value.</returns>
    public sealed bool HasClaim(string key, string value) => Claims?.TryGetValue(key, out var actualValue) == true && actualValue == value;

    /// <summary>
    /// Indicates whether the user is authenticated.
    /// </summary>
    /// <value>True if the user is authenticated.</value>
    public sealed bool IsAuthenticated => UserId is not null;
}