using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MitMediator.AppAuthorize.Web.Tests;

public class BasicAuthMiddlewareTests
{
    private static string EncodeBasic(string username, string password)
    {
        var raw = $"{username}:{password}";
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static HttpContext CreateContext(string? authHeader = null)
    {
        var context = new DefaultHttpContext();
        if (authHeader != null)
        {
            context.Request.Headers["Authorization"] = authHeader;
        }
        return context;
    }

    [Fact]
    public async Task InvokeAsync_ShouldAuthenticateUser_WhenCredentialsAreValid()
    {
        // Arrange
        var user = new UserInfo("user123", "Dmitriy", ["Admin"], ["TenantA"]);

        var mockAuthenticator = new Mock<IUserAuthenticator>();
        mockAuthenticator
            .Setup(a => a.AuthByPasswordAsync("login", "pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var context = CreateContext(EncodeBasic("login", "pass"));
        context.RequestServices = new ServiceCollection()
            .AddSingleton(mockAuthenticator.Object)
            .BuildServiceProvider();

        var middleware = new BasicAuthMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var principal = context.User;
        Assert.NotNull(principal);
        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.Equal("user123", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("Dmitriy", principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        Assert.Contains(principal.Claims, c => c.Type == AppAuthorizeClaimTypes.TenantId && c.Value == "TenantA");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_WhenCredentialsAreMalformed()
    {
        // Arrange
        var context = CreateContext("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid_format")));
        context.Response.Body = new MemoryStream();

        var middleware = new BasicAuthMiddleware(_ => Task.CompletedTask);
        context.RequestServices = new ServiceCollection()
            .AddSingleton(Mock.Of<IUserAuthenticator>())
            .BuildServiceProvider();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.True(context.Response.Headers.ContainsKey("WWW-Authenticate"));
    }

    [Fact]
    public async Task InvokeAsync_ShouldSkipAuth_WhenNoAuthorizationHeader()
    {
        // Arrange
        var context = CreateContext(); // no header
        var called = false;

        var middleware = new BasicAuthMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        context.RequestServices = new ServiceCollection()
            .AddSingleton(Mock.Of<IUserAuthenticator>())
            .BuildServiceProvider();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(called);
        Assert.Null(context.User?.Identity?.Name);
    }

    [Fact]
    public async Task InvokeAsync_ShouldThrow_WhenUserIdIsNull()
    {
        // Arrange
        var user = new UserInfo(null, "Name");
        var mockAuthenticator = new Mock<IUserAuthenticator>();
        mockAuthenticator
            .Setup(a => a.AuthByPasswordAsync("login", "pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var context = CreateContext(EncodeBasic("login", "pass"));
        context.RequestServices = new ServiceCollection()
            .AddSingleton(mockAuthenticator.Object)
            .BuildServiceProvider();

        var middleware = new BasicAuthMiddleware(_ => Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            middleware.InvokeAsync(context));
    }
}