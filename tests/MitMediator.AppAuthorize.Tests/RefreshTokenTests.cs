using MitMediator.AppAuthorize.Domain;

namespace MitMediator.AppAuthorize.Tests;

public class RefreshTokenTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldRejectInvalidRefreshTokenKey(
        string? refreshTokenKey)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new RefreshToken(
                refreshTokenKey!,
                "user-id",
                DateTimeOffset.UtcNow.AddHours(1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldRejectInvalidUserId(string? userId)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new RefreshToken(
                "refresh-token",
                userId!,
                DateTimeOffset.UtcNow.AddHours(1)));
    }

    [Fact]
    public void RefreshToken_PropertiesShouldBeImmutable()
    {
        var properties = typeof(RefreshToken).GetProperties();

        Assert.All(
            properties,
            property => Assert.Null(property.SetMethod));
    }

    [Fact]
    public void UserInfo_ShouldNormalizeIdentifiersAndNames()
    {
        var user = new UserInfo(" user-id ", " User ");

        Assert.Equal("user-id", user.UserId);
        Assert.Equal("User", user.Name);
    }

    [Fact]
    public void RefreshToken_ShouldNormalizeUserIdAndPreserveKeyAndExpiration()
    {
        var expiresAt = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var token = new RefreshToken(
            " opaque-key ",
            " user-id ",
            expiresAt);

        Assert.Equal("user-id", token.UserId);
        Assert.Equal(" opaque-key ", token.RefreshTokenKey);
        Assert.Equal(expiresAt, token.ExpiresAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UserInfo_ShouldRejectEmptyUserId(string? userId)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new UserInfo(userId!, "name"));
    }

    [Fact]
    public void UserInfo_ShouldNormalizeRolesAndTenants()
    {
        var user = new UserInfo(
            "user",
            "name",
            [" Admin ", "Admin", "", " "],
            [" Tenant ", "Tenant", "", " "]);

        Assert.Equal(["Admin"], user.Roles!);
        Assert.Equal(["Tenant"], user.Tenants!);
    }

    [Fact]
    public void UserInfo_ShouldExposeReadOnlyRolesAndTenants()
    {
        var roles = new[] { "Admin" };
        var tenants = new[] { "Tenant" };
        var user = new UserInfo("user", "name", roles, tenants);

        roles[0] = "Changed";
        tenants[0] = "Changed";

        Assert.Equal(["Admin"], user.Roles!);
        Assert.Equal(["Tenant"], user.Tenants!);
        Assert.Throws<NotSupportedException>(
            () => ((IList<string>)user.Roles!).Add("User"));
        Assert.Throws<NotSupportedException>(
            () => ((IList<string>)user.Tenants!).Add("Other"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Admin, ")]
    [InlineData("Admin,Admin")]
    public void AppAuthorize_ShouldRejectInvalidRoles(string value)
    {
        var roles = value.Split(',', StringSplitOptions.None);

        Assert.ThrowsAny<ArgumentException>(
            () => new AppAuthorizeAttribute(roles));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("TenantA, ")]
    [InlineData("TenantA,TenantA")]
    public void AppAuthorize_ShouldRejectInvalidTenants(string value)
    {
        var tenants = value.Split(',', StringSplitOptions.None);

        Assert.ThrowsAny<ArgumentException>(
            () => new AppAuthorizeAttribute(tenantsIds: tenants));
    }

    [Fact]
    public void AppAuthorize_ShouldTrimValues()
    {
        var attribute =
            new AppAuthorizeAttribute([" Admin "], [" TenantA "]);

        Assert.Equal(["Admin"], attribute.Roles!);
        Assert.Equal(["TenantA"], attribute.TenantsIds!);
    }
}