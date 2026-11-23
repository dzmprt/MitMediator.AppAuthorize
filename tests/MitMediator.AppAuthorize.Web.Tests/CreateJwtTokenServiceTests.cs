using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MitMediator.AppAuthorize.Web.Tests;

public class CreateJwtTokenServiceTests
{
    private static JwtTokenConfiguration CreateTestConfig()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("supersecurekey12345678901234567890"));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TestIssuer",
            ValidAudience = "TestAudience",
            IssuerSigningKey = key
        };

        return new JwtTokenConfiguration(parameters, jwtTokenLifeSeconds: 600, refreshTokenLifeSeconds: 3600);
    }

    [Fact]
    public void CreateJwtToken_ShouldGenerateToken_WithMinimalClaims()
    {
        // Arrange
        var config = CreateTestConfig();
        var service = new CreateJwtTokenService(config);
        var user = new UserInfo("user123", null);

        // Act
        var token = service.CreateJwtToken(user, DateTime.UtcNow.AddMinutes(10));

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user123");
    }

    [Fact]
    public void CreateJwtToken_ShouldIncludeAllClaims_WhenProvided()
    {
        // Arrange
        var config = CreateTestConfig();
        var service = new CreateJwtTokenService(config);
        var user = new UserInfo("user123", "Dmitriy", new[] { "Admin", "User" }, new[] { "TenantA", "TenantB" });

        // Act
        var token = service.CreateJwtToken(user, DateTime.UtcNow.AddMinutes(10));

        // Assert
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user123");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Name && c.Value == "Dmitriy");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "User");
        Assert.Contains(jwt.Claims, c => c.Type == AppAuthorizeClaimTypes.TenantId && c.Value == "TenantA");
        Assert.Contains(jwt.Claims, c => c.Type == AppAuthorizeClaimTypes.TenantId && c.Value == "TenantB");
    }

    [Fact]
    public void CreateJwtToken_ShouldUseConfiguredNameAndRoleClaimTypes()
    {
        var config = CreateTestConfig();
        config.TokenValidationParameters.NameClaimType = "custom-name";
        config.TokenValidationParameters.RoleClaimType = "custom-role";
        var service = new CreateJwtTokenService(config);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(
            service.CreateJwtToken(
                new UserInfo("user123", "User", ["Admin"]),
                DateTime.UtcNow.AddMinutes(10)));

        Assert.Contains(
            jwt.Claims,
            claim =>
                claim.Type == "custom-name" &&
                claim.Value == "User");
        Assert.Contains(
            jwt.Claims,
            claim =>
                claim.Type == "custom-role" &&
                claim.Value == "Admin");
    }

    [Fact]
    public void CreateJwtToken_ShouldNotIncludeBlankOrDuplicateAuthorizationClaims()
    {
        var service = new CreateJwtTokenService(CreateTestConfig());
        var user = new UserInfo(
            "user123",
            " ",
            ["Admin", "Admin", " "],
            ["TenantA", "TenantA", ""]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(
            service.CreateJwtToken(
                user,
                DateTime.UtcNow.AddMinutes(10)));

        Assert.DoesNotContain(
            jwt.Claims,
            claim => claim.Type == ClaimTypes.Name);
        Assert.Single(
            jwt.Claims,
            claim => claim.Type == ClaimTypes.Role &&
                     claim.Value == "Admin");
        Assert.Single(
            jwt.Claims,
            claim =>
                claim.Type == AppAuthorizeClaimTypes.TenantId &&
                claim.Value == "TenantA");
    }

    [Fact]
    public void CreateJwtToken_ShouldSupportRsaSigningKey()
    {
        using var rsa = RSA.Create(2048);
        var securityKey = new RsaSecurityKey(rsa);
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TestIssuer",
            ValidAudience = "TestAudience",
            IssuerSigningKey = securityKey
        };
        var service = new CreateJwtTokenService(
            new JwtTokenConfiguration(parameters, 600, 3600));

        var token = service.CreateJwtToken(
            new UserInfo("user123", "User"),
            DateTime.UtcNow.AddMinutes(10));

        var principal = new JwtSecurityTokenHandler().ValidateToken(
            token,
            parameters,
            out _);
        Assert.Equal("user123", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void CreateJwtToken_ShouldSupportEcdsaSigningKey()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var securityKey = new ECDsaSecurityKey(ecdsa);
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TestIssuer",
            ValidAudience = "TestAudience",
            IssuerSigningKey = securityKey
        };
        var service = new CreateJwtTokenService(
            new JwtTokenConfiguration(parameters, 600, 3600));

        var token = service.CreateJwtToken(
            new UserInfo("user123", "User"),
            DateTimeOffset.UtcNow.AddMinutes(10));

        var principal = new JwtSecurityTokenHandler().ValidateToken(
            token,
            parameters,
            out _);
        Assert.Equal(
            "user123",
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(
            SecurityAlgorithms.EcdsaSha256,
            new JwtSecurityTokenHandler().ReadJwtToken(token).SignatureAlgorithm);
    }

    [Fact]
    public void CreateJwtToken_ShouldSupportX509SigningKey()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=MitMediator.AppAuthorize.Tests",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(10));
        var securityKey = new X509SecurityKey(certificate);
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TestIssuer",
            ValidAudience = "TestAudience",
            IssuerSigningKey = securityKey
        };
        var service = new CreateJwtTokenService(
            new JwtTokenConfiguration(parameters, 600, 3600));

        var token = service.CreateJwtToken(
            new UserInfo("user123", "User"),
            DateTime.UtcNow.AddMinutes(5));

        var principal = new JwtSecurityTokenHandler().ValidateToken(
            token,
            parameters,
            out _);
        Assert.Equal("user123", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public void CreateJwtToken_ShouldSupportRepeatedSigningWithX509EcdsaKey()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var request = new CertificateRequest(
            "CN=MitMediator.AppAuthorize.Ecdsa.Tests",
            ecdsa,
            HashAlgorithmName.SHA384);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(10));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TestIssuer",
            ValidAudience = "TestAudience",
            IssuerSigningKey = new X509SecurityKey(certificate)
        };
        var service = new CreateJwtTokenService(
            new JwtTokenConfiguration(parameters, 600, 3600));

        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        var firstToken = service.CreateJwtToken(
            new UserInfo("user123", "First User"),
            expiresAt);
        var secondToken = service.CreateJwtToken(
            new UserInfo("user456", "Second User"),
            expiresAt);
        using var validationEcdsa = ECDsa.Create();
        using var publicKey = certificate.GetECDsaPublicKey()!;
        validationEcdsa.ImportSubjectPublicKeyInfo(publicKey.ExportSubjectPublicKeyInfo(), out _);
        var validationParameters = parameters.Clone();
        validationParameters.IssuerSigningKey =
            new ECDsaSecurityKey(validationEcdsa);

        var handler = new JwtSecurityTokenHandler();
        foreach (var (token, userId) in new[] { (firstToken, "user123"), (secondToken, "user456") })
        {
            var principal = handler.ValidateToken(token, validationParameters, out _);
            Assert.Equal(userId, principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal(SecurityAlgorithms.EcdsaSha384, handler.ReadJwtToken(token).SignatureAlgorithm);
        }
    }

    [Fact]
    public void CreateJwtToken_ShouldUseAllConfiguredAudiences()
    {
        var parameters = CreateTestConfig().TokenValidationParameters;
        parameters.ValidAudiences = ["SecondAudience", "ThirdAudience"];
        var service = new CreateJwtTokenService(
            new JwtTokenConfiguration(parameters, 600, 3600));

        var token = service.CreateJwtToken(
            new UserInfo("user123", "User"),
            DateTime.UtcNow.AddMinutes(10));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Equal(
            ["TestAudience", "SecondAudience", "ThirdAudience"],
            jwt.Audiences);
    }

    [Fact]
    public void CreateJwtToken_ShouldUseDedicatedIssuerAndAudience()
    {
        var parameters = CreateTestConfig().TokenValidationParameters;
        parameters.ValidIssuer = "IssuingIssuer";
        parameters.ValidAudiences = ["FirstAudience", "IssuingAudience"];
        var service = new CreateJwtTokenService(
            new JwtTokenConfiguration(
                parameters,
                600,
                3600,
                "IssuingIssuer",
                ["FirstAudience", "IssuingAudience"]));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(
            service.CreateJwtToken(
                new UserInfo("user123", "User"),
                DateTime.UtcNow.AddMinutes(10)));

        Assert.Equal("IssuingIssuer", jwt.Issuer);
        Assert.Equal(
            ["FirstAudience", "IssuingAudience"],
            jwt.Audiences);
    }

    [Fact]
    public void CreateJwtToken_ShouldIncludeIssuedAtClaim()
    {
        var service = new CreateJwtTokenService(CreateTestConfig());
        var startedAt = DateTimeOffset.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(
            service.CreateJwtToken(
                new UserInfo("user123", "User"),
                startedAt.AddMinutes(10)));

        Assert.InRange(
            new DateTimeOffset(jwt.Payload.IssuedAt).ToUnixTimeSeconds(),
            startedAt.ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task JwtBearer_ShouldAcceptFreshToken()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJwtAuthServices(
            "supersecurekey12345678901234567890",
            "TestIssuer",
            "TestAudience");
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<CreateJwtTokenService>();
        var token = tokenService.CreateJwtToken(
            new UserInfo("user123", "User"),
            DateTimeOffset.UtcNow.AddMinutes(10));
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Headers.Authorization = $"Bearer {token}";

        var result = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        Assert.Null(result.Failure);
        Assert.True(result.Succeeded);
    }

}