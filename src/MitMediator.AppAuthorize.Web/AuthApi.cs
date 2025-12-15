using Microsoft.AspNetCore.Mvc;
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
    private const string Tag = "auth";
    
    /// <summary>
    /// Use auth api endpoints.
    /// </summary>
    /// <param name="app"><see cref="WebApplication"/>.</param>
    /// <param name="usePassword">Add endpoint to create jwt token by login and password.</param>
    /// <param name="useCode">Add endpoint to create jwt token by login and code.</param>
    /// <param name="useRefreshToken">Add endpoint to create jwt token by refresh token.</param>
    /// <returns><see cref="WebApplication"/>.</returns>
    public static WebApplication MapAuthApi(this WebApplication app, bool usePassword = true, bool useCode = false, bool useRefreshToken = true)
    {
        #region Queries

        if (usePassword)
        {
            app.MapPost($"{Tag}/by-password", CreateJwtByPasswordAsync)
                .WithTags(Tag)
                .WithName("Create jwt token by login and password.")
                .Produces<JwtTokenModel>();   
        }

        if (useCode)
        {
            app.MapPost($"{Tag}/by-code", CreateJwtByCodeAsync)
                .WithTags(Tag)
                .WithName("Create jwt token by login and code.")
                .Produces<JwtTokenModel>();   
        }

        if (useRefreshToken)
        {
            app.MapPost($"{Tag}/by-refresh", CreateJwtByRefreshToken)
                .WithTags(Tag)
                .WithName("Create jwt token by refresh token")
                .Produces<JwtTokenModel>();   
        }
   
        #endregion

        return app;
    }
    
    private static async ValueTask<JwtTokenModel> CreateJwtByPasswordAsync(
        [FromServices] IUserAuthenticator userAuthenticator,
        [FromServices] IRefreshTokenRepository refreshTokens,
        [FromServices] CreateJwtTokenService createJwtTokenService,
        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
        [FromBody] CreateJwtByPasswordCommand createJwtCommand, CancellationToken cancellationToken)
    {
        var userInfo = await userAuthenticator.AuthByPasswordAsync(createJwtCommand.Login, createJwtCommand.Password, cancellationToken);

        if (userInfo is null)
        {
            throw new ForbiddenException();
        }

        var jwtTokenDateExpires = DateTime.UtcNow.AddSeconds(jwtTokenConfiguration.JwtTokenLifeSeconds);
        var refreshTokenDateExpires = DateTime.UtcNow.AddSeconds(jwtTokenConfiguration.RefreshTokenLifeSeconds);
        var token = createJwtTokenService.CreateJwtToken(userInfo, jwtTokenDateExpires);
        var refreshToken = await refreshTokens.AddAsync(new RefreshToken { UserId = userInfo.UserId, Expired = refreshTokenDateExpires }, cancellationToken);

        return new JwtTokenModel
        {
            JwtToken = token,
            RefreshToken = refreshToken.RefreshTokenKey,
            RefreshTokenExpires = refreshToken.Expired
        };
    }
    
    private static async ValueTask<JwtTokenModel> CreateJwtByCodeAsync(
        [FromServices] IUserAuthenticatorByCode userAuthenticatorByCode,
        [FromServices] IRefreshTokenRepository refreshTokens,
        [FromServices] CreateJwtTokenService createJwtTokenService,
        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
        [FromBody] CreateJwtByCodeCommand createJwtCommand, CancellationToken cancellationToken)
    {
        var userInfo = await userAuthenticatorByCode.AuthByCodeAsync(createJwtCommand.Login, createJwtCommand.Code, cancellationToken);

        if (userInfo is null)
        {
            throw new ForbiddenException();
        }

        var jwtTokenDateExpires = DateTime.UtcNow.AddSeconds(jwtTokenConfiguration.JwtTokenLifeSeconds);
        var refreshTokenDateExpires = DateTime.UtcNow.AddSeconds(jwtTokenConfiguration.RefreshTokenLifeSeconds);
        var token = createJwtTokenService.CreateJwtToken(userInfo, jwtTokenDateExpires);
        var refreshToken = await refreshTokens.AddAsync(new RefreshToken { UserId = userInfo.UserId, Expired = refreshTokenDateExpires }, cancellationToken);

        return new JwtTokenModel
        {
            JwtToken = token,
            RefreshToken = refreshToken.RefreshTokenKey,
            RefreshTokenExpires = refreshToken.Expired
        };
    }
    
    private static async ValueTask<JwtTokenModel> CreateJwtByRefreshToken(
        [FromServices] IUserAuthenticator userAuthenticator,
        [FromServices] IRefreshTokenRepository refreshTokens,
        [FromServices] CreateJwtTokenService createJwtTokenService,
        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
        [FromBody] CreateJwtByRefreshTokenCommand createJwtRefreshTokenCommand, 
        CancellationToken cancellationToken)
    {
        var refreshTokenFormDb =
            await refreshTokens.GetOrDefaultAsync(createJwtRefreshTokenCommand.RefreshTokenKey, cancellationToken);
        if (refreshTokenFormDb is null)
        {
            throw new ForbiddenException();
        }

        if (refreshTokenFormDb.Expired < DateTime.UtcNow)
        {
            await refreshTokens.RemoveAsync(refreshTokenFormDb, cancellationToken);
            throw new ForbiddenException();
        }

        var jwtTokenDateExpires = DateTime.UtcNow.AddSeconds(jwtTokenConfiguration.JwtTokenLifeSeconds);
        var refreshTokenDateExpires = DateTime.UtcNow.AddSeconds(jwtTokenConfiguration.RefreshTokenLifeSeconds);

        var userInfo =
            await refreshTokens.GetUserInfoByTokenOrDefaultAsync(refreshTokenFormDb,
                cancellationToken);

        if (userInfo is null)
        {
            throw new ForbiddenException();
        }
        
        var jwtToken = createJwtTokenService.CreateJwtToken(userInfo, jwtTokenDateExpires);

        await refreshTokens.RemoveAsync(refreshTokenFormDb, cancellationToken);
        var newRefreshToken = await refreshTokens.AddAsync(
            new RefreshToken { UserId = userInfo.UserId, Expired = refreshTokenDateExpires }, cancellationToken);

        return new JwtTokenModel
        {
            JwtToken = jwtToken,
            RefreshToken = newRefreshToken.RefreshTokenKey,
            RefreshTokenExpires = newRefreshToken.Expired
        };
    }
}