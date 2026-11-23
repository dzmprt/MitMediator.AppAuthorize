namespace MitMediator.AppAuthorize.Domain;

/// <summary>
/// Result of an atomic refresh-token rotation.
/// </summary>
public sealed record RefreshTokenRotation(
    UserInfo User,
    RefreshToken RefreshToken);
