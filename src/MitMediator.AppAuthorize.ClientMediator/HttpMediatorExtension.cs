using MitMediator.AutoApi.HttpMediator;

namespace MitMediator.AppAuthorize.ClientMediator;

public static class ClientMediatorExtension
{
    /// <summary>
    /// Get a JWT token from the server, provided the server is configured with MitMediator.AppAuthorize.Web. 
    /// </summary>
    /// <param name="clientMediator"><see cref="IClientMediator"/></param>
    /// <param name="login">User login.</param>
    /// <param name="password">User password.</param>
    /// <param name="cancellation"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="JwtTokenModel"/>.</returns>
    public static ValueTask<JwtTokenModel> GetJwtByPasswordAsync(this IClientMediator clientMediator, string login,
        string password, CancellationToken cancellation)
    {
        return clientMediator.SendAsync<CreateJwtByPasswordCommand, JwtTokenModel>(new CreateJwtByPasswordCommand()
        {
            Login = login,
            Password = password

        }, cancellation);
    }
    
    /// <summary>
    /// Get a JWT token from the server, provided the server is configured with MitMediator.AppAuthorize.Web. 
    /// </summary>
    /// <param name="clientMediator"><see cref="IClientMediator"/></param>
    /// <param name="login">User login.</param>
    /// <param name="code">Auth code (from sms, email, etc.).</param>
    /// <param name="cancellation"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="JwtTokenModel"/>.</returns>
    public static ValueTask<JwtTokenModel> GetJwtByCodeAsync(this IClientMediator clientMediator, string login,
        string code, CancellationToken cancellation)
    {
        return clientMediator.SendAsync<CreateJwtByCodeCommand, JwtTokenModel>(new CreateJwtByCodeCommand()
        {
            Login = login,
            Code = code

        }, cancellation);
    }

    /// <summary>
    /// Get a JWT token from the server, provided the server is configured with MitMediator.AppAuthorize.Web. 
    /// </summary>
    /// <param name="clientMediator"><see cref="IClientMediator"/></param>
    /// <param name="refreshTokenKey">Refresh token.</param>
    /// <param name="userId">User id.</param>
    /// <param name="cancellation"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="JwtTokenModel"/>.</returns>
    public static ValueTask<JwtTokenModel> GetJwtByRefreshAsync(this IClientMediator clientMediator, string userId,
        string refreshTokenKey, CancellationToken cancellation)
    {
        return clientMediator.SendAsync<CreateJwtByRefreshTokenCommand, JwtTokenModel>(new CreateJwtByRefreshTokenCommand
        {
            UserId = userId,
            RefreshTokenKey = refreshTokenKey
        }, cancellation);
    }
}