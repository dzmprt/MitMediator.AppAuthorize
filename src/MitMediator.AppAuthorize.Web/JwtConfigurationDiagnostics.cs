using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace MitMediator.AppAuthorize.Web;

internal sealed class JwtConfigurationDiagnostics(
    JwtTokenConfiguration configuration,
    ILogger<JwtConfigurationDiagnostics> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var parameters = configuration.TokenValidationParameters;

        WarnIfReservedClaimType(parameters.NameClaimType, nameof(parameters.NameClaimType));
        WarnIfReservedClaimType(parameters.RoleClaimType, nameof(parameters.RoleClaimType));

        if (parameters.NameClaimTypeRetriever is not null ||
            parameters.RoleClaimTypeRetriever is not null)
        {
            logger.LogWarning(
                "JWT NameClaimTypeRetriever or RoleClaimTypeRetriever is configured. Ensure it never selects a reserved JWT claim type.");
        }

        if (!configuration.HasExplicitTokenAudiences && configuration.TokenAudiences.Count > 1)
        {
            logger.LogWarning(
                "JWT issuance uses multiple audiences inherited from validation. Use an AddJwtAuthServices overload with explicit issuance audiences to avoid issuing tokens for unintended audiences.");
        }

        if (!parameters.RequireExpirationTime)
        {
            logger.LogWarning(
                "JWT RequireExpirationTime is disabled. Require an expiration time for validated tokens.");
        }

        (string Name, Delegate? Validator)[] validators =
        [
            (nameof(parameters.AlgorithmValidator), parameters.AlgorithmValidator),
            (nameof(parameters.AudienceValidator), parameters.AudienceValidator),
            (nameof(parameters.IssuerValidator), parameters.IssuerValidator),
            (nameof(parameters.IssuerValidatorUsingConfiguration), parameters.IssuerValidatorUsingConfiguration),
            (nameof(parameters.IssuerSigningKeyValidator), parameters.IssuerSigningKeyValidator),
            (nameof(parameters.IssuerSigningKeyValidatorUsingConfiguration), parameters.IssuerSigningKeyValidatorUsingConfiguration),
            (nameof(parameters.LifetimeValidator), parameters.LifetimeValidator),
            (nameof(parameters.SignatureValidator), parameters.SignatureValidator),
            (nameof(parameters.SignatureValidatorUsingConfiguration), parameters.SignatureValidatorUsingConfiguration)
        ];
        foreach (var (name, validator) in validators)
        {
            if (validator is not null)
            {
                logger.LogWarning(
                    "Custom JWT {ValidationSetting} can replace built-in validation. Ensure it enforces the intended token validation policy.",
                    name);
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private void WarnIfReservedClaimType(string claimType, string setting)
    {
        if (claimType is JwtRegisteredClaimNames.Iss or JwtRegisteredClaimNames.Sub
            or JwtRegisteredClaimNames.Aud or JwtRegisteredClaimNames.Exp
            or JwtRegisteredClaimNames.Nbf or JwtRegisteredClaimNames.Iat
            or JwtRegisteredClaimNames.Jti)
        {
            logger.LogWarning(
                "JWT {ClaimTypeSetting} uses the reserved claim type {ReservedClaimType}. Use a separate application claim type.",
                setting,
                claimType);
        }
    }
}
