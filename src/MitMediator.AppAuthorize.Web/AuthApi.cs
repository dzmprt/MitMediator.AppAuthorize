using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MitMediator.AppAuthorize.Domain;
using MitMediator.AppAuthorize.Exceptions;
using MitMediator.AppAuthorize.Web.Models;
using MitMediator.AppAuthorize.Web.Models.Requests;

namespace MitMediator.AppAuthorize.Web;

/// <summary>
/// Auth api endpoints.
/// </summary>
public static class AuthApi
{
    private const string DefaultRoutePrefix = "auth";
    
    /// <summary>
    /// Use auth api endpoints.
    /// </summary>
    /// <param name="app"><see cref="WebApplication"/>.</param>
    /// <param name="usePassword">Add endpoint to create jwt token by login and password.</param>
    /// <param name="useCode">Add endpoint to create jwt token by login and code.</param>
    /// <param name="useRefreshToken">Add endpoint to create jwt token by refresh token.</param>
    /// <param name="routePrefix">Route prefix for authentication endpoints.</param>
    /// <param name="rateLimitPolicy">Optional ASP.NET Core rate-limiting policy name.</param>
    /// <returns><see cref="WebApplication"/>.</returns>
    public static WebApplication MapAuthApi(
        this WebApplication app,
        bool usePassword = true,
        bool useCode = false,
        bool useRefreshToken = true,
        string routePrefix = DefaultRoutePrefix,
        string? rateLimitPolicy = null)
    {
        MapAuthApi(
            (IEndpointRouteBuilder)app,
            usePassword,
            useCode,
            useRefreshToken,
            routePrefix,
            rateLimitPolicy);
        return app;
    }

    /// <summary>
    /// Use auth api endpoints.
    /// </summary>
    /// <param name="app"><see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="usePassword">Add endpoint to create jwt token by login and password.</param>
    /// <param name="useCode">Add endpoint to create jwt token by login and code.</param>
    /// <param name="useRefreshToken">Add endpoint to create jwt token by refresh token.</param>
    /// <param name="routePrefix">Route prefix for authentication endpoints.</param>
    /// <param name="rateLimitPolicy">Optional ASP.NET Core rate-limiting policy name.</param>
    /// <returns><see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapAuthApi(
        this IEndpointRouteBuilder app,
        bool usePassword = true,
        bool useCode = false,
        bool useRefreshToken = true,
        string routePrefix = DefaultRoutePrefix,
        string? rateLimitPolicy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);
        if (routePrefix.Any(char.IsControl))
        {
            throw new ArgumentException(
                "The route prefix cannot contain control characters.",
                nameof(routePrefix));
        }

        if (rateLimitPolicy is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rateLimitPolicy);
        }

        var prefix = routePrefix.Trim().Trim('/').Trim();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException(
                "The route prefix must contain a non-separator character.",
                nameof(routePrefix));
        }

        ValidateRoutePrefix(prefix);

        if (rateLimitPolicy is null &&
            (usePassword || useCode || useRefreshToken))
        {
            app.ServiceProvider
                .GetService<ILoggerFactory>()?
                .CreateLogger(typeof(AuthApi).FullName!)
                .LogWarning(
                    "Authentication endpoints are mapped without a rate-limiting policy. Configure and pass rateLimitPolicy for production.");
        }

        if (usePassword)
        {
            RouteHandlerBuilder endpoint;
            if (useRefreshToken)
            {
                endpoint = app.MapPost(
                    $"{prefix}/by-password",
                    (
                        [FromServices] IUserAuthenticator userAuthenticator,
                        [FromServices] IRefreshTokenRepository refreshTokens,
                        [FromServices] CreateJwtTokenService createJwtTokenService,
                        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
                        [FromServices] AuthInputLimits inputLimits,
                        [FromBody] CreateJwtByPasswordCommand command,
                        CancellationToken cancellationToken) =>
                        CreateJwtByPasswordAsync(
                            userAuthenticator,
                            refreshTokens,
                            createJwtTokenService,
                            jwtTokenConfiguration,
                            inputLimits,
                            command,
                            cancellationToken));
                endpoint.Accepts<CreateJwtByPasswordCommand>("application/json");
            }
            else
            {
                endpoint = app.MapPost(
                    $"{prefix}/by-password",
                    (
                        [FromServices] IUserAuthenticator userAuthenticator,
                        [FromServices] CreateJwtTokenService createJwtTokenService,
                        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
                        [FromServices] AuthInputLimits inputLimits,
                        [FromBody] CreateJwtByPasswordCommand command,
                        CancellationToken cancellationToken) =>
                        CreateJwtByPasswordAsync(
                            userAuthenticator,
                            null,
                            createJwtTokenService,
                            jwtTokenConfiguration,
                            inputLimits,
                            command,
                            cancellationToken));
                endpoint.Accepts<CreateJwtByPasswordCommand>("application/json");
            }

            ConfigureAuthEndpoint(endpoint, app.ServiceProvider, prefix, rateLimitPolicy);
        }

        if (useCode)
        {
            RouteHandlerBuilder endpoint;
            if (useRefreshToken)
            {
                endpoint = app.MapPost(
                    $"{prefix}/by-code",
                    (
                        [FromServices] IUserAuthenticatorByCode userAuthenticator,
                        [FromServices] IRefreshTokenRepository refreshTokens,
                        [FromServices] CreateJwtTokenService createJwtTokenService,
                        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
                        [FromServices] AuthInputLimits inputLimits,
                        [FromBody] CreateJwtByCodeCommand command,
                        CancellationToken cancellationToken) =>
                        CreateJwtByCodeAsync(
                            userAuthenticator,
                            refreshTokens,
                            createJwtTokenService,
                            jwtTokenConfiguration,
                            inputLimits,
                            command,
                            cancellationToken));
                endpoint.Accepts<CreateJwtByCodeCommand>("application/json");
            }
            else
            {
                endpoint = app.MapPost(
                    $"{prefix}/by-code",
                    (
                        [FromServices] IUserAuthenticatorByCode userAuthenticator,
                        [FromServices] CreateJwtTokenService createJwtTokenService,
                        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
                        [FromServices] AuthInputLimits inputLimits,
                        [FromBody] CreateJwtByCodeCommand command,
                        CancellationToken cancellationToken) =>
                        CreateJwtByCodeAsync(
                            userAuthenticator,
                            null,
                            createJwtTokenService,
                            jwtTokenConfiguration,
                            inputLimits,
                            command,
                            cancellationToken));
                endpoint.Accepts<CreateJwtByCodeCommand>("application/json");
            }

            ConfigureAuthEndpoint(endpoint, app.ServiceProvider, prefix, rateLimitPolicy);
        }

        if (useRefreshToken)
        {
            var endpoint = app.MapPost(
                $"{prefix}/by-refresh",
                CreateJwtByRefreshToken);
            endpoint.Accepts<CreateJwtByRefreshTokenCommand>(
                "application/json");
            ConfigureAuthEndpoint(endpoint, app.ServiceProvider, prefix, rateLimitPolicy);
        }

        return app;
    }
    
    private static async ValueTask<IResult> CreateJwtByPasswordAsync(
        [FromServices] IUserAuthenticator userAuthenticator,
        IRefreshTokenRepository? refreshTokens,
        [FromServices] CreateJwtTokenService createJwtTokenService,
        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
        AuthInputLimits inputLimits,
        [FromBody] CreateJwtByPasswordCommand createJwtCommand,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(createJwtCommand.Login) ||
            string.IsNullOrWhiteSpace(createJwtCommand.Password))
        {
            return Results.BadRequest(new
            {
                Error = "Login and password are required."
            });
        }

        if (createJwtCommand.Login.Length > inputLimits.LoginLength ||
            createJwtCommand.Password.Length > inputLimits.PasswordLength)
        {
            return Results.BadRequest();
        }

        UserInfo? userInfo;
        try
        {
            userInfo = await userAuthenticator.AuthByPasswordAsync(
                createJwtCommand.Login,
                createJwtCommand.Password,
                cancellationToken);
        }
        catch (ForbiddenException)
        {
            throw new UnauthorizedException();
        }

        if (userInfo is null)
        {
            throw new UnauthorizedException();
        }

        return await CreateJwtAsync(
            userInfo,
            refreshTokens,
            createJwtTokenService,
            jwtTokenConfiguration,
            cancellationToken);
    }
    
    private static async ValueTask<IResult> CreateJwtByCodeAsync(
        [FromServices] IUserAuthenticatorByCode userAuthenticatorByCode,
        IRefreshTokenRepository? refreshTokens,
        [FromServices] CreateJwtTokenService createJwtTokenService,
        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
        AuthInputLimits inputLimits,
        [FromBody] CreateJwtByCodeCommand createJwtCommand,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(createJwtCommand.Login) ||
            string.IsNullOrWhiteSpace(createJwtCommand.Code))
        {
            return Results.BadRequest(new
            {
                Error = "Login and code are required."
            });
        }

        if (createJwtCommand.Login.Length > inputLimits.LoginLength ||
            createJwtCommand.Code.Length > inputLimits.CodeLength)
        {
            return Results.BadRequest();
        }

        UserInfo? userInfo;
        try
        {
            userInfo = await userAuthenticatorByCode.AuthByCodeAsync(
                createJwtCommand.Login,
                createJwtCommand.Code,
                cancellationToken);
        }
        catch (ForbiddenException)
        {
            throw new UnauthorizedException();
        }

        if (userInfo is null)
        {
            throw new UnauthorizedException();
        }

        return await CreateJwtAsync(
            userInfo,
            refreshTokens,
            createJwtTokenService,
            jwtTokenConfiguration,
            cancellationToken);
    }

    private static async ValueTask<IResult> CreateJwtAsync(
        UserInfo userInfo,
        IRefreshTokenRepository? refreshTokens,
        CreateJwtTokenService createJwtTokenService,
        JwtTokenConfiguration jwtTokenConfiguration,
        CancellationToken cancellationToken)
    {
        if (refreshTokens is null)
        {
            var jwtTokenDateExpires = DateTimeOffset.UtcNow
                .AddSeconds(jwtTokenConfiguration.JwtTokenLifeSeconds);
            var token = createJwtTokenService.CreateJwtToken(
                userInfo,
                jwtTokenDateExpires);
            return Results.Ok(new JwtTokenModel(token));
        }

        var refreshTokenDateExpires = DateTimeOffset.UtcNow
            .AddSeconds(jwtTokenConfiguration.RefreshTokenLifeSeconds);
        var refreshToken = await refreshTokens.AddAsync(
            userInfo.UserId,
            refreshTokenDateExpires,
            cancellationToken);
        var tokenWithRefreshExpiration = DateTimeOffset.UtcNow
            .AddSeconds(jwtTokenConfiguration.JwtTokenLifeSeconds);
        var tokenWithRefresh = createJwtTokenService.CreateJwtToken(
            userInfo,
            tokenWithRefreshExpiration);

        return Results.Ok(
            new JwtTokenModel(
                tokenWithRefresh,
                refreshToken.RefreshTokenKey,
                refreshToken.ExpiresAt));
    }
    
    private static async ValueTask<IResult> CreateJwtByRefreshToken(
        [FromServices] IRefreshTokenRepository refreshTokens,
        [FromServices] CreateJwtTokenService createJwtTokenService,
        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
        [FromServices] AuthInputLimits inputLimits,
        [FromBody] CreateJwtByRefreshTokenCommand createJwtRefreshTokenCommand,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
                createJwtRefreshTokenCommand.RefreshTokenKey) ||
            string.IsNullOrWhiteSpace(createJwtRefreshTokenCommand.UserId))
        {
            return Results.BadRequest(new
            {
                Error = "Refresh token and user id are required."
            });
        }

        if (createJwtRefreshTokenCommand.RefreshTokenKey.Length >
                inputLimits.RefreshTokenLength ||
            createJwtRefreshTokenCommand.UserId.Length >
                inputLimits.UserIdLength)
        {
            return Results.BadRequest();
        }

        var now = DateTimeOffset.UtcNow;
        var rotation = await refreshTokens.RotateAsync(
            createJwtRefreshTokenCommand.RefreshTokenKey,
            createJwtRefreshTokenCommand.UserId.Trim(),
            now.AddSeconds(
                jwtTokenConfiguration.RefreshTokenLifeSeconds),
            cancellationToken);
        if (rotation is null)
        {
            throw new UnauthorizedException();
        }

        var jwtTokenDateExpires = DateTimeOffset.UtcNow
            .AddSeconds(jwtTokenConfiguration.JwtTokenLifeSeconds);
        var token = createJwtTokenService.CreateJwtToken(
            rotation.User,
            jwtTokenDateExpires);

        return Results.Ok(
            new JwtTokenModel(
                token,
                rotation.RefreshToken.RefreshTokenKey,
                rotation.RefreshToken.ExpiresAt));
    }

    private static RouteHandlerBuilder DisableResponseCaching(
        this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(
            async (context, next) =>
            {
                var headers = context.HttpContext.Response.Headers;
                headers.CacheControl = "no-store, no-cache";
                headers.Pragma = "no-cache";
                headers.Expires = "0";
                return await next(context);
            });

    private static void ConfigureAuthEndpoint(
        RouteHandlerBuilder endpoint,
        IServiceProvider services,
        string prefix,
        string? rateLimitPolicy)
    {
        endpoint
            .AllowAnonymous()
            .WithTags(prefix)
            .Produces<JwtTokenModel>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .DisableResponseCaching();

        if (rateLimitPolicy is not null)
        {
            var options = services.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
            endpoint
                .RequireRateLimiting(rateLimitPolicy)
                .Produces(options.RejectionStatusCode);
        }
    }

    private static void ValidateRoutePrefix(string routePrefix)
    {
        var segments = routePrefix.Split('/', StringSplitOptions.None);
        if (segments.Any(string.IsNullOrEmpty) ||
            segments.Any(segment => segment is "." or "..") ||
            routePrefix.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not ('-' or '.' or '_' or '~' or '/')))
        {
            throw new ArgumentException(
                "The route prefix can contain only ASCII letters, digits, '-', '.', '_', '~', and single '/' separators between non-empty segments.",
                nameof(routePrefix));
        }
    }

}