using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MitMediator.AppAuthorize.Web.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public async Task HostStartup_ShouldWarnAboutJwtSettingsWithoutBlockingStartup()
    {
        var parameters = CreateParametersForDiagnostics();
        parameters.RoleClaimType = JwtRegisteredClaimNames.Aud;
        parameters.NameClaimTypeRetriever = (_, _) => System.Security.Claims.ClaimTypes.Name;
        parameters.RequireExpirationTime = false;
        parameters.AlgorithmValidator = (algorithm, _, _, _) => algorithm == SecurityAlgorithms.HmacSha256;
        parameters.ValidAudiences = ["another-test-audience"];

        var warnings = await CollectJwtStartupWarnings(
            services => services.AddJwtAuthServices(parameters));

        Assert.Collection(warnings,
            message => Assert.Contains("RoleClaimType uses the reserved claim type", message),
            message => Assert.Contains("NameClaimTypeRetriever or RoleClaimTypeRetriever", message),
            message => Assert.Contains("multiple audiences inherited from validation", message),
            message => Assert.Contains("RequireExpirationTime is disabled", message),
            message => Assert.Contains("AlgorithmValidator can replace built-in validation", message));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HostStartup_ShouldNotWarnAboutExplicitOrSingleDistinctIssuanceAudience(bool explicitAudience)
    {
        var parameters = CreateParametersForDiagnostics();
        parameters.ValidAudiences = explicitAudience
            ? ["another-test-audience"]
            : [parameters.ValidAudience!];

        var warnings = await CollectJwtStartupWarnings(services =>
        {
            if (explicitAudience)
            {
                services.AddJwtAuthServices(parameters, "test-issuer", "test-audience");
            }
            else
            {
                services.AddJwtAuthServices(parameters);
            }
        });

        Assert.Empty(warnings);
    }

    private static TokenValidationParameters CreateParametersForDiagnostics() => new()
    {
        ValidIssuer = "test-issuer",
        ValidAudience = "test-audience",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32))
    };

    private static async Task<IReadOnlyList<string>> CollectJwtStartupWarnings(Action<IServiceCollection> configure)
    {
        var logger = new JwtStartupLogger();
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                configure(services);
                services.AddSingleton<ILogger<JwtConfigurationDiagnostics>>(logger);
            })
            .Build();

        await host.StartAsync();
        await host.StopAsync();

        return logger.Warnings;
    }

    private sealed class JwtStartupLogger : ILogger<JwtConfigurationDiagnostics>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }

    [Fact]
    public void AddJwtAuthServices_WithKey_ShouldRegisterJwtTokenConfigurationAndTokenService()
    {
        // Arrange
        var services = new ServiceCollection();
        var key = "supersecretkey12345678901234567890";

        // Act
        services.AddJwtAuthServices(
            key,
            "test-issuer",
            "test-audience",
            jwtTokenLifeSeconds: 900,
            refreshTokenLifeSeconds: 3600);
        var provider = services.BuildServiceProvider();

        // Assert
        var config = provider.GetService<JwtTokenConfiguration>();
        var tokenService = provider.GetService<CreateJwtTokenService>();

        Assert.NotNull(config);
        Assert.NotNull(tokenService);
        Assert.Equal(900, config.JwtTokenLifeSeconds);
        Assert.Equal(3600, config.RefreshTokenLifeSeconds);
        Assert.IsType<SymmetricSecurityKey>(config.TokenValidationParameters.IssuerSigningKey);
    }

    [Fact]
    public void AddJwtAuthServices_WithParameters_ShouldRegisterJwtTokenConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                "key12345678901234567890123456789012"u8.ToArray())
        };

        // Act
        services.AddJwtAuthServices(parameters, jwtTokenLifeSeconds: 1200, refreshTokenLifeSeconds: 7200);
        var provider = services.BuildServiceProvider();

        // Assert
        var config = provider.GetService<JwtTokenConfiguration>();
        Assert.NotNull(config);
        Assert.Equal(1200, config.JwtTokenLifeSeconds);
        Assert.Equal(7200, config.RefreshTokenLifeSeconds);
        Assert.NotSame(parameters, config.TokenValidationParameters);
        Assert.NotSame(config.TokenValidationParameters,
            provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme)
                .TokenValidationParameters);
    }

    [Fact]
    public void AddJwtAuthServices_ShouldDefensivelyCopyValidationParameters()
    {
        var validAudiences = new List<string> { "audience-a" };
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "issuer-a",
            ValidAudiences = validAudiences,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                "key12345678901234567890123456789012"u8.ToArray())
        };
        var services = new ServiceCollection();

        services.AddJwtAuthServices(
            parameters,
            tokenIssuer: "issuer-a",
            tokenAudience: "audience-a");
        validAudiences.Add("audience-b");
        parameters.ValidIssuer = "issuer-c";
        parameters.ValidAudience = "audience-c";
        var provider = services.BuildServiceProvider();

        var configuration =
            provider.GetRequiredService<JwtTokenConfiguration>();
        Assert.Equal("issuer-a", configuration.TokenIssuer);
        Assert.Equal(["audience-a"], configuration.TokenAudiences);
        Assert.Equal("issuer-a", configuration.TokenValidationParameters.ValidIssuer);
        Assert.Equal(["audience-a"], configuration.TokenValidationParameters.ValidAudiences);
        Assert.Null(configuration.TokenValidationParameters.ValidAudience);
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRegisterDefaultInputLimits()
    {
        var provider = new ServiceCollection()
            .AddJwtAuthServices(
                "key12345678901234567890123456789012",
                "issuer",
                "audience")
            .BuildServiceProvider();

        var limits = provider.GetRequiredService<AuthInputLimits>();

        Assert.Equal(256, limits.LoginLength);
        Assert.Equal(1024, limits.PasswordLength);
        Assert.Equal(1024, limits.CodeLength);
        Assert.Equal(256, limits.UserIdLength);
        Assert.Equal(4096, limits.RefreshTokenLength);
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRegisterConfiguredInputLimits()
    {
        var expected = new AuthInputLimits(
            loginLength: 10,
            passwordLength: 20,
            codeLength: 30,
            userIdLength: 40,
            refreshTokenLength: 50);
        var provider = new ServiceCollection()
            .AddJwtAuthServices(
                "key12345678901234567890123456789012",
                "issuer",
                "audience",
                inputLimits: expected)
            .BuildServiceProvider();

        Assert.Same(
            expected,
            provider.GetRequiredService<AuthInputLimits>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AuthInputLimits_ShouldRejectNonPositiveValues(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AuthInputLimits(loginLength: value));
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRejectWeakSigningKey()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            services.AddJwtAuthServices(
                "weak-key",
                "test-issuer",
                "test-audience"));
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(-1, 60)]
    [InlineData(60, 0)]
    [InlineData(60, -1)]
    public void AddJwtAuthServices_ShouldRejectNonPositiveTokenLifetimes(
        int jwtTokenLifeSeconds,
        int refreshTokenLifeSeconds)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddJwtAuthServices(
                "supersecretkey12345678901234567890",
                "test-issuer",
                "test-audience",
                jwtTokenLifeSeconds,
                refreshTokenLifeSeconds));
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRejectMissingSigningKey_WhenValidationIsEnabled()
    {
        var services = new ServiceCollection();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true
        };

        Assert.Throws<ArgumentException>(() =>
            services.AddJwtAuthServices(parameters));
    }

    [Theory]
    [InlineData("", "audience")]
    [InlineData("issuer", "")]
    [InlineData(" ", "audience")]
    public void AddJwtAuthServices_ShouldRejectMissingIssuerOrAudience(
        string issuer,
        string audience)
    {
        var services = new ServiceCollection();

        Assert.ThrowsAny<ArgumentException>(() =>
            services.AddJwtAuthServices(
                "supersecretkey12345678901234567890",
                issuer,
                audience));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddJwtAuthServices_ShouldRejectBlankConfiguredIssuer(
        string issuer)
    {
        var parameters = CreateParameters();
        parameters.ValidIssuer = issuer;

        var exception = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddJwtAuthServices(parameters));

        Assert.Equal("tokenValidationParameters", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddJwtAuthServices_ShouldRejectBlankConfiguredAudience(
        string audience)
    {
        var parameters = CreateParameters();
        parameters.ValidAudience = audience;
        parameters.ValidAudiences = null;

        var exception = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddJwtAuthServices(parameters));

        Assert.Equal("tokenValidationParameters", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void AddJwtAuthServices_ShouldRejectBlankAudienceInCollection(
        string? audience)
    {
        var parameters = CreateParameters();
        parameters.ValidAudiences = ["other-audience", audience!];

        var exception = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddJwtAuthServices(parameters));

        Assert.Equal("tokenValidationParameters", exception.ParamName);
    }

    [Theory]
    [InlineData("same")]
    [InlineData("name-identifier")]
    [InlineData("role-identifier")]
    [InlineData("name-tenant")]
    [InlineData("role-tenant")]
    public void AddJwtAuthServices_ShouldRejectConflictingClaimTypes(
        string conflict)
    {
        var parameters = CreateParameters();
        switch (conflict)
        {
            case "same":
                parameters.NameClaimType = "shared";
                parameters.RoleClaimType = "shared";
                break;
            case "name-identifier":
                parameters.NameClaimType =
                    System.Security.Claims.ClaimTypes.NameIdentifier;
                break;
            case "role-identifier":
                parameters.RoleClaimType =
                    System.Security.Claims.ClaimTypes.NameIdentifier;
                break;
            case "name-tenant":
                parameters.NameClaimType =
                    AppAuthorizeClaimTypes.TenantId;
                break;
            case "role-tenant":
                parameters.RoleClaimType =
                    AppAuthorizeClaimTypes.TenantId;
                break;
        }

        var exception = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddJwtAuthServices(parameters));

        Assert.Equal("tokenValidationParameters", exception.ParamName);
    }

    [Theory]
    [InlineData("iss")]
    [InlineData("aud")]
    [InlineData("exp")]
    [InlineData("iat")]
    public void AddJwtAuthServices_ShouldLeaveRegisteredClaimSelectionToConsumer(
        string claimType)
    {
        var parameters = CreateParameters();
        parameters.NameClaimType = claimType;

        new ServiceCollection().AddJwtAuthServices(parameters);
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRejectWeakRsaSigningKey()
    {
        using var rsa = RSA.Create(1024);
        var parameters = new TokenValidationParameters
        {
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true
        };

        Assert.Throws<ArgumentException>(() =>
            new ServiceCollection().AddJwtAuthServices(parameters));
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRejectPublicOnlyRsaSigningKey()
    {
        using var rsa = RSA.Create(2048);
        using var publicRsa = RSA.Create();
        publicRsa.ImportParameters(rsa.ExportParameters(false));
        var parameters = new TokenValidationParameters
        {
            IssuerSigningKey = new RsaSecurityKey(publicRsa),
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true
        };

        Assert.Throws<ArgumentException>(() =>
            new ServiceCollection().AddJwtAuthServices(parameters));
    }

    [Fact]
    public void AddJwtAuthServices_ShouldAdaptX509EcdsaKeyForValidation()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(
            "CN=MitMediator.AppAuthorize.Jwt.Tests",
            ecdsa,
            HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(10));
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "issuer",
            ValidAudience = "audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new X509SecurityKey(certificate)
        };
        var services = new ServiceCollection();

        services.AddJwtAuthServices(parameters);
        var provider = services.BuildServiceProvider();
        var bearerParameters =
            provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme)
                .TokenValidationParameters;

        Assert.IsType<ECDsaSecurityKey>(
            bearerParameters.IssuerSigningKey);
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRejectAlgorithmConfigurationThatCannotAcceptIssuedTokens()
    {
        var services = new ServiceCollection();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                "key12345678901234567890123456789012"u8.ToArray()),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha512]
        };

        var exception = Assert.Throws<ArgumentException>(
            () => services.AddJwtAuthServices(parameters));

        Assert.Contains(
            SecurityAlgorithms.HmacSha256,
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRejectConflictingRepeatedRegistration()
    {
        var services = new ServiceCollection();
        services.AddJwtAuthServices(
            "first-key1234567890123456789012345",
            "first-issuer",
            "first-audience");

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddJwtAuthServices(
                "second-key123456789012345678901234",
                "second-issuer",
                "second-audience"));

        Assert.Contains("already configured", exception.Message);
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRejectEquivalentRepeatedRegistration()
    {
        var services = new ServiceCollection();
        services.AddJwtAuthServices(
            "same-key12345678901234567890123456",
            "issuer",
            "audience");

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddJwtAuthServices(
                "same-key12345678901234567890123456",
                "issuer",
                "audience"));

        Assert.Contains("already configured", exception.Message);
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRejectSameTokenValidationParametersInstance()
    {
        var services = new ServiceCollection();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "issuer",
            ValidAudience = "audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                "key12345678901234567890123456789012"u8.ToArray())
        };
        services.AddJwtAuthServices(parameters);

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddJwtAuthServices(parameters));

        Assert.Contains("already configured", exception.Message);
    }

    [Fact]
    public void AddJwtAuthServices_ShouldAllowDedicatedTokenIssuerAndAudiences()
    {
        var services = new ServiceCollection();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "IssuingIssuer",
            ValidAudiences = ["TrustedAudience", "IssuingAudience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                "key12345678901234567890123456789012"u8.ToArray())
        };

        services.AddJwtAuthServices(
            parameters,
            tokenIssuer: "IssuingIssuer",
            tokenAudiences: ["TrustedAudience", "IssuingAudience"]);
        var provider = services.BuildServiceProvider();
        var configuration =
            provider.GetRequiredService<JwtTokenConfiguration>();

        Assert.Equal("IssuingIssuer", configuration.TokenIssuer);
        Assert.Equal(
            ["TrustedAudience", "IssuingAudience"],
            configuration.TokenAudiences);
    }

    [Fact]
    public void AddJwtAuthServices_ShouldRejectValidIssuersCollection()
    {
        var parameters = new TokenValidationParameters
        {
            ValidIssuers = ["TestIssuer"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                "key12345678901234567890123456789012"u8.ToArray())
        };

        var exception = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddJwtAuthServices(parameters));

        Assert.Contains("ValidIssuer", exception.Message);
    }

    [Theory]
    [InlineData(nameof(TokenValidationParameters.RequireSignedTokens))]
    [InlineData(nameof(TokenValidationParameters.ValidateIssuerSigningKey))]
    [InlineData(nameof(TokenValidationParameters.ValidateLifetime))]
    [InlineData(nameof(TokenValidationParameters.ValidateIssuer))]
    [InlineData(nameof(TokenValidationParameters.ValidateAudience))]
    public void AddJwtAuthServices_ShouldRejectDisabledSecurityValidation(
        string parameterName)
    {
        var parameters = CreateParameters();
        switch (parameterName)
        {
            case nameof(TokenValidationParameters.RequireSignedTokens):
                parameters.RequireSignedTokens = false;
                break;
            case nameof(TokenValidationParameters.ValidateIssuerSigningKey):
                parameters.ValidateIssuerSigningKey = false;
                break;
            case nameof(TokenValidationParameters.ValidateLifetime):
                parameters.ValidateLifetime = false;
                break;
            case nameof(TokenValidationParameters.ValidateIssuer):
                parameters.ValidateIssuer = false;
                break;
            case nameof(TokenValidationParameters.ValidateAudience):
                parameters.ValidateAudience = false;
                break;
        }

        var exception = Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddJwtAuthServices(parameters));

        Assert.Equal("tokenValidationParameters", exception.ParamName);
    }

    [Fact]
    public void AddDefaultAuthContext_ShouldRegisterIAuthenticationContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDefaultAuthContext();
        var provider = services.BuildServiceProvider();

        // Assert
        var context = provider.GetService<IAuthenticationContext>();
        Assert.NotNull(context);
        Assert.IsType<DefaultAuthenticationContext>(context);
    }

    [Fact]
    public void AddDefaultAuthContext_ShouldPreserveExistingContext()
    {
        var services = new ServiceCollection();
        services.AddTransient<IAuthenticationContext, ExistingContext>();

        services.AddDefaultAuthContext();
        var provider = services.BuildServiceProvider();

        Assert.IsType<ExistingContext>(
            provider.GetRequiredService<IAuthenticationContext>());
    }

    [Fact]
    public void AddDefaultAuthContext_ShouldNotRegisterDuplicates()
    {
        var services = new ServiceCollection();

        services.AddDefaultAuthContext();
        services.AddDefaultAuthContext();

        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IAuthenticationContext));
    }

    private sealed class ExistingContext : IAuthenticationContext
    {
        public bool IsAuthenticated => true;

        public string? UserId => "user-id";

        public string? UserName => null;

        public string[]? UserRoles => null;

        public IReadOnlyDictionary<string, IReadOnlyList<string>>? Claims =>
            null;

        public string[]? TenantIds => null;
    }

    private static TokenValidationParameters CreateParameters() =>
        new()
        {
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                "key12345678901234567890123456789012"u8.ToArray())
        };
}