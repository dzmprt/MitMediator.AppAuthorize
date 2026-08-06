using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace MitMediator.AppAuthorize.Web.Tests;

public class DefaultAuthenticationContextTests
{
    [Fact]
    public void Properties_ShouldReturnNull_WhenHttpContextIsMissing()
    {
        var authenticationContext = new DefaultAuthenticationContext(new HttpContextAccessor());

        Assert.Null(authenticationContext.UserId);
        Assert.Null(authenticationContext.UserName);
        Assert.Null(authenticationContext.UserRoles);
        Assert.Null(authenticationContext.Claims);
        Assert.Null(authenticationContext.TenantIds);
    }

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

    [Fact]
    public void Properties_ShouldReadIdentityClaims()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
                new Claim(ClaimTypes.Name, "User")
            ], "Test"))
        };
        var authenticationContext = new DefaultAuthenticationContext(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Equal("user-id", authenticationContext.UserId);
        Assert.Equal("User", authenticationContext.UserName);
        Assert.Empty(authenticationContext.UserRoles!);
        Assert.Equal("user-id", authenticationContext.Claims![ClaimTypes.NameIdentifier]);
        Assert.Empty(authenticationContext.TenantIds!);
    }
}