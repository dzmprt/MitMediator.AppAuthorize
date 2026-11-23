using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace MitMediator.AppAuthorize.Web;

/// <summary>
/// Dependency injection.
/// </summary>
public static class DependencyInjection
{
    private const int MinimumHmacKeySizeInBytes = 32;

    /// <summary>
    /// Add Auth services.
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <param name="issuerSigningKey">SecurityKey that is to be used for signature validation.</param>
    /// <param name="issuer">The trusted token issuer.</param>
    /// <param name="audience">The trusted token audience.</param>
    /// <param name="jwtTokenLifeSeconds">Jwt token lifetime in seconds.</param>
    /// <param name="refreshTokenLifeSeconds">Refresh token lifetime in seconds.</param>
    /// <param name="inputLimits">Optional authentication input limits.</param>
    /// <returns><see cref="IServiceProvider"/>.</returns>
    public static IServiceCollection AddJwtAuthServices(this IServiceCollection services,
        string issuerSigningKey,
        string issuer,
        string audience,
        int jwtTokenLifeSeconds = 600,
        int refreshTokenLifeSeconds = 2592000,
        AuthInputLimits? inputLimits = null)
    {
        ArgumentNullException.ThrowIfNull(issuerSigningKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        ValidateTokenLifetimes(jwtTokenLifeSeconds, refreshTokenLifeSeconds);

        var keyBytes = Encoding.UTF8.GetBytes(issuerSigningKey);
        if (keyBytes.Length < MinimumHmacKeySizeInBytes)
        {
            throw new ArgumentException(
                $"The signing key must contain at least {MinimumHmacKeySizeInBytes} UTF-8 bytes.",
                nameof(issuerSigningKey));
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
        return services.AddJwtAuthServices(
            tokenValidationParameters,
            issuer,
            audience,
            jwtTokenLifeSeconds,
            refreshTokenLifeSeconds,
            inputLimits);
    }

    /// <summary>
    /// Add Auth services.
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <param name="tokenValidationParameters">Parameters used to validate identity tokens.</param>
    /// <param name="jwtTokenLifeSeconds">Jwt token lifetime in seconds.</param>
    /// <param name="refreshTokenLifeSeconds">Refresh token lifetime in seconds.</param>
    /// <param name="inputLimits">Optional authentication input limits.</param>
    /// <returns><see cref="IServiceProvider"/>.</returns>
    public static IServiceCollection AddJwtAuthServices(this IServiceCollection services,
        TokenValidationParameters tokenValidationParameters,
        int jwtTokenLifeSeconds = 600,
        int refreshTokenLifeSeconds = 2592000,
        AuthInputLimits? inputLimits = null) =>
        AddJwtAuthServicesCore(
            services,
            tokenValidationParameters,
            jwtTokenLifeSeconds,
            refreshTokenLifeSeconds,
            inputLimits: inputLimits);

    /// <summary>
    /// Add Auth services with explicitly selected issuer and audience values for
    /// newly issued tokens.
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <param name="tokenValidationParameters">Parameters used to validate identity tokens.</param>
    /// <param name="tokenIssuer">Issuer written to newly issued tokens.</param>
    /// <param name="tokenAudience">Audience written to newly issued tokens.</param>
    /// <param name="jwtTokenLifeSeconds">Jwt token lifetime in seconds.</param>
    /// <param name="refreshTokenLifeSeconds">Refresh token lifetime in seconds.</param>
    /// <param name="inputLimits">Optional authentication input limits.</param>
    /// <returns><see cref="IServiceProvider"/>.</returns>
    public static IServiceCollection AddJwtAuthServices(
        this IServiceCollection services,
        TokenValidationParameters tokenValidationParameters,
        string tokenIssuer,
        string tokenAudience,
        int jwtTokenLifeSeconds = 600,
        int refreshTokenLifeSeconds = 2592000,
        AuthInputLimits? inputLimits = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenIssuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenAudience);
        return AddJwtAuthServicesCore(
            services,
            tokenValidationParameters,
            jwtTokenLifeSeconds,
            refreshTokenLifeSeconds,
            tokenIssuer,
            [tokenAudience],
            inputLimits);
    }

    /// <summary>
    /// Add Auth services with explicitly selected issuer and audience values for
    /// newly issued tokens.
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <param name="tokenValidationParameters">Parameters used to validate identity tokens.</param>
    /// <param name="tokenIssuer">Issuer written to newly issued tokens.</param>
    /// <param name="tokenAudiences">Audiences written to newly issued tokens.</param>
    /// <param name="jwtTokenLifeSeconds">Jwt token lifetime in seconds.</param>
    /// <param name="refreshTokenLifeSeconds">Refresh token lifetime in seconds.</param>
    /// <param name="inputLimits">Optional authentication input limits.</param>
    public static IServiceCollection AddJwtAuthServices(
        this IServiceCollection services,
        TokenValidationParameters tokenValidationParameters,
        string tokenIssuer,
        IEnumerable<string> tokenAudiences,
        int jwtTokenLifeSeconds = 600,
        int refreshTokenLifeSeconds = 2592000,
        AuthInputLimits? inputLimits = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenIssuer);
        ArgumentNullException.ThrowIfNull(tokenAudiences);
        var audiences = tokenAudiences.ToArray();
        if (audiences.Length == 0 ||
            audiences.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "At least one non-blank token audience is required.",
                nameof(tokenAudiences));
        }

        return AddJwtAuthServicesCore(
            services,
            tokenValidationParameters,
            jwtTokenLifeSeconds,
            refreshTokenLifeSeconds,
            tokenIssuer,
            audiences,
            inputLimits);
    }

    private static IServiceCollection AddJwtAuthServicesCore(
        IServiceCollection services,
        TokenValidationParameters tokenValidationParameters,
        int jwtTokenLifeSeconds,
        int refreshTokenLifeSeconds,
        string? tokenIssuer = null,
        IEnumerable<string>? tokenAudiences = null,
        AuthInputLimits? inputLimits = null)
    {
        ArgumentNullException.ThrowIfNull(tokenValidationParameters);
        ValidateTokenLifetimes(jwtTokenLifeSeconds, refreshTokenLifeSeconds);
        ValidateTokenValidationParameters(tokenValidationParameters);
        var configuration = new JwtTokenConfiguration(
            tokenValidationParameters,
            jwtTokenLifeSeconds,
            refreshTokenLifeSeconds,
            tokenIssuer,
            tokenAudiences);
        var configuredParameters =
            configuration.TokenValidationParameters;
        ValidateIssuanceConfiguration(configuration);

        var existingConfiguration = services
            .FirstOrDefault(
                descriptor =>
                    descriptor.ServiceType == typeof(JwtTokenConfiguration))
            ?.ImplementationInstance as JwtTokenConfiguration;
        if (existingConfiguration is not null)
        {
            throw new InvalidOperationException(
                "JWT authentication is already configured and cannot be reconfigured.");
        }

        var certificateKey = configuredParameters.IssuerSigningKey as X509SecurityKey;
        using var ecdsaPublicKey = certificateKey?.Certificate.GetECDsaPublicKey();
        var x509EcdsaPublicKey = ecdsaPublicKey?.ExportSubjectPublicKeyInfo();
        if (x509EcdsaPublicKey is not null)
        {
            services.AddSingleton(
                _ => new EcdsaValidationKey(x509EcdsaPublicKey));
        }

        services.AddValidation()
            .AddAuthorization()
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                
                if (x509EcdsaPublicKey is null)
                {
                    options.TokenValidationParameters =
                        configuredParameters.Clone();
                }
            });

        if (certificateKey is not null && x509EcdsaPublicKey is not null)
        {
            services
                .AddOptions<JwtBearerOptions>(
                    JwtBearerDefaults.AuthenticationScheme)
                .Configure<EcdsaValidationKey>(
                    (options, validationKey) =>
                    {
                        var parameters = configuredParameters.Clone();
                        ConfigureEcdsaCertificateValidation(
                            parameters, certificateKey, validationKey.Key);
                        options.TokenValidationParameters = parameters;
                    });
        }

        services.AddSingleton<CreateJwtTokenService>();
        services.AddSingleton(configuration);
        services.AddSingleton(inputLimits ?? new AuthInputLimits());
        services.AddHostedService<JwtConfigurationDiagnostics>();

        return services;
    }

    private static void ConfigureEcdsaCertificateValidation(
        TokenValidationParameters parameters,
        X509SecurityKey certificateKey,
        ECDsa publicKey)
    {
        // IdentityModel can verify the signature with a raw ECDSA key, but
        // its signing-key validator needs the X.509 key to check certificate dates.
        var signatureKey = new ECDsaSecurityKey(publicKey)
        {
            KeyId = certificateKey.KeyId
        };
        parameters.IssuerSigningKey = signatureKey;

        var configuredValidator = parameters.IssuerSigningKeyValidatorUsingConfiguration;
        parameters.IssuerSigningKeyValidatorUsingConfiguration =
            (key, token, validationParameters, configuration) =>
            {
                var signingKey = ReferenceEquals(key, signatureKey) ? certificateKey : key;
                if (configuredValidator is not null)
                {
                    return configuredValidator(signingKey, token, validationParameters, configuration);
                }

                if (validationParameters.IssuerSigningKeyValidator is { } validator)
                {
                    return validator(signingKey, token, validationParameters);
                }

                var defaultParameters = validationParameters.Clone();
                defaultParameters.IssuerSigningKeyValidatorUsingConfiguration = null;
                Validators.ValidateIssuerSecurityKey(signingKey, token, defaultParameters);
                return true;
            };
    }

    private static void ValidateIssuanceConfiguration(
        JwtTokenConfiguration configuration)
    {
        var parameters = configuration.TokenValidationParameters;
        var algorithm =
            CreateJwtTokenService.GetSigningAlgorithm(
                parameters.IssuerSigningKey!);
        if (parameters.ValidAlgorithms is { } validAlgorithms &&
            validAlgorithms.Any() &&
            !validAlgorithms.Contains(
                algorithm,
                StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"The configured valid algorithms do not include the algorithm used for token issuance ('{algorithm}').",
                "tokenValidationParameters");
        }

        if (parameters.ValidateIssuer &&
            (configuration.TokenIssuer is null ||
             !GetValidIssuers(parameters).Contains(
                 configuration.TokenIssuer,
                 StringComparer.Ordinal)))
        {
            throw new ArgumentException(
                "The token issuer must be one of the configured valid issuers.",
                "tokenIssuer");
        }

        if (parameters.ValidateAudience &&
            (configuration.TokenAudiences.Count == 0 ||
             configuration.TokenAudiences.Any(
                 audience => !GetValidAudiences(parameters).Contains(
                     audience,
                     StringComparer.Ordinal))))
        {
            throw new ArgumentException(
                "Every token audience must be one of the configured valid audiences.",
                "tokenAudiences");
        }
    }

    private static string[] GetValidIssuers(
        TokenValidationParameters parameters) =>
        GetConfiguredValues(parameters.ValidIssuer, null);

    private static string[] GetValidAudiences(
        TokenValidationParameters parameters) =>
        GetConfiguredValues(
            parameters.ValidAudience,
            parameters.ValidAudiences);

    private static string[] GetConfiguredValues(
        string? value,
        IEnumerable<string>? values) =>
        (values ?? [])
            .Prepend(value)
            .Where(candidate => candidate is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private sealed class EcdsaValidationKey : IDisposable
    {
        public EcdsaValidationKey(byte[] publicKey)
        {
            Key = ECDsa.Create();
            Key.ImportSubjectPublicKeyInfo(publicKey, out _);
        }

        public ECDsa Key { get; }

        public void Dispose() => Key.Dispose();
    }

    /// <summary>
    /// Use default authentication context <see cref="DefaultAuthenticationContext"/> and add HttpContext accessor.
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <returns><see cref="IServiceProvider"/>.</returns>
    public static IServiceCollection AddDefaultAuthContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddTransient<
            IAuthenticationContext,
            DefaultAuthenticationContext>();
        return services;
    }

    private static void ValidateTokenLifetimes(
        int jwtTokenLifeSeconds,
        int refreshTokenLifeSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            jwtTokenLifeSeconds,
            0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            refreshTokenLifeSeconds,
            0);
    }

    private static void ValidateTokenValidationParameters(
        TokenValidationParameters tokenValidationParameters)
    {
        if (tokenValidationParameters.ValidIssuers?.Any() == true)
        {
            throw new ArgumentException(
                "The ValidIssuers collection is not supported. Configure ValidIssuer with one trusted issuer.",
                nameof(tokenValidationParameters));
        }

        if (!tokenValidationParameters.RequireSignedTokens)
        {
            throw new ArgumentException(
                "Signed tokens are required.",
                nameof(tokenValidationParameters));
        }

        if (!tokenValidationParameters.ValidateIssuerSigningKey)
        {
            throw new ArgumentException(
                "Issuer signing-key validation must be enabled.",
                nameof(tokenValidationParameters));
        }

        if (!tokenValidationParameters.ValidateLifetime)
        {
            throw new ArgumentException(
                "Token lifetime validation must be enabled.",
                nameof(tokenValidationParameters));
        }

        if (!tokenValidationParameters.ValidateIssuer)
        {
            throw new ArgumentException(
                "Issuer validation must be enabled.",
                nameof(tokenValidationParameters));
        }

        if (!tokenValidationParameters.ValidateAudience)
        {
            throw new ArgumentException(
                "Audience validation must be enabled.",
                nameof(tokenValidationParameters));
        }

        ValidateClaimTypes(tokenValidationParameters);

        var issuerSigningKey = tokenValidationParameters.IssuerSigningKey;
        if (issuerSigningKey is null)
        {
            throw new ArgumentException(
                "An issuer signing key is required to issue tokens.",
                nameof(tokenValidationParameters));
        }

        if (issuerSigningKey is not (
            SymmetricSecurityKey or
            RsaSecurityKey or
            ECDsaSecurityKey or
            X509SecurityKey))
        {
            throw new ArgumentException(
                $"The signing key type '{issuerSigningKey.GetType().Name}' is not supported for token issuance.",
                nameof(tokenValidationParameters));
        }

        if (issuerSigningKey is SymmetricSecurityKey { KeySize: < 256 })
        {
            throw new ArgumentException(
                "The symmetric signing key must contain at least 256 bits.",
                nameof(tokenValidationParameters));
        }

        if (issuerSigningKey is
            AsymmetricSecurityKey and
            not X509SecurityKey and
            {
                PrivateKeyStatus: not PrivateKeyStatus.Exists
            })
        {
            throw new ArgumentException(
                "The asymmetric signing key must include a private key.",
                nameof(tokenValidationParameters));
        }

        if (issuerSigningKey is RsaSecurityKey { KeySize: < 2048 })
        {
            throw new ArgumentException(
                "The RSA signing key must contain at least 2048 bits.",
                nameof(tokenValidationParameters));
        }

        if (issuerSigningKey is ECDsaSecurityKey { KeySize: var ecdsaKeySize } &&
            ecdsaKeySize is not (256 or 384 or 521))
        {
            throw new ArgumentException(
                "The ECDSA signing key must use a supported 256-, 384-, or 521-bit curve.",
                nameof(tokenValidationParameters));
        }

        if (issuerSigningKey is X509SecurityKey
            {
                Certificate: var certificate
            })
        {
            using var rsa = certificate.GetRSAPrivateKey();
            using var ecdsa = certificate.GetECDsaPrivateKey();

            if (rsa is null && ecdsa is null)
            {
                throw new ArgumentException(
                    "The X.509 signing key must contain an RSA or ECDSA private key.",
                    nameof(tokenValidationParameters));
            }

            if (rsa is { KeySize: < 2048 })
            {
                throw new ArgumentException(
                    "The X.509 RSA signing key must contain at least 2048 bits.",
                    nameof(tokenValidationParameters));
            }

            if (ecdsa is { KeySize: < 256 })
            {
                throw new ArgumentException(
                    "The X.509 ECDSA signing key must contain at least 256 bits.",
                    nameof(tokenValidationParameters));
            }

            if (ecdsa is not null &&
                ecdsa.KeySize is not (256 or 384 or 521))
            {
                throw new ArgumentException(
                    "The X.509 ECDSA signing key must use a supported 256-, 384-, or 521-bit curve.",
                    nameof(tokenValidationParameters));
            }
        }

        if (tokenValidationParameters.ValidateIssuer &&
            string.IsNullOrWhiteSpace(tokenValidationParameters.ValidIssuer))
        {
            throw new ArgumentException(
                "Issuer validation requires a valid issuer.",
                nameof(tokenValidationParameters));
        }

        if (tokenValidationParameters.ValidateAudience)
        {
            var validAudiences = tokenValidationParameters.ValidAudiences
                ?.ToArray() ?? [];
            var hasSingleAudience =
                tokenValidationParameters.ValidAudience is not null;
            if ((!hasSingleAudience && validAudiences.Length == 0) ||
                (hasSingleAudience &&
                 string.IsNullOrWhiteSpace(
                     tokenValidationParameters.ValidAudience)) ||
                validAudiences.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(
                    "Audience validation requires at least one non-blank valid audience.",
                    nameof(tokenValidationParameters));
            }
        }
    }

    private static void ValidateClaimTypes(
        TokenValidationParameters tokenValidationParameters)
    {
        var nameClaimType = tokenValidationParameters.NameClaimType;
        var roleClaimType = tokenValidationParameters.RoleClaimType;
        if (string.IsNullOrWhiteSpace(nameClaimType) ||
            string.IsNullOrWhiteSpace(roleClaimType))
        {
            throw new ArgumentException(
                "Name and role claim types must be non-blank.",
                nameof(tokenValidationParameters));
        }

        var reservedClaimTypes = new[]
        {
            System.Security.Claims.ClaimTypes.NameIdentifier,
            AppAuthorizeClaimTypes.TenantId
        };
        if (string.Equals(
                nameClaimType,
                roleClaimType,
                StringComparison.Ordinal) ||
            reservedClaimTypes.Contains(
                nameClaimType,
                StringComparer.Ordinal) ||
            reservedClaimTypes.Contains(
                roleClaimType,
                StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "Name and role claim types must be distinct from each other and from identifier and tenant claim types.",
                nameof(tokenValidationParameters));
        }
    }
}