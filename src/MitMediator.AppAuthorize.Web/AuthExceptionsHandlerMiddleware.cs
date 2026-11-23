using System.Net;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MitMediator.AppAuthorize.Exceptions;

namespace MitMediator.AppAuthorize.Web;

internal class AuthExceptionsHandlerMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
        catch (ForbiddenException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode = exception switch
        {
            UnauthorizedException => HttpStatusCode.Unauthorized,
            ForbiddenException => HttpStatusCode.Forbidden,
            _ => HttpStatusCode.InternalServerError
        };
        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Type = statusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "https://www.rfc-editor.org/rfc/rfc9110.html#name-401-unauthorized",
                HttpStatusCode.Forbidden =>
                    "https://www.rfc-editor.org/rfc/rfc9110.html#name-403-forbidden",
                _ =>
                    "https://www.rfc-editor.org/rfc/rfc9110.html#name-500-internal-server-error"
            },
            Title = statusCode.ToString(),
            Detail = exception switch
            {
                UnauthorizedException => "Authentication is required or credentials are invalid.",
                ForbiddenException => "The authenticated user does not have access to this resource.",
                _ => "An unexpected error occurred."
            },
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier
            }
        };

        ResetResponseBody(context.Response);
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;
        if (context.Features.Get<IHttpResponseFeature>() is { } responseFeature)
        {
            responseFeature.ReasonPhrase = null;
        }

        if (statusCode == HttpStatusCode.Unauthorized)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
        }
        DisableResponseCaching(context);

        var requestServices = context.Features
            .Get<IServiceProvidersFeature>()?
            .RequestServices;
        var serializerOptions = requestServices?
            .GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()?
            .Value
            .SerializerOptions
            ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        return context.Response.WriteAsJsonAsync(
            problemDetails,
            serializerOptions,
            "application/problem+json",
            context.RequestAborted);
    }

    private static void ResetResponseBody(HttpResponse response)
    {
        if (response.Body.CanSeek)
        {
            response.Body.SetLength(0);
            response.Body.Position = 0;
        }

        var headers = response.Headers;
        headers.ContentLength = null;
        headers.ContentEncoding = default;
        headers.ContentLanguage = default;
        headers.ContentLocation = default;
        headers.ContentRange = default;
        headers.ContentDisposition = default;
        headers.ETag = default;
        headers.LastModified = default;
        headers.AcceptRanges = default;
        headers.TransferEncoding = default;
        headers.Trailer = default;
    }

    private static void DisableResponseCaching(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.CacheControl = "no-store, no-cache";
        headers.Pragma = "no-cache";
        headers.Expires = "0";
    }
}

public static class AuthExceptionsHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthException(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthExceptionsHandlerMiddleware>();
    }
}
