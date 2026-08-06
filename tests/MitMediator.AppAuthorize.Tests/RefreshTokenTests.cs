using MitMediator.AppAuthorize.Domain;

namespace MitMediator.AppAuthorize.Tests;

public class RefreshTokenTests
{
    [Fact]
    public void Constructor_AssignsPropertiesCorrectly()
    {
        // Arrange
        var tokenId = "token123";
        var userId = "user456";
        var expiry = DateTime.UtcNow.AddHours(1);

        // Act
        var token = new RefreshToken
        {
            RefreshTokenKey = tokenId,
            UserId = userId,
            Expired = expiry
        };

        // Assert
        Assert.Equal(tokenId, token.RefreshTokenKey);
        Assert.Equal(userId, token.UserId);
        Assert.Equal(expiry, token.Expired);
    }

    [Fact]
    public void ExpiredToken_ShouldBeInPast()
    {
        // Arrange
        var token = new RefreshToken
        {
            Expired = DateTime.UtcNow.AddMinutes(-5)
        };

        // Act
        var isExpired = token.Expired < DateTime.UtcNow;

        // Assert
        Assert.True(isExpired);
    }

    [Fact]
    public void ValidToken_ShouldNotBeExpired()
    {
        // Arrange
        var token = new RefreshToken
        {
            Expired = DateTime.UtcNow.AddMinutes(10)
        };

        // Act
        var isExpired = token.Expired < DateTime.UtcNow;

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public void UserInfo_ShouldAssignAllProperties()
    {
        var user = new UserInfo("user", "name", ["Admin"], ["Tenant"]);

        Assert.Equal("user", user.UserId);
        Assert.Equal("name", user.Name);
        Assert.NotNull(user.Roles);
        Assert.NotNull(user.Tenants);
        Assert.Equal(["Admin"], user.Roles);
        Assert.Equal(["Tenant"], user.Tenants);
    }

}