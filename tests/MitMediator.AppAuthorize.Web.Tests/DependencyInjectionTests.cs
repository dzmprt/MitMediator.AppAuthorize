using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MitMediator.AppAuthorize.Web.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddJwtAuthServices_WithKey_ShouldRegisterJwtTokenConfigurationAndTokenService()
    {
        // Arrange
        var services = new ServiceCollection();
        var key = "supersecretkey12345678901234567890";

        // Act
        services.AddJwtAuthServices(key, jwtTokenLifeSeconds: 900, refreshTokenLifeSeconds: 3600);
        var provider = services.BuildServiceProvider();

        // Assert
        var config = provider.GetService<JwtTokenConfiguration>();
        var tokenService = provider.GetService<CreateJwtTokenService>();

        Assert.NotNull(config);
        Assert.NotNull(tokenService);
        Assert.Equal(900, config.JwtTokenLifeSeconds);
        Assert.Equal(3600, config.RefreshTokenLifeSeconds);
        Assert.IsType<SymmetricSecurityKey>(config.TokenValidationParameters.IssuerSigningKey);
    }

    [Fact]
    public void AddJwtAuthServices_WithParameters_ShouldRegisterJwtTokenConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            IssuerSigningKey = new SymmetricSecurityKey("key12345678901234567890"u8.ToArray())
        };

        // Act
        services.AddJwtAuthServices(parameters, jwtTokenLifeSeconds: 1200, refreshTokenLifeSeconds: 7200);
        var provider = services.BuildServiceProvider();

        // Assert
        var config = provider.GetService<JwtTokenConfiguration>();
        Assert.NotNull(config);
        Assert.Equal(1200, config.JwtTokenLifeSeconds);
        Assert.Equal(7200, config.RefreshTokenLifeSeconds);
        Assert.Same(parameters, config.TokenValidationParameters);
    }

    [Fact]
    public void AddDefaultAuthContext_ShouldRegisterIAuthenticationContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDefaultAuthContext();
        var provider = services.BuildServiceProvider();

        // Assert
        var context = provider.GetService<IAuthenticationContext>();
        Assert.NotNull(context);
        Assert.IsType<DefaultAuthenticationContext>(context);
    }
}