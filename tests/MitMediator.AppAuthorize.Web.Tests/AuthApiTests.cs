using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MitMediator.AppAuthorize.Domain;
using MitMediator.AppAuthorize.Web.Models;
using Moq;

namespace MitMediator.AppAuthorize.Web.Tests;

public class AuthApiTests
{
    private const string SigningKey = "key1234567890098765433123123123232323";

    private static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddJwtAuthServices(SigningKey);
        return builder;
    }

    private static async Task<HttpClient> StartAppAsync(WebApplicationBuilder builder,
        bool usePassword = true, bool useCode = false, bool useRefreshToken = true)
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
        authenticator.Setup(a => a.AuthByPasswordAsync("login", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var repository = new Mock<IRefreshTokenRepository>();
        repository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken token, CancellationToken _) =>
            {
                token.RefreshTokenKey = "refresh-token";
                return token;
            });

        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator.Object);
        builder.Services.AddSingleton(repository.Object);
        var client = await StartAppAsync(builder, useCode: true, useRefreshToken: false);

        var response = await client.PostAsJsonAsync("/auth/by-password", new { login = "login", password = "password" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("refresh-token", (await response.Content.ReadFromJsonAsync<JwtTokenModel>())!.RefreshToken);
    }

    [Fact]
    public async Task CodeEndpoint_ShouldCreateTokens()
    {
        var authenticator = new Mock<IUserAuthenticatorByCode>();
        authenticator.Setup(a => a.AuthByCodeAsync("login", "1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        var repository = new Mock<IRefreshTokenRepository>();
        repository.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken token, CancellationToken _) =>
            {
                token.RefreshTokenKey = "refresh-token";
                return token;
            });

        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator.Object);
        builder.Services.AddSingleton(repository.Object);
        var client = await StartAppAsync(builder, usePassword: false, useCode: true, useRefreshToken: false);

        var response = await client.PostAsJsonAsync("/auth/by-code", new { login = "login", code = "1234" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("refresh-token", (await response.Content.ReadFromJsonAsync<JwtTokenModel>())!.RefreshToken);
    }

    [Fact]
    public async Task PasswordEndpoint_ShouldReturn403_WhenAuthenticationFails()
    {
        var authenticator = new Mock<IUserAuthenticator>();
        authenticator.Setup(a => a.AuthByPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfo)null!);
        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator.Object);
        builder.Services.AddSingleton(Mock.Of<IRefreshTokenRepository>());
        var client = await StartAppAsync(builder, useCode: false, useRefreshToken: false);

        var response = await client.PostAsJsonAsync("/auth/by-password", new { login = "login", password = "password" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CodeEndpoint_ShouldReturn403_WhenAuthenticationFails()
    {
        var authenticator = new Mock<IUserAuthenticatorByCode>();
        authenticator.Setup(a => a.AuthByCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfo)null!);
        var builder = CreateBuilder();
        builder.Services.AddSingleton(authenticator.Object);
        builder.Services.AddSingleton(Mock.Of<IRefreshTokenRepository>());
        var client = await StartAppAsync(builder, usePassword: false, useCode: true, useRefreshToken: false);

        var response = await client.PostAsJsonAsync("/auth/by-code", new { login = "login", code = "1234" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldReturn403_WhenTokenDoesNotExist()
    {
        var repository = new Mock<IRefreshTokenRepository>();
        repository.Setup(r => r.GetOrDefaultAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);
        var builder = CreateBuilder();
        builder.Services.AddSingleton(repository.Object);
        var client = await StartAppAsync(builder, usePassword: false, useCode: false);

        var response = await client.PostAsJsonAsync("/auth/by-refresh", new { refreshTokenKey = "missing", userId = "user" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldRemoveExpiredTokenAndReturn403()
    {
        var token = new RefreshToken { RefreshTokenKey = "expired", UserId = "user", Expired = DateTime.UtcNow.AddMinutes(-1) };
        var repository = new Mock<IRefreshTokenRepository>();
        repository.Setup(r => r.GetOrDefaultAsync("expired", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        var builder = CreateBuilder();
        builder.Services.AddSingleton(repository.Object);
        var client = await StartAppAsync(builder, usePassword: false, useCode: false);

        var response = await client.PostAsJsonAsync("/auth/by-refresh", new { refreshTokenKey = "expired", userId = "user" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        repository.Verify(r => r.RemoveAsync(token, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldReturn403_WhenUserIsMissing()
    {
        var token = new RefreshToken { RefreshTokenKey = "valid", UserId = "user", Expired = DateTime.UtcNow.AddMinutes(1) };
        var repository = new Mock<IRefreshTokenRepository>();
        repository.Setup(r => r.GetOrDefaultAsync("valid", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        repository.Setup(r => r.GetUserInfoByTokenOrDefaultAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInfo?)null);
        var builder = CreateBuilder();
        builder.Services.AddSingleton(repository.Object);
        var client = await StartAppAsync(builder, usePassword: false, useCode: false);

        var response = await client.PostAsJsonAsync("/auth/by-refresh", new { refreshTokenKey = "valid", userId = "user" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefreshEndpoint_ShouldNotRequireUserAuthenticator()
    {
        var refreshToken = new RefreshToken
        {
            RefreshTokenKey = "refresh-token",
            UserId = "user-id",
            Expired = DateTime.UtcNow.AddMinutes(10)
        };
        var repository = new Mock<IRefreshTokenRepository>();
        repository
            .Setup(r => r.GetOrDefaultAsync("refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
        repository
            .Setup(r => r.GetUserInfoByTokenOrDefaultAsync(refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserInfo("user-id", "User"));
        repository
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken token, CancellationToken _) =>
            {
                token.RefreshTokenKey = "new-refresh-token";
                return token;
            });

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(repository.Object);
        builder.Services.AddJwtAuthServices(SigningKey);

        await using var app = builder.Build();
        app.MapAuthApi(usePassword: false, useCode: false);
        await app.StartAsync();

        var response = await app.GetTestClient().PostAsJsonAsync("/auth/by-refresh", new
        {
            refreshTokenKey = "refresh-token",
            userId = "user-id"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}