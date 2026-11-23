using Microsoft.IdentityModel.Tokens;

namespace MitMediator.AppAuthorize.Web;

internal class JwtTokenConfiguration
{
    public JwtTokenConfiguration(
        TokenValidationParameters tokenValidationParameters,
        int jwtTokenLifeSeconds,
        int refreshTokenLifeSeconds,
        string? tokenIssuer = null,
        IEnumerable<string>? tokenAudiences = null)
    {
        TokenValidationParameters = CloneParameters(tokenValidationParameters);
        JwtTokenLifeSeconds = jwtTokenLifeSeconds;
        RefreshTokenLifeSeconds = refreshTokenLifeSeconds;
        TokenIssuer = tokenIssuer
                      ?? TokenValidationParameters.ValidIssuer;
        HasExplicitTokenAudiences = tokenAudiences is not null;
        TokenAudiences = Array.AsReadOnly(
            (tokenAudiences ?? GetConfiguredAudiences(
                    TokenValidationParameters))
            .Distinct(StringComparer.Ordinal)
            .ToArray());
    }

    public TokenValidationParameters TokenValidationParameters { get; }

    public int JwtTokenLifeSeconds { get; }

    public int RefreshTokenLifeSeconds { get; }

    public string? TokenIssuer { get; }

    public bool HasExplicitTokenAudiences { get; }

    public IReadOnlyList<string> TokenAudiences { get; }

    private static TokenValidationParameters CloneParameters(
        TokenValidationParameters source)
    {
        var clone = source.Clone();
        clone.ValidIssuers = source.ValidIssuers?.ToArray();
        clone.ValidAudiences = source.ValidAudiences?.ToArray();
        clone.ValidAlgorithms = source.ValidAlgorithms?.ToArray();
        clone.IssuerSigningKeys = source.IssuerSigningKeys?.ToArray();
        return clone;
    }

    private static IEnumerable<string> GetConfiguredAudiences(
        TokenValidationParameters parameters)
    {
        if (parameters.ValidAudience is { } audience)
        {
            yield return audience;
        }

        if (parameters.ValidAudiences is null)
        {
            yield break;
        }

        foreach (var configuredAudience in parameters.ValidAudiences)
        {
            yield return configuredAudience;
        }
    }
}