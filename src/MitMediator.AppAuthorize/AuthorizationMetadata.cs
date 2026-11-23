using System.Reflection;

namespace MitMediator.AppAuthorize;

internal sealed record AuthorizationMetadata(
    bool AllowAnonymous,
    AppAuthorizeAttribute? Authorize)
{
    public static AuthorizationMetadata GetOrCreate(Type requestType)
    {
        var hasInheritedAuthorization =
            requestType.BaseType is { } baseType &&
            baseType
                .EnumerateTypeHierarchy()
                .Any(type =>
                    type.IsDefined(
                        typeof(AppAllowAnonymousAttribute),
                        inherit: false) ||
                    type.IsDefined(
                        typeof(AppAuthorizeAttribute),
                        inherit: false));
        if (hasInheritedAuthorization)
        {
            throw new InvalidOperationException(
                $"Authorization attributes cannot be inherited. Declare the required attributes directly on '{requestType.FullName}'.");
        }

        var allowAnonymous =
            requestType.GetCustomAttribute<AppAllowAnonymousAttribute>(
                inherit: false) is not null;
        var authorize =
            requestType.GetCustomAttribute<AppAuthorizeAttribute>(
                inherit: false);
        if (allowAnonymous && authorize is not null)
        {
            throw new InvalidOperationException(
                $"'{nameof(AppAllowAnonymousAttribute)}' and '{nameof(AppAuthorizeAttribute)}' cannot be declared together on '{requestType.FullName}'.");
        }

        return new AuthorizationMetadata(allowAnonymous, authorize);
    }
}
