namespace MitMediator.AppAuthorize.Domain;

/// <summary>
/// Refresh token.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// Initializes an immutable refresh token.
    /// </summary>
    /// <param name="refreshTokenKey">Refresh token key.</param>
    /// <param name="userId">User identifier associated with the token.</param>
    /// <param name="expiresAt">Expiration date and time of the token.</param>
    public RefreshToken(
        string refreshTokenKey,
        string userId,
        DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        RefreshTokenKey = refreshTokenKey;
        UserId = userId.Trim();
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Refresh token key.
    /// </summary>
    public string RefreshTokenKey { get; }

    /// <summary>
    /// User identifier associated with the token.
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Expiration date and time of the token.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; }
}
