using MitMediator;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Exceptions;
using MitMediator.AppAuthorize.Tests;

public class SuperuserAccessTests
{
    [Fact]
    public async Task SuperuserRole_AllowsAccessToAnyRequest()
    {
        // Arrange
        SuperuserRoleName.Value = "super";
        var context = new FakeAuthenticationContext
        {
            UserId = "1",
            UserRoles = new[] { "super" }
        };
        var behavior = new AppAuthorizeBehavior<DummyRequest, string>(context);
        var nextCalled = false;
        var next = new DummyNextHandler<string>(() => { nextCalled = true; return new ValueTask<string>("ok"); });
        var result = await behavior.HandleAsync(new DummyRequest(), next, CancellationToken.None);
        // Assert
        Assert.True(nextCalled);
        Assert.Equal("ok", result);
    }
    
    [Fact]
    public async Task SuperuserRole_DeniedAccessToAppAuthorizeRequestWithOtherRole()
    {
        // Arrange
        SuperuserRoleName.Value = "super";
        var context = new FakeAuthenticationContext
        {
            UserId = "1",
            UserRoles = new[] { "notsuper" }
        };
        var behavior = new AppAuthorizeBehavior<DummyRequest, string>(context);
        var nextCalled = false;
        var next = new DummyNextHandler<string>(() => { nextCalled = true; return new ValueTask<string>("ok"); });
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenException>(async () => await behavior.HandleAsync(new DummyRequest(), next, CancellationToken.None)) ;
        Assert.Equal("User must possess at least one of the required roles: TestRole.", exception.Message);
    }
    
    [AppAuthorize(["TestRole"])]
    private class DummyRequest : IRequest<string> { }
    private class DummyNextHandler<T> : IRequestHandlerNext<DummyRequest, T>
    {
        private readonly Func<ValueTask<T>> _func;
        public DummyNextHandler(Func<ValueTask<T>> func) => _func = func;
        public ValueTask<T> InvokeAsync(DummyRequest request, CancellationToken cancellationToken) => _func();
    }
}
