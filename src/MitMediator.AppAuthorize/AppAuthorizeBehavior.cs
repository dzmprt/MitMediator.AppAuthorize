using System.Reflection;

namespace MitMediator.AppAuthorize;

public class AppAuthorizeBehavior<TRequest, TResponse>(IAuthenticationContext authContext) 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<TResponse> HandleAsync(TRequest request, IRequestHandlerNext<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        var allowAnonymous = request.GetType().GetCustomAttribute<AppAllowAnonymousAttribute>();
        if (allowAnonymous != null)
        {
            return next.InvokeAsync(request, cancellationToken);
        }

        if (!authContext.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }

        var authAttr = request.GetType().GetCustomAttribute<AppAuthorizeAttribute>();
        if (authAttr == null)
        {
            return next.InvokeAsync(request, cancellationToken);
        }
        
        if (authAttr.Roles is { Length: > 0 } && !authContext.IsInOneOfRoles(authAttr.Roles))
        {
            throw new ForbiddenException($"User must possess at least one of the required roles: {string.Join(", ", authAttr.Roles)}.");
        }
        
        if (authAttr.TenantsIds is { Length: > 0 } && !authContext.IsInOneOfTenants(authAttr.TenantsIds))
        {
            throw new ForbiddenException("Invalid user tenant.");
        }

        return next.InvokeAsync(request, cancellationToken);
    }
}
