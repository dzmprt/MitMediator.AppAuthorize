using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MitMediator.AppAuthorize.Web;

/// <summary>
/// Dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add Auth services.
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <param name="issuerSigningKey">SecurityKey that is to be used for signature validation.</param>
    /// <param name="jwtTokenLifeSeconds">Jwt token lifetime in seconds.</param>
    /// <param name="refreshTokenLifeSeconds">Refresh token lifetime in seconds.</param>
    /// <returns><see cref="IServiceProvider"/>.</returns>
    public static IServiceCollection AddJwtAuthServices(this IServiceCollection services, 
        string issuerSigningKey, 
        int jwtTokenLifeSeconds = 600, 
        int refreshTokenLifeSeconds = 2592000)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = AppDomain.CurrentDomain.FriendlyName,
            ValidAudience = AppDomain.CurrentDomain.FriendlyName,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(issuerSigningKey))
        };
        services.AddAuthorization()
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;
            });

        services.AddSingleton<CreateJwtTokenService>();
        services.AddSingleton<JwtTokenConfiguration>(_ => new JwtTokenConfiguration(tokenValidationParameters, jwtTokenLifeSeconds, refreshTokenLifeSeconds));
        
        return services;
    }

    /// <summary>
    /// Add Auth services.
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <param name="tokenValidationParameters">Parameters used to validate identity tokens.</param>
    /// <param name="jwtTokenLifeSeconds">Jwt token lifetime in seconds.</param>
    /// <param name="refreshTokenLifeSeconds">Refresh token lifetime in seconds.</param>
    /// <returns><see cref="IServiceProvider"/>.</returns>
    public static IServiceCollection AddJwtAuthServices(this IServiceCollection services, 
        TokenValidationParameters tokenValidationParameters, 
        int jwtTokenLifeSeconds = 600, 
        int refreshTokenLifeSeconds = 2592000)
    {
        services.AddAuthorization()
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = tokenValidationParameters;
            });
        
        services.AddSingleton<JwtTokenConfiguration>(_ => new JwtTokenConfiguration(tokenValidationParameters, jwtTokenLifeSeconds, refreshTokenLifeSeconds));
        
        return services;
    }

    /// <summary>
    /// Use default authentication context <see cref="DefaultAuthenticationContext"/> and add HttpContext accessor.
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <returns><see cref="IServiceProvider"/>.</returns>
    public static IServiceCollection AddDefaultAuthContext(this IServiceCollection services)
    {
        return services
            .AddHttpContextAccessor()
            .AddTransient<IAuthenticationContext, DefaultAuthenticationContext>();
    }
}