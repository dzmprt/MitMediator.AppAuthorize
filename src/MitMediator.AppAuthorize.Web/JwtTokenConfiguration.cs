using Microsoft.IdentityModel.Tokens;

namespace MitMediator.AppAuthorize.Web;

internal class JwtTokenConfiguration(
    TokenValidationParameters tokenValidationParameters,
    int jwtTokenLifeSeconds,
    int refreshTokenLifeSeconds)
{
    public TokenValidationParameters TokenValidationParameters { get; } = tokenValidationParameters;

    public int JwtTokenLifeSeconds { get; } = jwtTokenLifeSeconds;

    public int RefreshTokenLifeSeconds { get; } = refreshTokenLifeSeconds;
}