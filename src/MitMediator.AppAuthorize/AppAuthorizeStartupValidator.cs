using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace MitMediator.AppAuthorize;

internal sealed class AppAuthorizeStartupValidator(
    IServiceCollection services) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var requestType in services
                     .Where(IsRequestHandlerRegistration)
                     .Select(descriptor =>
                         descriptor.ServiceType.GenericTypeArguments[0])
                     .Distinct())
        {
            AuthorizationMetadata.GetOrCreate(requestType);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    internal static bool IsRequestHandlerRegistration(
        ServiceDescriptor descriptor)
    {
        if (!descriptor.ServiceType.IsGenericType ||
            descriptor.ServiceType.ContainsGenericParameters)
        {
            return false;
        }

        var genericType =
            descriptor.ServiceType.GetGenericTypeDefinition();
        return genericType == typeof(IRequestHandler<>) ||
               genericType == typeof(IRequestHandler<,>);
    }
}
