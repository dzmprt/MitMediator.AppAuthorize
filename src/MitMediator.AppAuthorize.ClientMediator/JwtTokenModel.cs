using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace MitMediator.AppAuthorize.ClientMediator;

/// <summary>
/// Client-side JWT response. This type decodes claims and expiration but does
/// not validate the token signature, issuer, audience, or lifetime.
/// </summary>
public sealed class JwtTokenModel
{
    [JsonConstructor]
    public JwtTokenModel(
        string jwtToken,
        string? refreshToken = null,
        DateTimeOffset? refreshTokenExpires = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwtToken);

        JwtSecurityToken parsedToken;
        try
        {
            parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(jwtToken);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException(
                "The JWT token is invalid.",
                nameof(jwtToken),
                exception);
        }

        if (string.IsNullOrWhiteSpace(refreshToken) !=
            (refreshTokenExpires is null))
        {
            throw new ArgumentException(
                "The refresh token and its expiration must be provided together.",
                nameof(refreshToken));
        }

        if (refreshToken is not null &&
            string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException(
                "The refresh token cannot be blank.",
                nameof(refreshToken));
        }

        var userIds = parsedToken.Claims
            .Where(claim => claim.Type == ClaimTypes.NameIdentifier)
            .Select(claim => claim.Value)
            .Take(2)
            .ToArray();
        if (userIds is not { Length: 1 } ||
            string.IsNullOrWhiteSpace(userIds[0]))
        {
            throw new ArgumentException(
                "The JWT token must contain exactly one non-blank user identifier claim.",
                nameof(jwtToken));
        }

        JwtToken = jwtToken;
        RefreshToken = refreshToken;
        RefreshTokenExpires = refreshTokenExpires;
        JwtTokenExpires =
            DateTimeOffset.FromUnixTimeSeconds(
                parsedToken.Payload.Expiration
                ?? throw new ArgumentException(
                    "The JWT token does not contain an expiration claim.",
                    nameof(jwtToken)));
        UserId = userIds[0];
    }

    public string JwtToken { get; }

    public string? RefreshToken { get; }

    public DateTimeOffset JwtTokenExpires { get; }

    public DateTimeOffset? RefreshTokenExpires { get; }

    public bool IsJwtExpired(DateTimeOffset utcNow) =>
        JwtTokenExpires <= utcNow;

    public bool IsRefreshTokenExpired(DateTimeOffset utcNow) =>
        RefreshTokenExpires is not null &&
        RefreshTokenExpires <= utcNow;

    public string UserId { get; }
}
