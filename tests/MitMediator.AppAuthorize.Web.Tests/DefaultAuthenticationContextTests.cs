using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MitMediator.AppAuthorize.Web.Tests;

public class DefaultAuthenticationContextTests
{
    [Fact]
    public void Claims_ShouldUseFirstValue_WhenClaimTypeOccursMoreThanOnce()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User"),
                new Claim(AppAuthorizeClaimTypes.TenantId, "TenantA"),
                new Claim(AppAuthorizeClaimTypes.TenantId, "TenantB")
            ], "Test"))
        };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var authenticationContext = new DefaultAuthenticationContext(accessor);

        var claims = authenticationContext.Claims;
        var userRoles = authenticationContext.UserRoles;
        var tenantIds = authenticationContext.TenantIds;

        Assert.NotNull(claims);
        Assert.NotNull(userRoles);
        Assert.NotNull(tenantIds);
        Assert.Equal("Admin", claims[ClaimTypes.Role]);
        Assert.Equal("TenantA", claims[AppAuthorizeClaimTypes.TenantId]);
        Assert.Equal(["Admin", "User"], userRoles);
        Assert.Equal(["TenantA", "TenantB"], tenantIds);
    }
}