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

    [Theory]
    [InlineData(null, "user1")]
    [InlineData("token1", null)]
    [InlineData(null, null)]
    public void Token_WithNullFields_ShouldBeHandled(string tokenId, string userId)
    {
        // Act
        var token = new RefreshToken
        {
            RefreshTokenKey = tokenId,
            UserId = userId,
            Expired = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(tokenId, token.RefreshTokenKey);
        Assert.Equal(userId, token.UserId);
    }
}