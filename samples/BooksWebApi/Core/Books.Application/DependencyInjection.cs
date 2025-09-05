using System.Reflection;
using Books.Application.Behaviors;
using Books.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MitMediator;
using MitMediator.AppAuthorize;

namespace Books.Application;

/// <summary>
/// Dependency injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add application services.
    /// </summary>
    /// <param name="services"><see cref="IServiceProvider"/>.</param>
    /// <returns><see cref="IServiceProvider"/></returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services
            .AddMitMediator()
            .AddAppAuthorize()
            .AddTransient<IUserAuthenticator, UserAuthenticator>()
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true)
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    }
}