using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MitMediator.AppAuthorize.Web.Tests;

public class AuthExceptionsHandlerMiddlewareTests
{
    [Fact]
    public async Task Invoke_ShouldSet401_WhenUnauthorizedExceptionThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new AuthExceptionsHandlerMiddleware(_ =>
        {
            throw new UnauthorizedException("Not authenticated");
        });

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(JsonSerializer.Serialize("Not authenticated"), responseText);
    }

    [Fact]
    public async Task Invoke_ShouldSet403_WhenForbiddenExceptionThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new AuthExceptionsHandlerMiddleware(_ =>
        {
            throw new ForbiddenException("Access denied");
        });

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(JsonSerializer.Serialize("Access denied"), responseText);
    }

    [Fact]
    public async Task Invoke_ShouldPassThrough_WhenNoExceptionThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var called = false;

        var middleware = new AuthExceptionsHandlerMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.True(called);
        Assert.Equal(200, context.Response.StatusCode); // default
    }
}