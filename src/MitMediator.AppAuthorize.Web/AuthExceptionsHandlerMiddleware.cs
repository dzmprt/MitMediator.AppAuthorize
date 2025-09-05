using System.Net;

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
            await HandleExceptionAsync(context, exception);
        }
        catch (ForbiddenException exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var result = string.Empty;
        switch (exception)
        {
            case UnauthorizedException unauthorized:
                code = HttpStatusCode.Unauthorized;
                result = System.Text.Json.JsonSerializer.Serialize(unauthorized.Message);
                break;
            case ForbiddenException forbidden:
                code = HttpStatusCode.Forbidden;
                result = System.Text.Json.JsonSerializer.Serialize(forbidden.Message);
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(result);
    }
}

public static class AuthExceptionsHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthException(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthExceptionsHandlerMiddleware>();
    }
}