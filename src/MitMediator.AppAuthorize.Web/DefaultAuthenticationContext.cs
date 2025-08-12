using System.Security.Claims;

namespace MitMediator.AppAuthorize.Web;

public class DefaultAuthenticationContext(IHttpContextAccessor httpContextAccessor) : IAuthenticationContext
{
    public string? UserId  => httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? UserName  => httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;
    public string[]? UserRoles  => httpContextAccessor.HttpContext?.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
    public Dictionary<string, string>? Claims => httpContextAccessor.HttpContext?.User.Claims.ToDictionary(c => c.Type, c => c.Value);
    public string[]? TenantIds  => httpContextAccessor.HttpContext?.User.Claims.Where(c => c.Type == AppAuthorizeClaimTypes.Tenant).Select(c => c.Value).ToArray();
}