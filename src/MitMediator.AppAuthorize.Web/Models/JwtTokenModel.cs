namespace MitMediator.AppAuthorize.Web.Models;

internal sealed class JwtTokenModel
{
    public JwtTokenModel(
        string jwtToken,
        string? refreshToken = null,
        DateTimeOffset? refreshTokenExpires = null)
    {
        JwtToken = jwtToken;
        RefreshToken = refreshToken;
        RefreshTokenExpires = refreshTokenExpires;
    }

    public string JwtToken { get; }

    public string? RefreshToken { get; }

    public DateTimeOffset? RefreshTokenExpires { get; }
}
