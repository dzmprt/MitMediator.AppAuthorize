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
        IAuthenticationContext context = new FakeAuthenticationContext
        {
            UserId = "user",
            UserRoles = ["Admin", "User"]
        };
        Assert.True(context.IsInRole("Admin"));
    }

    [Fact]
    public void IsInRole_ShouldReturnFalse_WhenRoleDoesNotExist()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { UserRoles = new[] { "User" } };
        Assert.False(context.IsInRole("Admin"));
    }

    [Fact]
    public void IsInRole_ShouldReturnFalse_WhenRolesAreNull()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { UserRoles = null };
        Assert.False(context.IsInRole("Admin"));
    }

    [Fact]
    public void IsInOneOfRoles_ShouldReturnTrue_WhenAnyRoleMatches()
    {
        IAuthenticationContext context = new FakeAuthenticationContext
        {
            UserId = "user",
            UserRoles = ["User", "Manager"]
        };
        Assert.True(context.IsInOneOfRoles(new[] { "Admin", "Manager" }));
    }

    [Fact]
    public void IsInOneOfRoles_ShouldReturnFalse_WhenNoRolesMatch()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { UserRoles = new[] { "User" } };
        Assert.False(context.IsInOneOfRoles(new[] { "Admin", "Manager" }));
    }

    [Fact]
    public void IsInOneOfRoles_ShouldReturnFalse_WhenRolesAreNull()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { UserRoles = null };
        Assert.False(context.IsInOneOfRoles(new[] { "Admin" }));
    }

    [Fact]
    public void IsInTenant_ShouldReturnTrue_WhenTenantExists()
    {
        IAuthenticationContext context = new FakeAuthenticationContext
        {
            UserId = "user",
            TenantIds = ["T1", "T2"]
        };
        Assert.True(context.IsInTenant("T1"));
    }

    [Fact]
    public void IsInTenant_ShouldReturnFalse_WhenTenantDoesNotExist()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { TenantIds = new[] { "T2" } };
        Assert.False(context.IsInTenant("T1"));
    }

    [Fact]
    public void IsInTenant_ShouldReturnFalse_WhenTenantsAreNull()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { TenantIds = null };
        Assert.False(context.IsInTenant("T1"));
    }

    [Fact]
    public void IsInOneOfTenants_ShouldReturnTrue_WhenAnyTenantMatches()
    {
        IAuthenticationContext context = new FakeAuthenticationContext
        {
            UserId = "user",
            TenantIds = ["T1", "T2"]
        };
        Assert.True(context.IsInOneOfTenants(new[] { "T3", "T2" }));
    }

    [Fact]
    public void IsInOneOfTenants_ShouldReturnFalse_WhenNoTenantsMatch()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { TenantIds = new[] { "T1" } };
        Assert.False(context.IsInOneOfTenants(new[] { "T2", "T3" }));
    }

    [Fact]
    public void IsInOneOfTenants_ShouldReturnFalse_WhenTenantsAreNull()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { TenantIds = null };
        Assert.False(context.IsInOneOfTenants(new[] { "T1" }));
    }

    [Fact]
    public void HasClaim_ShouldReturnTrue_WhenClaimMatches()
    {
        IAuthenticationContext context = new FakeAuthenticationContext
        {
            UserId = "user",
            Claims = new Dictionary<string, IReadOnlyList<string>>
            {
                ["scope"] = ["read"]
            }
        };
        Assert.True(context.HasClaim("scope", "read"));
    }

    [Fact]
    public void HasClaim_ShouldReturnFalse_WhenClaimKeyMissing()
    {
        IAuthenticationContext context = new FakeAuthenticationContext
        {
            Claims = new Dictionary<string, IReadOnlyList<string>>
            {
                ["scope"] = ["read"]
            }
        };
        Assert.False(context.HasClaim("role", "admin"));
    }

    [Fact]
    public void HasClaim_ShouldReturnFalse_WhenClaimValueMismatch()
    {
        IAuthenticationContext context = new FakeAuthenticationContext
        {
            Claims = new Dictionary<string, IReadOnlyList<string>>
            {
                ["scope"] = ["write"]
            }
        };
        Assert.False(context.HasClaim("scope", "read"));
    }

    [Fact]
    public void HasClaim_ShouldReturnFalse_WhenClaimsAreNull()
    {
        IAuthenticationContext context = new FakeAuthenticationContext { Claims = null };
        Assert.False(context.HasClaim("scope", "read"));
    }

    [Fact]
    public void DefaultHelpers_ShouldFailClosed_WhenNotAuthenticated()
    {
        IAuthenticationContext context = new UnauthenticatedContext
        {
            UserRoles = ["Admin"],
            TenantIds = ["Tenant"],
            Claims = new Dictionary<string, IReadOnlyList<string>>
            {
                ["scope"] = ["read"]
            }
        };

        Assert.False(context.IsInRole("Admin"));
        Assert.False(context.IsInOneOfRoles(["Admin"]));
        Assert.False(context.IsInTenant("Tenant"));
        Assert.False(context.IsInOneOfTenants(["Tenant"]));
        Assert.Empty(context.GetClaimValues("scope"));
        Assert.False(context.HasClaim("scope", "read"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DefaultHelpers_ShouldFailClosed_WhenUserIdIsMissing(
        string? userId)
    {
        IAuthenticationContext context = new InvalidUserContext
        {
            UserId = userId,
            UserRoles = ["Admin"],
            TenantIds = ["Tenant"],
            Claims = new Dictionary<string, IReadOnlyList<string>>
            {
                ["scope"] = ["read"]
            }
        };

        Assert.False(context.IsInRole("Admin"));
        Assert.False(context.IsInOneOfRoles(["Admin"]));
        Assert.False(context.IsInTenant("Tenant"));
        Assert.False(context.IsInOneOfTenants(["Tenant"]));
        Assert.Empty(context.GetClaimValues("scope"));
        Assert.False(context.HasClaim("scope", "read"));
    }

    private sealed class InvalidUserContext : IAuthenticationContext
    {
        public bool IsAuthenticated => true;
        public string? UserId { get; init; }
        public string? UserName => null;
        public string[]? UserRoles { get; init; }
        public IReadOnlyDictionary<string, IReadOnlyList<string>>? Claims
        {
            get;
            init;
        }
        public string[]? TenantIds { get; init; }
    }

    private sealed class UnauthenticatedContext : IAuthenticationContext
    {
        public bool IsAuthenticated => false;
        public string? UserId => null;
        public string? UserName => null;
        public string[]? UserRoles { get; init; }
        public IReadOnlyDictionary<string, IReadOnlyList<string>>? Claims
        {
            get;
            init;
        }
        public string[]? TenantIds { get; init; }
    }
}