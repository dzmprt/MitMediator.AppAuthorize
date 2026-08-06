using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MitMediator.AppAuthorize.Domain;
using Moq;

namespace MitMediator.AppAuthorize.Web.Tests;

public class AuthApiTests
{
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
        builder.Services.AddJwtAuthServices("key1234567890098765433123123123232323");

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