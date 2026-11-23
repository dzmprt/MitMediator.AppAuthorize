using System.Collections.Concurrent;
using MitMediator.AppAuthorize.Exceptions;

namespace MitMediator.AppAuthorize;

internal class AppAuthorizeBehavior<TRequest, TResponse>(
    IAuthenticationContext authContext,
    AppAuthorizeConfiguration configuration)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, AuthorizationMetadata> Metadata = new();

    public AppAuthorizeBehavior(IAuthenticationContext authContext)
        : this(authContext, new AppAuthorizeConfiguration(null))
    {
    }

    public ValueTask<TResponse> HandleAsync(TRequest request, IRequestHandlerNext<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        var metadata = Metadata.GetOrAdd(
            request.GetType(),
            AuthorizationMetadata.GetOrCreate);

        if (metadata.AllowAnonymous)
        {
            return next.InvokeAsync(request, cancellationToken);
        }

        if (!authContext.IsAuthenticated ||
            string.IsNullOrWhiteSpace(authContext.UserId))
        {
            throw new UnauthorizedException();
        }

        if (configuration.SuperuserRoleName is not null &&
            authContext.IsInRole(configuration.SuperuserRoleName))
        {
            return next.InvokeAsync(request, cancellationToken);
        }

        var authorizeAttribute = metadata.Authorize;
        if (authorizeAttribute is null)
        {
            return next.InvokeAsync(request, cancellationToken);
        }

        if (authorizeAttribute.Roles is { Length: > 0 } &&
            !authContext.IsInOneOfRoles(authorizeAttribute.Roles))
        {
            throw new ForbiddenException($"User must possess at least one of the required roles: {string.Join(", ", authorizeAttribute.Roles)}.");
        }

        if (authorizeAttribute.TenantsIds is { Length: > 0 } &&
            !authContext.IsInOneOfTenants(authorizeAttribute.TenantsIds))
        {
            throw new ForbiddenException("Invalid user tenant.");
        }

        return next.InvokeAsync(request, cancellationToken);
    }
}
