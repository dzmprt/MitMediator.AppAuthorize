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
    public void IsAuthenticated_ShouldRequireAuthenticatedIdentity()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-id")],
                authenticationType: null))
        };
        IAuthenticationContext authenticationContext =
            new DefaultAuthenticationContext(
                new HttpContextAccessor { HttpContext = httpContext });

        Assert.False(authenticationContext.IsAuthenticated);
    }

    [Fact]
    public void Properties_ShouldNotExposeClaimsFromUnauthenticatedIdentity()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
                new Claim(ClaimTypes.Name, "User"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(AppAuthorizeClaimTypes.TenantId, "TenantA"),
                new Claim("scope", "read")
            ]))
        };
        IAuthenticationContext authenticationContext =
            new DefaultAuthenticationContext(
                new HttpContextAccessor { HttpContext = httpContext });

        Assert.Null(authenticationContext.UserId);
        Assert.Null(authenticationContext.UserName);
        Assert.Null(authenticationContext.UserRoles);
        Assert.Null(authenticationContext.Claims);
        Assert.Null(authenticationContext.TenantIds);
        Assert.Empty(((IAuthenticationContext)authenticationContext)
            .GetClaimValues("scope"));
        Assert.False(((IAuthenticationContext)authenticationContext)
            .HasClaim("scope", "read"));
    }

    [Fact]
    public void Claims_ShouldUseFirstValue_WhenClaimTypeOccursMoreThanOnce()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
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
        Assert.Equal(["Admin", "User"], claims[ClaimTypes.Role]);
        Assert.Equal(["TenantA", "TenantB"], claims[AppAuthorizeClaimTypes.TenantId]);
        Assert.Equal(["Admin", "User"], userRoles);
        Assert.Equal(["TenantA", "TenantB"], tenantIds);
        Assert.Equal(
            ["Admin", "User"],
            ((IAuthenticationContext)authenticationContext)
                .GetClaimValues(ClaimTypes.Role));
        Assert.Equal(
            ["TenantA", "TenantB"],
            ((IAuthenticationContext)authenticationContext)
                .GetClaimValues(AppAuthorizeClaimTypes.TenantId));
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
        Assert.Equal(
            ["user-id"],
            authenticationContext.Claims![ClaimTypes.NameIdentifier]);
        Assert.Empty(authenticationContext.TenantIds!);
    }

    [Fact]
    public void Properties_ShouldUseIdentityConfiguredNameAndRoleClaimTypes()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
                new Claim("custom-name", "User"),
                new Claim("custom-role", "Admin")
            ],
            "Test",
            "custom-name",
            "custom-role");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        IAuthenticationContext authenticationContext =
            new DefaultAuthenticationContext(
                new HttpContextAccessor { HttpContext = httpContext });

        Assert.Equal("User", authenticationContext.UserName);
        Assert.Equal(["Admin"], authenticationContext.UserRoles!);
    }

    [Fact]
    public void HasClaim_ShouldFindAnyRepeatedClaimValue()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
                new Claim("scope", "read"),
                new Claim("scope", "write")
            ], "Test"))
        };
        IAuthenticationContext authenticationContext =
            new DefaultAuthenticationContext(
                new HttpContextAccessor { HttpContext = httpContext });

        Assert.True(((IAuthenticationContext)authenticationContext)
            .HasClaim("scope", "write"));
    }

    [Fact]
    public void Properties_ShouldIgnoreClaimsFromUnauthenticatedIdentities()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
            [
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "authenticated-user"),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim(AppAuthorizeClaimTypes.TenantId, "TenantA")
                ], "Authenticated"),
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "unauthenticated-user"),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim(AppAuthorizeClaimTypes.TenantId, "TenantB"),
                    new Claim("scope", "admin")
                ])
            ])
        };
        IAuthenticationContext authenticationContext =
            new DefaultAuthenticationContext(
                new HttpContextAccessor { HttpContext = httpContext });
        var roles = authenticationContext.UserRoles;
        var tenantIds = authenticationContext.TenantIds;

        Assert.Equal("authenticated-user", authenticationContext.UserId);
        Assert.NotNull(roles);
        Assert.NotNull(tenantIds);
        Assert.Equal(["User"], roles);
        Assert.Equal(["TenantA"], tenantIds);
        Assert.Empty(((IAuthenticationContext)authenticationContext)
            .GetClaimValues("scope"));
        Assert.False(((IAuthenticationContext)authenticationContext).HasClaim(
            AppAuthorizeClaimTypes.TenantId,
            "TenantB"));
        Assert.False(((IAuthenticationContext)authenticationContext)
            .HasClaim(ClaimTypes.Role, "Admin"));
    }

    [Fact]
    public void Properties_ShouldFailClosed_WhenMultipleIdentitiesAreAuthenticated()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
            [
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "first-user"),
                    new Claim(ClaimTypes.Role, "User")
                ], "First"),
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "second-user"),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim(AppAuthorizeClaimTypes.TenantId, "TenantB")
                ], "Second")
            ])
        };
        IAuthenticationContext authenticationContext =
            new DefaultAuthenticationContext(
                new HttpContextAccessor { HttpContext = httpContext });

        Assert.False(authenticationContext.IsAuthenticated);
        Assert.Null(authenticationContext.UserId);
        Assert.Null(authenticationContext.UserRoles);
        Assert.Null(authenticationContext.TenantIds);
        Assert.Null(authenticationContext.Claims);
        Assert.False(authenticationContext.HasClaim(ClaimTypes.Role, "Admin"));
    }

    [Fact]
    public void Properties_ShouldFailClosed_WhenUserIdentifierIsMissing()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, "Admin")],
                    "Test"))
        };
        IAuthenticationContext authenticationContext =
            new DefaultAuthenticationContext(
                new HttpContextAccessor { HttpContext = httpContext });

        Assert.False(authenticationContext.IsAuthenticated);
        Assert.Null(authenticationContext.UserId);
        Assert.Null(authenticationContext.UserRoles);
        Assert.Null(authenticationContext.Claims);
    }

    [Fact]
    public void Properties_ShouldFailClosed_WhenUserIdentifierIsRepeated()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "user-id"),
                    new Claim(ClaimTypes.NameIdentifier, "user-id")
                ], "Test"))
        };
        IAuthenticationContext authenticationContext =
            new DefaultAuthenticationContext(
                new HttpContextAccessor { HttpContext = httpContext });

        Assert.False(authenticationContext.IsAuthenticated);
        Assert.Null(authenticationContext.UserId);
        Assert.Null(authenticationContext.Claims);
    }

    [Fact]
    public void Properties_ShouldRefresh_WhenPrincipalChanges()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "first-user")],
                    "Test"))
        };
        var authenticationContext = new DefaultAuthenticationContext(
            new HttpContextAccessor { HttpContext = httpContext });

        Assert.Equal("first-user", authenticationContext.UserId);

        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "second-user")],
                "Test"));

        Assert.Equal("second-user", authenticationContext.UserId);
    }

    [Fact]
    public void Properties_ShouldReflectClaimsAddedToSamePrincipal()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-id")],
            "Test");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        IAuthenticationContext authenticationContext =
            new DefaultAuthenticationContext(
                new HttpContextAccessor { HttpContext = httpContext });

        Assert.Empty(authenticationContext.UserRoles!);

        identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        identity.AddClaim(
            new Claim(AppAuthorizeClaimTypes.TenantId, "TenantA"));

        Assert.Equal(["Admin"], authenticationContext.UserRoles!);
        Assert.Equal(["TenantA"], authenticationContext.TenantIds!);
        Assert.True(authenticationContext.HasClaim(ClaimTypes.Role, "Admin"));
    }

    [Fact]
    public void Properties_ShouldNotExposeMutableCollections()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "user-id"),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim(AppAuthorizeClaimTypes.TenantId, "TenantA")
                ], "Test"))
        };
        var authenticationContext = new DefaultAuthenticationContext(
            new HttpContextAccessor { HttpContext = httpContext });
        var roles = authenticationContext.UserRoles!;
        var tenants = authenticationContext.TenantIds!;

        roles[0] = "User";
        tenants[0] = "TenantB";

        Assert.Equal(["Admin"], authenticationContext.UserRoles!);
        Assert.Equal(["TenantA"], authenticationContext.TenantIds!);
        Assert.Throws<NotSupportedException>(
            () => ((IList<string>)authenticationContext.Claims![
                ClaimTypes.Role]).Add("User"));
    }
}