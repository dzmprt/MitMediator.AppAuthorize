namespace MitMediator.AppAuthorize;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AppAuthorizeAttribute(string[]? roles = null, string[]? tenantsIds = null) : Attribute
{
    public string[]? Roles { get; } = roles;
    
    public string[]? TenantsIds { get; } = tenantsIds;
}