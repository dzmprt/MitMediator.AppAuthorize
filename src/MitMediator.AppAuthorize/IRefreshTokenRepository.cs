using MitMediator.AppAuthorize.Domain;

namespace MitMediator.AppAuthorize;

/// <summary>
/// Refresh token repository.
/// </summary>
public interface IRefreshTokenRepository 
{
    /// <summary>
    /// Add refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token to add.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>The added refresh token.</returns>
    ValueTask<RefreshToken> AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    
    /// <summary>
    /// Delete refresh token.
    /// </summary>
    /// <param name="refreshTokenKey">Refresh token id to get.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>The refresh token object.</returns>
    ValueTask<RefreshToken?> GetOrDefaultAsync(string refreshTokenKey, CancellationToken cancellationToken);

    /// <summary>
    /// Delete refresh token.
    /// </summary>
    /// <param name="entity">Refresh token to delete.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>A task that represents the asynchronous remove operation.</returns>
    ValueTask RemoveAsync(RefreshToken entity, CancellationToken cancellationToken);

    /// <summary>
    /// Delete refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token to add.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns>The refresh token object.</returns>
    ValueTask<UserInfo?> GetUserInfoByTokenOrDefaultAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
}