namespace MitMediator.AppAuthorize;

public interface IUserAuthenticator
{
    /// <summary>
    /// Get user info by login and password.
    /// </summary>
    /// <param name="username">Username.</param>
    /// <param name="password">Password.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="UserInfo"/>.</returns>
    ValueTask<UserInfo> AuthByPasswordAsync(string username, string password, CancellationToken cancellationToken);
}