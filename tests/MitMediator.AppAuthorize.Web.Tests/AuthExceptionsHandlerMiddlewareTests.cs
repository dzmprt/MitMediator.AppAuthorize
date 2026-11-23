using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;
using MitMediator.AppAuthorize.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal("Bearer", context.Response.Headers.WWWAuthenticate);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problemDetails =
            await JsonSerializer.DeserializeAsync<ProblemDetails>(
                context.Response.Body);
        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status401Unauthorized, problemDetails.Status);
        Assert.Equal("Unauthorized", problemDetails.Title);
        Assert.Equal(
            "Authentication is required or credentials are invalid.",
            problemDetails.Detail);
        Assert.Equal(
            "no-store, no-cache",
            context.Response.Headers.CacheControl);
        Assert.Equal("no-cache", context.Response.Headers.Pragma);
        Assert.Equal("0", context.Response.Headers.Expires);
        Assert.NotNull(problemDetails.Type);
        Assert.Equal(
            context.TraceIdentifier,
            problemDetails.Extensions["traceId"]?.ToString());
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
        Assert.Equal("application/problem+json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problemDetails =
            await JsonSerializer.DeserializeAsync<ProblemDetails>(
                context.Response.Body);
        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status403Forbidden, problemDetails.Status);
        Assert.Equal("Forbidden", problemDetails.Title);
        Assert.Equal(
            "The authenticated user does not have access to this resource.",
            problemDetails.Detail);
        Assert.NotNull(problemDetails.Type);
        Assert.NotNull(problemDetails.Extensions["traceId"]);
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

    [Fact]
    public async Task Invoke_ShouldRethrowBadRequestException()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new AuthExceptionsHandlerMiddleware(_ =>
            throw new BadHttpRequestException("internal detail"));

        await Assert.ThrowsAsync<BadHttpRequestException>(
            () => middleware.Invoke(context));
    }

    [Fact]
    public async Task Invoke_ShouldRethrowAuthException_WhenResponseHasStarted()
    {
        var responseFeature = new Mock<IHttpResponseFeature>();
        responseFeature.SetupGet(feature => feature.HasStarted).Returns(true);
        var context = new DefaultHttpContext();
        context.Features.Set(responseFeature.Object);
        var middleware = new AuthExceptionsHandlerMiddleware(_ =>
            throw new UnauthorizedException());

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => middleware.Invoke(context));
    }

    [Theory]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status403Forbidden)]
    public async Task Invoke_ShouldReplaceBodyAndPreserveUnrelatedHeaders(
        int expectedStatusCode)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-id"
        };
        context.Response.Body = new MemoryStream();
        context.Response.StatusCode = StatusCodes.Status202Accepted;
        var responseFeature = context.Features.Get<IHttpResponseFeature>()!;
        responseFeature.ReasonPhrase = "Accepted";
        context.Response.ContentType = "text/plain";
        context.Response.Headers["X-Request-ID"] = "request-id";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers.ContentEncoding = "gzip";
        context.Response.Headers.ETag = "\"stale\"";
        context.Response.ContentLength = 42;
        await context.Response.Body.WriteAsync("previous response"u8.ToArray());
        var middleware = new AuthExceptionsHandlerMiddleware(_ =>
        {
            throw expectedStatusCode == StatusCodes.Status401Unauthorized
                ? new UnauthorizedException()
                : new ForbiddenException();
        });

        await middleware.Invoke(context);

        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.Null(responseFeature.ReasonPhrase);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Null(context.Response.ContentLength);
        Assert.False(context.Response.Headers.ContainsKey("Content-Encoding"));
        Assert.False(context.Response.Headers.ContainsKey("ETag"));
        Assert.Equal("request-id", context.Response.Headers["X-Request-ID"]);
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var problemDetails =
            await JsonSerializer.DeserializeAsync<ProblemDetails>(
                context.Response.Body);
        Assert.Equal(expectedStatusCode, problemDetails!.Status);
        Assert.Equal("trace-id", problemDetails.Extensions["traceId"]?.ToString());
    }

    [Fact]
    public async Task Invoke_ShouldUseConfiguredJsonOptions()
    {
        var services = new ServiceCollection();
        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(
            options => options.SerializerOptions.WriteIndented = true);
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        context.Response.Body = new MemoryStream();
        var middleware = new AuthExceptionsHandlerMiddleware(
            _ => throw new UnauthorizedException());

        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        Assert.Contains(Environment.NewLine, json, StringComparison.Ordinal);
        Assert.Contains("\"status\": 401", json, StringComparison.Ordinal);
    }
}
