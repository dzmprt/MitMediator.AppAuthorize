using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MitMediator.AppAuthorize.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddAppAuthorize_ShouldRegisterAppAuthorizeBehavior_AsTransient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAppAuthorize();
        services.AddTransient<IAuthenticationContext, FakeAuthenticationContext>();
        var provider = services.BuildServiceProvider();

        // Assert
        var behavior = provider.GetService<IPipelineBehavior<FakeRequest, string>>();
        Assert.NotNull(behavior);
        Assert.IsType<AppAuthorizeBehavior<FakeRequest, string>>(behavior);
        
        var behavior2 = provider.GetService<IPipelineBehavior<FakeRequest, string>>();
        Assert.NotSame(behavior, behavior2);
    }

    [Fact]
    public void AddAppAuthorize_ShouldNotRegisterDuplicateBehavior_WhenReconfiguredIdentically()
    {
        var services = new ServiceCollection();

        services.AddAppAuthorize("First");
        services.AddAppAuthorize("First");
        services.AddTransient<IAuthenticationContext, FakeAuthenticationContext>();
        var provider = services.BuildServiceProvider();

        Assert.Single(
            provider.GetServices<IPipelineBehavior<FakeRequest, string>>());
        Assert.Single(
            provider.GetServices<AppAuthorizeConfiguration>());
    }

    [Fact]
    public void AddAppAuthorize_ShouldRejectConflictingSuperuserRole()
    {
        var services = new ServiceCollection();
        services.AddAppAuthorize("First");

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddAppAuthorize("Second"));

        Assert.Contains("already configured", exception.Message);
        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(AppAuthorizeConfiguration));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddAppAuthorize_ShouldRejectBlankSuperuserRole(string role)
    {
        var services = new ServiceCollection();

        Assert.ThrowsAny<ArgumentException>(
            () => services.AddAppAuthorize(role));
    }

    [Fact]
    public void AddAppAuthorize_ShouldTrimSuperuserRole()
    {
        var services = new ServiceCollection();
        services.AddAppAuthorize(" Superuser ");
        var configuration =
            services.BuildServiceProvider()
                .GetRequiredService<AppAuthorizeConfiguration>();

        Assert.Equal("Superuser", configuration.SuperuserRoleName);
    }

    [Fact]
    public async Task AddAppAuthorize_ShouldValidateRegisteredRequestMetadataAtStartup()
    {
        var services = new ServiceCollection();
        services.AddAppAuthorize();
        services.AddTransient<
            IRequestHandler<ConflictingAuthorizationRequest, string>,
            ConflictingAuthorizationRequestHandler>();
        var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IHostedService>();

        var validator = Assert.Single(validators);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));

        Assert.Contains("cannot be declared together", exception.Message);
    }

    [Fact]
    public async Task AddAppAuthorize_ShouldRejectInheritedAuthorizationAtStartup()
    {
        var services = new ServiceCollection();
        services.AddAppAuthorize();
        services.AddTransient<
            IRequestHandler<DerivedAnonymousRequest, string>,
            DerivedAnonymousRequestHandler>();
        var provider = services.BuildServiceProvider();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Assert.Single(provider.GetServices<IHostedService>())
                .StartAsync(CancellationToken.None));

        Assert.Contains("cannot be inherited", exception.Message);
    }

    public class FakeRequest : IRequest<string> { }

    [AppAllowAnonymous]
    [AppAuthorize(["Admin"])]
    public sealed class ConflictingAuthorizationRequest : IRequest<string>;

    private sealed class ConflictingAuthorizationRequestHandler
        : IRequestHandler<ConflictingAuthorizationRequest, string>
    {
        public ValueTask<string> HandleAsync(
            ConflictingAuthorizationRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(string.Empty);
    }

    [AppAllowAnonymous]
    public class AnonymousRequest : IRequest<string>;

    public sealed class DerivedAnonymousRequest : AnonymousRequest;

    private sealed class DerivedAnonymousRequestHandler
        : IRequestHandler<DerivedAnonymousRequest, string>
    {
        public ValueTask<string> HandleAsync(
            DerivedAnonymousRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(string.Empty);
    }
}