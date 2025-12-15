namespace MitMediator.AppAuthorize;

/// <summary>
/// Authenticator by login and code (from sms, email, etc).
/// </summary>
public interface IUserAuthenticatorByCode
{
    /// <summary>
    /// Get user info by login and code.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="code">Code.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="UserInfo"/>.</returns>
    ValueTask<UserInfo> AuthByCodeAsync(string username, string code, CancellationToken cancellationToken);
}