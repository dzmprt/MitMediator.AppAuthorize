namespace MitMediator.AppAuthorize.Web;

/// <summary>
/// Maximum lengths accepted by authentication endpoints.
/// </summary>
public sealed class AuthInputLimits
{
    public const int DefaultLoginLength = 256;
    public const int DefaultPasswordLength = 1024;
    public const int DefaultCodeLength = 1024;
    public const int DefaultUserIdLength = 256;
    public const int DefaultRefreshTokenLength = 4096;

    /// <summary>
    /// Initializes authentication input limits.
    /// </summary>
    public AuthInputLimits(
        int loginLength = DefaultLoginLength,
        int passwordLength = DefaultPasswordLength,
        int codeLength = DefaultCodeLength,
        int userIdLength = DefaultUserIdLength,
        int refreshTokenLength = DefaultRefreshTokenLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(loginLength, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(passwordLength, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(codeLength, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(userIdLength, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            refreshTokenLength,
            0);

        LoginLength = loginLength;
        PasswordLength = passwordLength;
        CodeLength = codeLength;
        UserIdLength = userIdLength;
        RefreshTokenLength = refreshTokenLength;
    }

    /// <summary>
    /// Maximum login length.
    /// </summary>
    public int LoginLength { get; }

    /// <summary>
    /// Maximum password length.
    /// </summary>
    public int PasswordLength { get; }

    /// <summary>
    /// Maximum authentication code length.
    /// </summary>
    public int CodeLength { get; }

    /// <summary>
    /// Maximum user identifier length.
    /// </summary>
    public int UserIdLength { get; }

    /// <summary>
    /// Maximum refresh-token key length.
    /// </summary>
    public int RefreshTokenLength { get; }

}
