using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace MitMediator.AppAuthorize.Web;

internal class CreateJwtTokenService(JwtTokenConfiguration jwtTokenConfiguration)
{
    private static readonly CryptoProviderFactory UncachedSigningProviderFactory = new()
    {
        CacheSignatureProviders = false
    };

    public string CreateJwtToken(
        UserInfo user,
        DateTimeOffset jwtTokenDateExpires)
    {
        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.UserId));
        claims.Add(new Claim(
            JwtRegisteredClaimNames.Iat,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                .ToString(CultureInfo.InvariantCulture),
            ClaimValueTypes.Integer64));

        foreach (var audience in jwtTokenConfiguration.TokenAudiences)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));
        }

        if (user.Name is not null)
        {
            claims.Add(new Claim(
                jwtTokenConfiguration.TokenValidationParameters.NameClaimType,
                user.Name));
        }

        if (user.Roles is not null)
        {
            foreach (var userRole in user.Roles)
            {
                claims.Add(new Claim(
                    jwtTokenConfiguration.TokenValidationParameters.RoleClaimType,
                    userRole));
            }
        }

        if (user.Tenants is not null)
        {
            foreach (var tenant in user.Tenants)
            {
                claims.Add(new Claim(AppAuthorizeClaimTypes.TenantId, tenant));
            }
        }

        var securityKey = jwtTokenConfiguration.TokenValidationParameters.IssuerSigningKey;
        if (securityKey is X509SecurityKey certificateKey)
        {
            using var ecdsa = certificateKey.Certificate.GetECDsaPrivateKey();
            if (ecdsa is not null)
            {
                var algorithm = GetEcdsaAlgorithm(ecdsa);
                return WriteToken(
                    new SigningCredentials(
                        new ECDsaSecurityKey(ecdsa) { KeyId = certificateKey.KeyId },
                        algorithm)
                    {
                        // The private key is disposed at the end of this call.
                        CryptoProviderFactory = UncachedSigningProviderFactory
                    });
            }
        }

        return WriteToken(
            new SigningCredentials(
                securityKey,
                GetSigningAlgorithm(securityKey)));

        string WriteToken(SigningCredentials credentials)
        {
            var tokenDescriptor = new JwtSecurityToken(
                jwtTokenConfiguration.TokenIssuer,
                audience: null,
                claims,
                expires: jwtTokenDateExpires.UtcDateTime,
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler()
                .WriteToken(tokenDescriptor)!;
        }
    }

    internal static string GetSigningAlgorithm(SecurityKey securityKey)
    {
        return securityKey switch
        {
            RsaSecurityKey => SecurityAlgorithms.RsaSha256,
            X509SecurityKey { Certificate: var certificate } =>
                GetX509SigningAlgorithm(certificate),
            ECDsaSecurityKey ecdsa => GetEcdsaAlgorithm(ecdsa.ECDsa),
            SymmetricSecurityKey => SecurityAlgorithms.HmacSha256,
            _ => throw new NotSupportedException(
                $"The signing key type '{securityKey.GetType().Name}' is not supported.")
        };
    }

    private static string GetX509SigningAlgorithm(
        X509Certificate2 certificate)
    {
        using var ecdsa = certificate.GetECDsaPrivateKey();
        if (ecdsa is not null)
        {
            return GetEcdsaAlgorithm(ecdsa);
        }

        using var rsa = certificate.GetRSAPrivateKey();
        return rsa is not null
            ? SecurityAlgorithms.RsaSha256
            : throw new InvalidOperationException(
                "The X.509 signing certificate does not contain a supported private key.");
    }

    private static string GetEcdsaAlgorithm(ECDsa key) => key.KeySize switch
    {
        256 => SecurityAlgorithms.EcdsaSha256,
        384 => SecurityAlgorithms.EcdsaSha384,
        521 => SecurityAlgorithms.EcdsaSha512,
        _ => throw new NotSupportedException(
            $"The ECDSA key size '{key.KeySize}' is not supported.")
    };
}
