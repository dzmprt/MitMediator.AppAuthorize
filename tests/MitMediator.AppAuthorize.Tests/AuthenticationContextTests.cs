namespace MitMediator.AppAuthorize.Tests;

using System.Collections.Generic;
using Xunit;

public class AuthenticationContextTests
{
    [Fact]
    public void IsAuthenticated_ShouldReturnTrue_WhenUserIdIsNotNull()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { UserId = "user123" };
        Assert.True(context.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ShouldReturnFalse_WhenUserIdIsNull()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { UserId = null };
        Assert.False(context.IsAuthenticated);
    }

    [Fact]
    public void IsInRole_ShouldReturnTrue_WhenRoleExists()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { UserRoles = new[] { "Admin", "User" } };
        Assert.True(context.IsInRole("Admin"));
    }

    [Fact]
    public void IsInRole_ShouldReturnFalse_WhenRoleDoesNotExist()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { UserRoles = new[] { "User" } };
        Assert.False(context.IsInRole("Admin"));
    }

    [Fact]
    public void IsInOneOfRoles_ShouldReturnTrue_WhenAnyRoleMatches()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { UserRoles = new[] { "User", "Manager" } };
        Assert.True(context.IsInOneOfRoles(new[] { "Admin", "Manager" }));
    }

    [Fact]
    public void IsInOneOfRoles_ShouldReturnFalse_WhenNoRolesMatch()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { UserRoles = new[] { "User" } };
        Assert.False(context.IsInOneOfRoles(new[] { "Admin", "Manager" }));
    }

    [Fact]
    public void IsInTenant_ShouldReturnTrue_WhenTenantExists()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { TenantIds = new[] { "T1", "T2" } };
        Assert.True(context.IsInTenant("T1"));
    }

    [Fact]
    public void IsInTenant_ShouldReturnFalse_WhenTenantDoesNotExist()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { TenantIds = new[] { "T2" } };
        Assert.False(context.IsInTenant("T1"));
    }

    [Fact]
    public void IsInOneOfTenants_ShouldReturnTrue_WhenAnyTenantMatches()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { TenantIds = new[] { "T1", "T2" } };
        Assert.True(context.IsInOneOfTenants(new[] { "T3", "T2" }));
    }

    [Fact]
    public void IsInOneOfTenants_ShouldReturnFalse_WhenNoTenantsMatch()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { TenantIds = new[] { "T1" } };
        Assert.False(context.IsInOneOfTenants(new[] { "T2", "T3" }));
    }

    [Fact]
    public void HasClaim_ShouldReturnTrue_WhenClaimMatches()
    {
        IAuthenticationContext context = new FakeAuthenticationContext
        {
            Claims = new Dictionary<string, string> { ["scope"] = "read" }
        };
        Assert.True(context.HasClaim("scope", "read"));
    }

    [Fact]
    public void HasClaim_ShouldReturnFalse_WhenClaimKeyMissing()
    {
        IAuthenticationContext context = new FakeAuthenticationContext
        {
            Claims = new Dictionary<string, string> { ["scope"] = "read" }
        };
        Assert.False(context.HasClaim("role", "admin"));
    }

    [Fact]
    public void HasClaim_ShouldReturnFalse_WhenClaimValueMismatch()
    {
        IAuthenticationContext context = new FakeAuthenticationContext
        {
            Claims = new Dictionary<string, string> { ["scope"] = "write" }
        };
        Assert.False(context.HasClaim("scope", "read"));
    }
}