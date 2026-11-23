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
        string password, CancellationToken cancellation = default)
    {
        ValidateCredentials(login, password, nameof(login), nameof(password));
        return clientMediator.SendAsync<CreateJwtByPasswordCommand, JwtTokenModel>(new CreateJwtByPasswordCommand()
        {
            Login = login,
            Password = password

        }, cancellation);
    }

    /// <summary>
    /// Get a JWT token using a custom auth route prefix.
    /// </summary>
    public static ValueTask<JwtTokenModel> GetJwtByPasswordAsync(
        this IClientMediator clientMediator,
        string login,
        string password,
        string routePrefix,
        CancellationToken cancellation = default)
    {
        ValidateCredentials(login, password, nameof(login), nameof(password));
        return clientMediator.SendAsync<CreateJwtByPasswordWithPrefixCommand, JwtTokenModel>(
            new CreateJwtByPasswordWithPrefixCommand
            {
                Key = NormalizeRoutePrefix(routePrefix),
                Login = login,
                Password = password
            },
            cancellation);
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
        string code, CancellationToken cancellation = default)
    {
        ValidateCredentials(login, code, nameof(login), nameof(code));
        return clientMediator.SendAsync<CreateJwtByCodeCommand, JwtTokenModel>(new CreateJwtByCodeCommand()
        {
            Login = login,
            Code = code

        }, cancellation);
    }

    /// <summary>
    /// Get a JWT token by code using a custom auth route prefix.
    /// </summary>
    public static ValueTask<JwtTokenModel> GetJwtByCodeAsync(
        this IClientMediator clientMediator,
        string login,
        string code,
        string routePrefix,
        CancellationToken cancellation = default)
    {
        ValidateCredentials(login, code, nameof(login), nameof(code));
        return clientMediator.SendAsync<CreateJwtByCodeWithPrefixCommand, JwtTokenModel>(
            new CreateJwtByCodeWithPrefixCommand
            {
                Key = NormalizeRoutePrefix(routePrefix),
                Login = login,
                Code = code
            },
            cancellation);
    }

    /// <summary>
    /// Get a JWT token from the server, provided the server is configured with MitMediator.AppAuthorize.Web. 
    /// </summary>
    /// <param name="clientMediator"><see cref="IClientMediator"/></param>
    /// <param name="userId">User id.</param>
    /// <param name="refreshTokenKey">Refresh token.</param>
    /// <param name="cancellation"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="JwtTokenModel"/>.</returns>
    public static ValueTask<JwtTokenModel> GetJwtByRefreshAsync(this IClientMediator clientMediator, string userId,
        string refreshTokenKey, CancellationToken cancellation = default)
    {
        ValidateCredentials(
            userId,
            refreshTokenKey,
            nameof(userId),
            nameof(refreshTokenKey));
        return clientMediator.SendAsync<CreateJwtByRefreshTokenCommand, JwtTokenModel>(new CreateJwtByRefreshTokenCommand
        {
            UserId = userId.Trim(),
            RefreshTokenKey = refreshTokenKey
        }, cancellation);
    }

    /// <summary>
    /// Refresh a JWT token using a custom auth route prefix.
    /// </summary>
    public static ValueTask<JwtTokenModel> GetJwtByRefreshAsync(
        this IClientMediator clientMediator,
        string userId,
        string refreshTokenKey,
        string routePrefix,
        CancellationToken cancellation = default)
    {
        ValidateCredentials(
            userId,
            refreshTokenKey,
            nameof(userId),
            nameof(refreshTokenKey));
        return clientMediator.SendAsync<CreateJwtByRefreshTokenWithPrefixCommand, JwtTokenModel>(
            new CreateJwtByRefreshTokenWithPrefixCommand
            {
                Key = NormalizeRoutePrefix(routePrefix),
                UserId = userId.Trim(),
                RefreshTokenKey = refreshTokenKey
            },
            cancellation);
    }

    private static void ValidateCredentials(
        string first,
        string second,
        string firstParameterName,
        string secondParameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            first,
            firstParameterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            second,
            secondParameterName);
    }

    private static string NormalizeRoutePrefix(string routePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);
        if (routePrefix.Any(char.IsControl))
        {
            throw new ArgumentException(
                "The route prefix cannot contain control characters.",
                nameof(routePrefix));
        }

        var normalized = routePrefix.Trim().Trim('/').Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException(
                "The route prefix must contain a non-separator character.",
                nameof(routePrefix));
        }

        var segments = normalized.Split('/', StringSplitOptions.None);
        if (segments.Any(string.IsNullOrEmpty) ||
            segments.Any(segment => segment is "." or "..") ||
            normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not ('-' or '.' or '_' or '~' or '/')))
        {
            throw new ArgumentException(
                "The route prefix can contain only ASCII letters, digits, '-', '.', '_', '~', and single '/' separators between non-empty segments.",
                nameof(routePrefix));
        }

        return normalized;
    }
}