namespace MitMediator.AppAuthorize.Domain;

/// <summary>
/// Refresh token.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Refresh token key.
    /// </summary>
    public string RefreshTokenKey { get; set; } = null!;

    /// <summary>
    /// User identifier associated with the token.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Expiration date and time of the token.
    /// </summary>
    public DateTime Expired { get; set; }
}