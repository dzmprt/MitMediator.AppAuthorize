using MitMediator.AppAuthorize.Domain;

namespace MitMediator.AppAuthorize;

/// <summary>
/// Refresh token repository.
/// </summary>
public interface IRefreshTokenRepository 
{
    /// <summary>
    /// Atomically creates and persists a refresh token.
    /// </summary>
    /// <param name="userId">User identifier associated with the token.</param>
    /// <param name="expiresAt">Expiration date and time of the token.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>The persisted refresh token.</returns>
    ValueTask<RefreshToken> AddAsync(
        string userId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically validates and consumes a refresh token, then persists its replacement.
    /// </summary>
    /// <param name="refreshTokenKey">Refresh token key.</param>
    /// <param name="userId">Expected token owner.</param>
    /// <param name="newTokenExpiration">Expiration of the replacement token.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>The replacement token and its user, or <see langword="null"/> if the token is invalid.</returns>
    ValueTask<RefreshTokenRotation?> RotateAsync(
        string refreshTokenKey,
        string userId,
        DateTimeOffset newTokenExpiration,
        CancellationToken cancellationToken);
}