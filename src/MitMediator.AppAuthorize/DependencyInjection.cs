using MitMediator;
using MitMediator.AppAuthorize;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    /// <summary>
    /// Inject <see cref="AppAuthorizeBehavior"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>.</param>
    /// <param name="superuserRoleName">Add superuser role name. All requests will be available for users with a superuser role.</param>
    /// <returns><see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddAppAuthorize(this IServiceCollection services, string? superuserRoleName = null)
    {
        SuperuserRoleName.Value = superuserRoleName;
        return services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AppAuthorizeBehavior<,>));
    }
}