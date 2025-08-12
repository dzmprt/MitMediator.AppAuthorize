using Microsoft.AspNetCore.Mvc;
using MitMediator.AppAuthorize.Domain;
using MitMediator.AppAuthorize.Web.Models.Dtos;
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
    /// <returns><see cref="WebApplication"/>.</returns>
    public static WebApplication MapJwtAuthApi(this WebApplication app)
    {
        #region Queries
        
        app.MapPost($"{Tag}", CreateJwtTokenAsync)
            .WithTags(Tag)
            .WithName("Create jwt token")
            .Produces<JwtTokenModel>();
        
        app.MapPost($"{Tag}/by-refresh", CreateJwtTokenByRefreshToken)
            .WithTags(Tag)
            .WithName("Create jwt token by refresh token")
            .Produces<JwtTokenModel>();
   
        #endregion

        return app;
    }
    
    private static async ValueTask<JwtTokenModel> CreateJwtTokenAsync(
        [FromServices] IUserAuthenticator userAuthenticator,
        [FromServices] IRefreshTokenRepository refreshTokens,
        [FromServices] CreateJwtTokenService createJwtTokenService,
        [FromServices] JwtTokenConfiguration jwtTokenConfiguration,
        [FromBody] CreateJwtCommand createJwtCommand, CancellationToken cancellationToken)
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
    
    private static async ValueTask<JwtTokenModel> CreateJwtTokenByRefreshToken(
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