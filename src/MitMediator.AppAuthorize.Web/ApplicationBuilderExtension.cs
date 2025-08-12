namespace MitMediator.AppAuthorize.Web;

public static class ApplicationBuilderExtension
{
    /// <summary>
    /// Use Basic Auth.
    /// </summary>
    /// <param name="builder"><see cref="IApplicationBuilder"/>.</param>
    /// <returns><see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseBasicAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<BasicAuthMiddleware>();
    }
}