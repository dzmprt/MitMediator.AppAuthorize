using System.Security.Claims;
using System.Text;

namespace MitMediator.AppAuthorize.Web;

public class BasicAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var headerValue)
            && headerValue.ToString().StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            var encodedCredentials = headerValue.ToString().Substring("Basic ".Length).Trim();
            string decodedCredentials;
            try
            {
                decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            }
            catch (FormatException)
            {
                SetUnauthorizedResponse(context);
                return;
            }

            var separatorIndex = decodedCredentials.IndexOf(':');

            if (separatorIndex >= 0)
            {
                var authenticator = context.RequestServices.GetRequiredService<IUserAuthenticator>();
                var user = await authenticator.AuthByPasswordAsync(
                    decodedCredentials[..separatorIndex],
                    decodedCredentials[(separatorIndex + 1)..],
                    context.RequestAborted);
                var claims = new List<Claim>();
                if (user.UserId is null)
                {
                    throw new ArgumentNullException(nameof(user.UserId));
                }
                
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user.UserId));

                if (user.Name is not null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, user.Name));
                }
         
                if (user.Roles is not null)
                {
                    foreach (var userRole in user.Roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole));
                    }
                }

                if (user.Tenants is not null)
                {
                    foreach (var tenant in user.Tenants)
                    {
                        claims.Add(new Claim(AppAuthorizeClaimTypes.TenantId, tenant));
                    }
                }
                
                var identity = new ClaimsIdentity(claims, "Basic");
                context.User = new ClaimsPrincipal(identity);
            }
            else
            {
                SetUnauthorizedResponse(context);
                return;
            }
        }

        await next(context);
    }

    private static void SetUnauthorizedResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.Append("WWW-Authenticate", "Basic");
    }
}