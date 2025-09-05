using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MitMediator.AppAuthorize.Web.Tests;

public class CreateJwtTokenServiceTests
{
    private static JwtTokenConfiguration CreateTestConfig()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("supersecurekey12345678901234567890"));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TestIssuer",
            ValidAudience = "TestAudience",
            IssuerSigningKey = key
        };

        return new JwtTokenConfiguration(parameters, jwtTokenLifeSeconds: 600, refreshTokenLifeSeconds: 3600);
    }

    [Fact]
    public void CreateJwtToken_ShouldThrow_WhenUserIdIsNull()
    {
        // Arrange
        var config = CreateTestConfig();
        var service = new CreateJwtTokenService(config);
        var user = new UserInfo(null, null);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            service.CreateJwtToken(user, DateTime.UtcNow.AddMinutes(10)));

        Assert.Equal("UserId", ex.ParamName);
    }

    [Fact]
    public void CreateJwtToken_ShouldGenerateToken_WithMinimalClaims()
    {
        // Arrange
        var config = CreateTestConfig();
        var service = new CreateJwtTokenService(config);
        var user = new UserInfo("user123", null);

        // Act
        var token = service.CreateJwtToken(user, DateTime.UtcNow.AddMinutes(10));

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user123");
    }

    [Fact]
    public void CreateJwtToken_ShouldIncludeAllClaims_WhenProvided()
    {
        // Arrange
        var config = CreateTestConfig();
        var service = new CreateJwtTokenService(config);
        var user = new UserInfo("user123", "Dmitriy", new[] { "Admin", "User" }, new[] { "TenantA", "TenantB" });

        // Act
        var token = service.CreateJwtToken(user, DateTime.UtcNow.AddMinutes(10));

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user123");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Name && c.Value == "Dmitriy");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "User");
        Assert.Contains(jwt.Claims, c => c.Type == AppAuthorizeClaimTypes.Tenant && c.Value == "TenantA");
        Assert.Contains(jwt.Claims, c => c.Type == AppAuthorizeClaimTypes.Tenant && c.Value == "TenantB");
    }
}