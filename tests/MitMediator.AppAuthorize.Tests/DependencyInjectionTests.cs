using Microsoft.Extensions.DependencyInjection;

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
    
    public class FakeRequest : IRequest<string> { }
}