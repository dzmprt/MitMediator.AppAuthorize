using MitMediator;
using MitMediator.AppAuthorize;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
        if (superuserRoleName is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(superuserRoleName);
            superuserRoleName = superuserRoleName.Trim();
        }

        var existingConfiguration = services
            .FirstOrDefault(descriptor =>
                descriptor.ServiceType == typeof(AppAuthorizeConfiguration))
            ?.ImplementationInstance as AppAuthorizeConfiguration;
        if (existingConfiguration is not null &&
            !string.Equals(
                existingConfiguration.SuperuserRoleName,
                superuserRoleName,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"App authorization is already configured with superuser role '{existingConfiguration.SuperuserRoleName ?? "<none>"}' and cannot be reconfigured with '{superuserRoleName ?? "<none>"}'.");
        }

        services.TryAddSingleton(new AppAuthorizeConfiguration(superuserRoleName));
        services.TryAddEnumerable(
            ServiceDescriptor.Transient(
                typeof(IPipelineBehavior<,>),
                typeof(AppAuthorizeBehavior<,>)));
        if (!services.Any(
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(AppAuthorizeStartupValidator)))
        {
            services.AddSingleton(
                new AppAuthorizeStartupValidator(services));
            services.AddSingleton<IHostedService>(
                provider =>
                    provider.GetRequiredService<
                        AppAuthorizeStartupValidator>());
        }

        return services;
    }
}