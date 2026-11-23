using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MitMediator.AppAuthorize.ClientMediator;
using MitMediator.AppAuthorize.Domain;
using MitMediator.AppAuthorize.Exceptions;
using MitMediator.AppAuthorize.Web.Models;
using MitMediator.AutoApi.HttpMediator;
using Moq;
using JwtTokenModel = MitMediator.AppAuthorize.Web.Models.JwtTokenModel;

namespace MitMediator.AppAuthorize.Web.Tests;

public class AuthApiTests
{
    private const string SigningKey =
        "key1234567890098765433123123123232323";

    private static WebApplicationBuilder CreateBuilder()
    {
        return CreateBuilder(null);
    }

    private static WebApplicationBuilder CreateBuilder(
        AuthInputLimits? inputLimits)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddJwtAuthServices(
            SigningKey,
            "test-issuer",
            "test-audience",
            inputLimits: inputLimits);
        return builder;
    }

    private static async Task<HttpClient> StartAppAsync(
        WebApplicationBuilder builder,
        bool usePassword = true,
        bool useCode = false,
        bool useRefreshToken = true)
    {
        var app = builder.Build();
        app.UseAuthException();
        app.MapAuthApi(usePassword, useCode, useRefreshToken);
        await app.StartAsync();
        return app.GetTestClient();
    }

    [Fact]
    public async Task PasswordEndpoint_ShouldCreateTokens()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(a => a.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var repository = CreateRepositoryMock();
        var builder = Configure(authenticator.Object, repository.Object);
        var client = await StartAppAsync(
            builder);

        var startedAt = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "password" });
        var completedAt = DateTimeOffset.UtcNow;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var model = await response.Content.ReadFromJsonAsync<JwtTokenModel>();
        Assert.Equal("refresh-token", model!.RefreshToken);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(model.JwtToken);
        Assert.InRange(
            jwt.Payload.Expiration!.Value,
            startedAt.AddSeconds(600).ToUnixTimeSeconds(),
            completedAt.AddSeconds(600).ToUnixTimeSeconds());
        var refreshExpiresAt = Assert.IsType<DateTimeOffset>(model.RefreshTokenExpires);
        Assert.InRange(
            refreshExpiresAt,
            startedAt.AddSeconds(2592000),
            completedAt.AddSeconds(2592000));
        repository.Verify(repository => repository.AddAsync(
            "user-id",
            refreshExpiresAt,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PasswordEndpoint_ShouldNotRequireOrIssueRefreshToken_WhenDisabled()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator.Object);
        var client = await StartAppAsync(
            builder,
            useRefreshToken: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "password" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var model = await response.Content
            .ReadFromJsonAsync<JwtTokenModel>();
        Assert.Null(model!.RefreshToken);
        Assert.Null(model.RefreshTokenExpires);
    }

    [Fact]
    public async Task CodeEndpoint_ShouldNotRequireOrIssueRefreshToken_WhenDisabled()
    {
        var authenticator = new Mock<IUserAuthenticatorByCode>();
        authenticator
            .Setup(service => service.AuthByCodeAsync(
                "login",
                "code",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator.Object);
        var client = await StartAppAsync(
            builder,
            usePassword: false,
            useCode: true,
            useRefreshToken: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-code",
            new { login = "login", code = "code" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var model = await response.Content
            .ReadFromJsonAsync<JwtTokenModel>();
        Assert.Null(model!.RefreshToken);
        Assert.Null(model.RefreshTokenExpires);
    }

    [Fact]
    public async Task MapAuthApi_ShouldUseConfiguredRoutePrefix()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator.Object);
        await using var app = builder.Build();
        app.UseAuthException();
        app.MapAuthApi(
            usePassword: true,
            useCode: false,
            useRefreshToken: false,
            routePrefix: "/custom/auth/");
        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/custom/auth/by-password",
            new { login = "login", password = "password" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MapAuthApi_ShouldRejectBlankRoutePrefix()
    {
        var app = CreateBuilder().Build();

        var exception = Assert.Throws<ArgumentException>(
            () => app.MapAuthApi(routePrefix: " / "));

        Assert.Equal("routePrefix", exception.ParamName);
        await app.DisposeAsync();
    }

    [Theory]
    [InlineData("api//identity")]
    [InlineData("{tenant}/identity")]
    [InlineData("identity?query")]
    [InlineData("identity#fragment")]
    [InlineData(@"api\identity")]
    [InlineData("api identity")]
    [InlineData("identity:*")]
    [InlineData(".")]
    [InlineData("api/..")]
    [InlineData("identity\n")]
    public async Task MapAuthApi_ShouldRejectUnsafeRoutePrefix(
        string routePrefix)
    {
        var app = CreateBuilder().Build();

        var exception = Assert.Throws<ArgumentException>(
            () => app.MapAuthApi(routePrefix: routePrefix));

        Assert.Equal("routePrefix", exception.ParamName);
        await app.DisposeAsync();
    }

    [Fact]
    public async Task MapAuthApi_ShouldWarn_WhenRateLimitPolicyIsMissing()
    {
        var logger = new CollectingLogger();
        var builder = CreateBuilder();
        builder.Services.AddSingleton<ILoggerFactory>(
            new LoggerFactory([new SingleLoggerProvider(logger)]));
        await using var app = builder.Build();

        app.MapAuthApi(
            usePassword: true,
            useCode: false,
            useRefreshToken: false);

        Assert.Contains(
            logger.WarningMessages,
            message => message.Contains(
                "without a rate-limiting policy",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task PasswordEndpoint_ShouldUseConfiguredInputLimits()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        var builder = CreateBuilder(
            new AuthInputLimits(loginLength: 4));
        builder.Services.AddSingleton(authenticator.Object);
        var client = await StartAppAsync(
            builder,
            useRefreshToken: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "12345", password = "password" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        authenticator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PasswordEndpoint_ShouldUseDefaultInputLimits()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator.Object);
        var client = await StartAppAsync(
            builder,
            useRefreshToken: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new
            {
                login = "login",
                password = new string('x', 1025)
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        authenticator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HttpMediator_ShouldRoundTripPasswordAuthentication()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var builder = Configure(
            authenticator.Object,
            CreateRepositoryMock().Object);
        var httpClient = await StartAppAsync(
            builder);
        await using var provider = new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new TestHttpClientFactory(httpClient))
            .AddScoped<IClientMediator, HttpMediator>(
                services => new HttpMediator(
                    services,
                    null,
                    "test"))
            .BuildServiceProvider();
        var mediator =
            provider.GetRequiredService<IClientMediator>();

        var token = await mediator.GetJwtByPasswordAsync(
            "login",
            "password");

        Assert.Equal("user-id", token.UserId);
        Assert.Equal("refresh-token", token.RefreshToken);
        Assert.True(token.JwtTokenExpires > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task HttpMediator_ShouldRoundTripCustomNestedRoutePrefix()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var builder = Configure(
            authenticator.Object,
            CreateRepositoryMock().Object);
        await using var app = builder.Build();
        app.UseAuthException();
        app.MapAuthApi(
            usePassword: true,
            useCode: false,
            useRefreshToken: false,
            routePrefix: "api/identity");
        await app.StartAsync();
        var client = app.GetTestClient();
        await using var provider = new ServiceCollection()
            .AddSingleton<IHttpClientFactory>(
                new TestHttpClientFactory(client))
            .AddScoped<IClientMediator, HttpMediator>(
                services => new HttpMediator(
                    services,
                    null,
                    "test"))
            .BuildServiceProvider();

        var token =
            await provider.GetRequiredService<IClientMediator>()
                .GetJwtByPasswordAsync(
                    "login",
                    "password",
                    "/api/identity/");

        Assert.Equal("user-id", token.UserId);
    }

    [Fact]
    public async Task AuthEndpoints_ShouldAllowAnonymous_WithFallbackPolicy()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(a => a.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var builder = CreateBuilder();
        builder.Services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        builder.Services.AddSingleton(authenticator.Object);
        builder.Services.AddSingleton(CreateRepositoryMock().Object);
        await using var app = builder.Build();
        app.UseAuthException();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapAuthApi(
            usePassword: true,
            useCode: false,
            useRefreshToken: false);
        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "password" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MapAuthApi_ShouldSupportRouteGroups()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var builder = Configure(
            authenticator.Object,
            CreateRepositoryMock().Object);
        await using var app = builder.Build();
        app.UseAuthException();
        app.MapGroup("/api").MapAuthApi(
            useCode: false,
            useRefreshToken: false);
        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/by-password",
            new { login = "login", password = "password" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MapAuthApi_ShouldNotReserveGlobalEndpointNames()
    {
        var builder = CreateBuilder();
        await using var app = builder.Build();
        app.MapGroup("/first").MapAuthApi(
            usePassword: true,
            useCode: false,
            useRefreshToken: false);
        app.MapGroup("/second").MapAuthApi(
            usePassword: true,
            useCode: false,
            useRefreshToken: false);
        await app.StartAsync();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Where(endpoint =>
                endpoint is RouteEndpoint route &&
                route.RoutePattern.RawText?.Contains(
                    "by-password",
                    StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Equal(2, endpoints.Length);
        Assert.All(
            endpoints,
            endpoint =>
                Assert.Null(
                    endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()));
    }

    [Fact]
    public async Task PasswordEndpoint_ShouldDisableResponseCaching()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var client = await StartAppAsync(
            Configure(
                authenticator.Object,
                CreateRepositoryMock().Object));

        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "password" });

        Assert.Equal(
            "no-store, no-cache",
            response.Headers.GetValues("Cache-Control").Single());
        Assert.Equal("no-cache", response.Headers.GetValues("Pragma").Single());
        Assert.Equal("0", response.Content.Headers.GetValues("Expires").Single());
    }

    [Fact]
    public async Task PasswordEndpoint_ShouldDisableResponseCaching_OnError()
    {
        var client = await StartAppAsync(
            Configure(
                Mock.Of<IUserAuthenticator>(),
                CreateRepositoryMock().Object),
            useRefreshToken: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "invalid" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(
            "no-store, no-cache",
            response.Headers.GetValues("Cache-Control").Single());
        Assert.Equal("no-cache", response.Headers.GetValues("Pragma").Single());
        Assert.Equal("0", response.Content.Headers.GetValues("Expires").Single());
    }

    [Fact]
    public async Task CodeEndpoint_ShouldCreateTokens()
    {
        var authenticator = new Mock<IUserAuthenticatorByCode>();
        authenticator
            .Setup(a => a.AuthByCodeAsync(
                "login",
                "1234",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator.Object);
        builder.Services.AddSingleton(CreateRepositoryMock().Object);
        var client = await StartAppAsync(
            builder,
            usePassword: false,
            useCode: true,
            useRefreshToken: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-code",
            new { login = "login", code = "1234" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("login", null)]
    [InlineData(null, "password")]
    public async Task PasswordEndpoint_ShouldReturn400_ForMissingCredentials(
        string? login,
        string? password)
    {
        var authenticator = new Mock<IUserAuthenticator>();
        var client = await StartAppAsync(
            Configure(authenticator.Object, CreateRepositoryMock().Object),
            useRefreshToken: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        authenticator.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("{")]
    [InlineData("""{"login":42,"password":"password"}""")]
    public async Task PasswordEndpoint_ShouldReturn400_ForMalformedJson(
        string requestBody)
    {
        var authenticator = new Mock<IUserAuthenticator>();
        var client = await StartAppAsync(
            Configure(authenticator.Object, CreateRepositoryMock().Object),
            useRefreshToken: false);

        var response = await client.PostAsync(
            "/auth/by-password",
            new StringContent(
                requestBody,
                Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        authenticator.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, HttpStatusCode.ServiceUnavailable)]
    [InlineData(StatusCodes.Status429TooManyRequests, HttpStatusCode.TooManyRequests)]
    public async Task AuthEndpoint_ShouldReturnAndAdvertiseRateLimitStatus(
        int? rejectionStatusCode,
        HttpStatusCode expectedStatus)
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator.Object);
        builder.Services.AddRateLimiter(options =>
        {
            if (rejectionStatusCode is int statusCode)
            {
                options.RejectionStatusCode = statusCode;
            }

            options.AddFixedWindowLimiter(
                "auth",
                limiter =>
                {
                    limiter.PermitLimit = 1;
                    limiter.Window = TimeSpan.FromMinutes(1);
                    limiter.QueueLimit = 0;
                });
        });
        await using var app = builder.Build();
        app.UseAuthException();
        app.UseRateLimiter();
        app.MapAuthApi(
            usePassword: true,
            useCode: false,
            useRefreshToken: false,
            rateLimitPolicy: "auth");
        await app.StartAsync();
        var client = app.GetTestClient();

        var first = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "password" });
        var second = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "password" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(expectedStatus, second.StatusCode);
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(endpoint =>
                endpoint is RouteEndpoint route &&
                route.RoutePattern.RawText == "auth/by-password");
        var responseMetadata = endpoint.Metadata
            .GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Contains(
            responseMetadata,
            metadata => metadata.StatusCode == (int)expectedStatus);
        authenticator.Verify(
            service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void WebCommands_ShouldBeInternal()
    {
        Type[] commandTypes =
        [
            typeof(Models.Requests.CreateJwtByPasswordCommand),
            typeof(Models.Requests.CreateJwtByCodeCommand),
            typeof(Models.Requests.CreateJwtByRefreshTokenCommand)
        ];

        Assert.All(commandTypes, type => Assert.False(type.IsVisible));
    }

    [Fact]
    public async Task PasswordEndpoint_ShouldReturn401_WithoutLeakingMessage()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(a => a.AuthByPasswordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("user database detail"));
        var client = await StartAppAsync(
            Configure(authenticator.Object, CreateRepositoryMock().Object),
            useRefreshToken: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "invalid" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("database", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("credentials are invalid", body);
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldRejectMissingToken()
    {
        var repository = new Mock<IRefreshTokenRepository>();
        repository
            .Setup(r => r.RotateAsync(
                "missing",
                "user",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenRotation?)null);
        var builder = CreateBuilder();
        builder.Services.AddSingleton(repository.Object);
        var client = await StartAppAsync(
            builder,
            usePassword: false,
            useCode: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-refresh",
            new { refreshTokenKey = "missing", userId = "user" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldNormalizeUserId()
    {
        var repository = new Mock<IRefreshTokenRepository>();
        repository
            .Setup(service => service.RotateAsync(
                "refresh-token",
                "user-id",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenRotation?)null);
        var builder = CreateBuilder();
        builder.Services.AddSingleton(repository.Object);
        var client = await StartAppAsync(
            builder,
            usePassword: false,
            useCode: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-refresh",
            new
            {
                refreshTokenKey = "refresh-token",
                userId = " user-id "
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldRejectMismatchedUser()
    {
        var repository = CreateRepositoryMock();
        repository
            .Setup(r => r.RotateAsync(
                "refresh-token",
                "attacker",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenRotation?)null);
        var builder = CreateBuilder();
        builder.Services.AddSingleton(repository.Object);
        var client = await StartAppAsync(
            builder,
            usePassword: false,
            useCode: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-refresh",
            new
            {
                refreshTokenKey = "refresh-token",
                userId = "attacker"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldConsumeTokenOnlyOnce()
    {
        var repository = new ConcurrentRepository();
        var builder = CreateBuilder();
        builder.Services.AddSingleton<IRefreshTokenRepository>(repository);
        var client = await StartAppAsync(
            builder,
            usePassword: false,
            useCode: false);

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync(
                "/auth/by-refresh",
                new { refreshTokenKey = "refresh-token", userId = "user-id" }),
            client.PostAsJsonAsync(
                "/auth/by-refresh",
                new { refreshTokenKey = "refresh-token", userId = "user-id" }));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(
            responses,
            r => r.StatusCode == HttpStatusCode.Unauthorized);
        Assert.Equal(1, repository.RotationCount);
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldReturn400_ForMissingInput()
    {
        var repository = new Mock<IRefreshTokenRepository>();
        var builder = CreateBuilder();
        builder.Services.AddSingleton(repository.Object);
        var client = await StartAppAsync(
            builder,
            usePassword: false,
            useCode: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-refresh",
            new { userId = "user-id" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PasswordEndpoint_ShouldUseRepositoryRefreshToken()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var repository = CreateRepositoryMock();
        repository
            .Setup(service => service.AddAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                (string _, DateTimeOffset _, CancellationToken _) =>
                    ValueTask.FromResult(
                        new RefreshToken(
                            "repository-token",
                            "repository-user",
                            DateTimeOffset.MinValue)));
        var client = await StartAppAsync(
            Configure(authenticator.Object, repository.Object));

        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "password" });
        var model = await response.Content.ReadFromJsonAsync<JwtTokenModel>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            new JwtSecurityTokenHandler().ReadJwtToken(model!.JwtToken).Claims,
            claim =>
                claim.Type == System.Security.Claims.ClaimTypes.NameIdentifier &&
                claim.Value == "user-id");
        Assert.Equal("repository-token", model.RefreshToken);
        Assert.Equal(DateTimeOffset.MinValue, model.RefreshTokenExpires);
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldUseRepositoryUserAndToken()
    {
        var repository = new Mock<IRefreshTokenRepository>();
        repository
            .Setup(service => service.RotateAsync(
                "refresh-token",
                "user-id",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RefreshTokenRotation(
                    new UserInfo("repository-user", "User"),
                    new RefreshToken(
                        "repository-token",
                        "repository-user",
                        DateTimeOffset.MinValue)));
        var builder = CreateBuilder();
        builder.Services.AddSingleton(repository.Object);
        var client = await StartAppAsync(
            builder,
            usePassword: false,
            useCode: false);

        var response = await client.PostAsJsonAsync(
            "/auth/by-refresh",
            new
            {
                refreshTokenKey = "refresh-token",
                userId = "user-id"
            });
        var model = await response.Content.ReadFromJsonAsync<JwtTokenModel>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            new JwtSecurityTokenHandler().ReadJwtToken(model!.JwtToken).Claims,
            claim =>
                claim.Type == System.Security.Claims.ClaimTypes.NameIdentifier &&
                claim.Value == "repository-user");
        Assert.Equal("repository-token", model.RefreshToken);
        Assert.Equal(DateTimeOffset.MinValue, model.RefreshTokenExpires);
    }

    [Fact]
    public async Task PasswordEndpoint_ShouldUseFreshTimeAfterSlowRepository()
    {
        var repositoryCompletedAt = DateTimeOffset.MinValue;
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var repository = new Mock<IRefreshTokenRepository>();
        repository
            .Setup(service => service.AddAsync(
                "user-id",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                async (string userId, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
                {
                    // JWT timestamps have whole-second precision.
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    repositoryCompletedAt = DateTimeOffset.UtcNow;
                    return new RefreshToken("refresh-token", userId, expiresAt);
                });
        var builder = Configure(authenticator.Object, repository.Object);
        var client = await StartAppAsync(builder);

        var response = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "password" });

        var model = await response.Content.ReadFromJsonAsync<JwtTokenModel>();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(model!.JwtToken);
        Assert.NotEqual(DateTimeOffset.MinValue, repositoryCompletedAt);
        Assert.InRange(
            jwt.Payload.Expiration!.Value,
            repositoryCompletedAt.AddMinutes(10).ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("role")]
    [InlineData("roles")]
    public async Task JwtFlow_ShouldAuthorizeWithConfiguredRoleClaimType(
        string? roleClaimType)
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator
            .Setup(service => service.AuthByPasswordAsync(
                "login",
                "password",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo(
                "user-id",
                "User",
                ["Admin"],
                ["TenantA"]));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey))
        };
        if (roleClaimType is not null)
        {
            parameters.RoleClaimType = roleClaimType;
        }

        builder.Services.AddJwtAuthServices(parameters);

        builder.Services.AddSingleton(authenticator.Object);
        builder.Services.AddDefaultAuthContext();
        builder.Services.AddAppAuthorize();
        await using var app = builder.Build();
        app.UseAuthException();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapAuthApi(
            usePassword: true,
            useCode: false,
            useRefreshToken: false);
        app.MapGet(
            "/protected",
            async (IServiceProvider services) =>
            {
                var behavior =
                    services.GetRequiredService<
                        IPipelineBehavior<JwtProtectedRequest, string>>();
                var next = new TestNextHandler();
                return await behavior.HandleAsync(
                    new JwtProtectedRequest(),
                    next,
                    CancellationToken.None);
            });
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/by-password",
            new { login = "login", password = "password" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var tokens =
            await loginResponse.Content.ReadFromJsonAsync<JwtTokenModel>();
        Assert.NotNull(tokens);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.JwtToken);

        using var protectedResponse = await client.GetAsync("/protected");

        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
        Assert.Equal(
            "authorized:user-id",
            await protectedResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task JwtFlow_ShouldRejectInvalidAndExpiredTokens()
    {
        var builder = CreateBuilder();
        builder.Services.AddDefaultAuthContext();
        builder.Services.AddAppAuthorize();
        await using var app = builder.Build();
        app.UseAuthException();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet(
            "/protected",
            async (IServiceProvider services) =>
            {
                var behavior =
                    services.GetRequiredService<
                        IPipelineBehavior<JwtProtectedRequest, string>>();
                return await behavior.HandleAsync(
                    new JwtProtectedRequest(),
                    new TestNextHandler(),
                    CancellationToken.None);
            });
        await app.StartAsync();
        var client = app.GetTestClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-jwt");
        var invalidResponse = await client.GetAsync("/protected");

        var expiredToken =
            app.Services.GetRequiredService<CreateJwtTokenService>()
                .CreateJwtToken(
                    new UserInfo("user-id", "User", ["Admin"], ["TenantA"]),
                    DateTimeOffset.UtcNow.AddMinutes(-10));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);
        var expiredResponse = await client.GetAsync("/protected");

        Assert.Equal(HttpStatusCode.Unauthorized, invalidResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, expiredResponse.StatusCode);
    }

    [Theory]
    [InlineData(false, -60, 60, 0, HttpStatusCode.OK)]
    [InlineData(false, -120, -60, 0, HttpStatusCode.Unauthorized)]
    [InlineData(false, 60, 120, 0, HttpStatusCode.Unauthorized)]
    [InlineData(true, -60, 60, 0, HttpStatusCode.OK)]
    [InlineData(true, -120, -60, 0, HttpStatusCode.Unauthorized)]
    [InlineData(true, 60, 120, 0, HttpStatusCode.Unauthorized)]
    [InlineData(true, -120, -60, 120, HttpStatusCode.OK)]
    public async Task JwtFlow_ShouldValidateSigningCertificateLifetime(
        bool useEcdsa,
        int notBeforeMinutes,
        int notAfterMinutes,
        int clockSkewMinutes,
        HttpStatusCode expectedStatus)
    {
        using var ecdsa = useEcdsa
            ? ECDsa.Create(ECCurve.NamedCurves.nistP256)
            : null;
        using var rsa = useEcdsa ? null : RSA.Create(2048);
        const string subject = "CN=MitMediator.AppAuthorize.Jwt.Tests";
        var certificateRequest = ecdsa is not null
            ? new CertificateRequest(subject, ecdsa, HashAlgorithmName.SHA256)
            : new CertificateRequest(
                subject, rsa!, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var now = DateTimeOffset.UtcNow;
        using var certificate = certificateRequest.CreateSelfSigned(
            now.AddMinutes(notBeforeMinutes),
            now.AddMinutes(notAfterMinutes));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddJwtAuthServices(new TokenValidationParameters
        {
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new X509SecurityKey(certificate),
            TryAllIssuerSigningKeys = false,
            ClockSkew = TimeSpan.FromMinutes(clockSkewMinutes)
        });

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/protected", () => "authorized").RequireAuthorization();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var token = app.Services.GetRequiredService<CreateJwtTokenService>()
            .CreateJwtToken(new UserInfo("user-id", "User"), now.AddMinutes(10));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/protected");

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    private static WebApplicationBuilder Configure(
        IUserAuthenticator authenticator,
        IRefreshTokenRepository repository)
    {
        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator);
        builder.Services.AddSingleton(repository);
        return builder;
    }

    private static Mock<IRefreshTokenRepository> CreateRepositoryMock()
    {
        var repository = new Mock<IRefreshTokenRepository>();
        repository
            .Setup(r => r.AddAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                (string userId, DateTimeOffset expiresAt, CancellationToken _) =>
                    ValueTask.FromResult(
                        new RefreshToken(
                            "refresh-token",
                            userId,
                            expiresAt)));
        repository
            .Setup(r => r.RotateAsync(
                "refresh-token",
                "user-id",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RefreshTokenRotation(
                    new UserInfo("user-id", "User"),
                    NewRefreshToken("user-id")));
        return repository;
    }

    private static RefreshToken NewRefreshToken(string userId) =>
        new(
            "new-refresh-token",
            userId,
            DateTimeOffset.UtcNow.AddHours(1));

    private sealed class ConcurrentRepository : IRefreshTokenRepository
    {
        private int _active = 1;
        private int _rotationCount;

        public int RotationCount => _rotationCount;

        public ValueTask<RefreshToken> AddAsync(
            string userId,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(
                new RefreshToken(
                    "new-refresh-token",
                    userId,
                    expiresAt));
        }

        public ValueTask<RefreshTokenRotation?> RotateAsync(
            string refreshTokenKey,
            string userId,
            DateTimeOffset newTokenExpiration,
            CancellationToken cancellationToken)
        {
            if (refreshTokenKey != "refresh-token" ||
                userId != "user-id" ||
                Interlocked.CompareExchange(ref _active, 0, 1) != 1)
            {
                return ValueTask.FromResult<RefreshTokenRotation?>(null);
            }

            Interlocked.Increment(ref _rotationCount);
            return ValueTask.FromResult<RefreshTokenRotation?>(
                new RefreshTokenRotation(
                    new UserInfo(userId, "User"),
                    new RefreshToken(
                        "new-refresh-token",
                        userId,
                        newTokenExpiration)));
        }
    }

    private sealed class TestHttpClientFactory(HttpClient client)
        : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class SingleLoggerProvider(ILogger logger)
        : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => logger;

        public void Dispose()
        {
        }
    }

    private sealed class CollectingLogger : ILogger
    {
        public List<string> WarningMessages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                WarningMessages.Add(formatter(state, exception));
            }
        }
    }

    [AppAuthorize(["Admin"], ["TenantA"])]
    public sealed class JwtProtectedRequest : IRequest<string>;

    private sealed class TestNextHandler
        : IRequestHandlerNext<JwtProtectedRequest, string>
    {
        public ValueTask<string> InvokeAsync(
            JwtProtectedRequest request,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult("authorized:user-id");
        }
    }
}
