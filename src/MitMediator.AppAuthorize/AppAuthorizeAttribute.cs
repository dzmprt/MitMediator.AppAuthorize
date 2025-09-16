namespace MitMediator.AppAuthorize;

/// <summary>
/// Specifies required roles and/or tenants for accessing a request.
/// </summary>
/// <param name="roles">The roles required to access the request.</param>
/// <param name="tenantsIds">The tenants identifiers required to access the request.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AppAuthorizeAttribute(string[]? roles = null, string[]? tenantsIds = null) : Attribute
{
    /// <summary>
    /// Roles required to access the request.
    /// </summary>
    public string[]? Roles { get; } = roles;

    /// <summary>
    /// Tenants identifiers required to access the request.
    /// </summary>
    public string[]? TenantsIds { get; } = tenantsIds;
}