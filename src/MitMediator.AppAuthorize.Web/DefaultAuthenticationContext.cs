using System.Security.Claims;

namespace MitMediator.AppAuthorize.Web;

/// <summary>
/// Default implementation of <see cref="IAuthenticationContext"/> that retrieves user information from the current HTTP context.
/// </summary>
public class DefaultAuthenticationContext(IHttpContextAccessor httpContextAccessor) : IAuthenticationContext
{
    /// <inheritdoc />
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <inheritdoc />
    public string? UserName => httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

    /// <inheritdoc />
    public string[]? UserRoles => httpContextAccessor.HttpContext?.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();

    /// <inheritdoc />
    public Dictionary<string, string>? Claims => httpContextAccessor.HttpContext?.User.Claims.ToDictionary(c => c.Type, c => c.Value);

    /// <inheritdoc />
    public string[]? TenantIds => httpContextAccessor.HttpContext?.User.Claims.Where(c => c.Type == AppAuthorizeClaimTypes.Tenant).Select(c => c.Value).ToArray();
}