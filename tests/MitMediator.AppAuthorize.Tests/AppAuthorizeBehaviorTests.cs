using MitMediator.AppAuthorize.Exceptions;
using Moq;

namespace MitMediator.AppAuthorize.Tests;

public class AppAuthorizeBehaviorTests
{
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    [Fact]
    public async Task HandleAsync_ShouldAllowAnonymousRequest()
    {
        // Arrange
        var authContext = new Mock<IAuthenticationContext>();
        var next = new Mock<IRequestHandlerNext<AnonymousRequest, string>>();
        next.Setup(n => n.InvokeAsync(It.IsAny<AnonymousRequest>(), _cancellationToken))
            .ReturnsAsync("allowed");

        var behavior = new AppAuthorizeBehavior<AnonymousRequest, string>(authContext.Object);

        // Act
        var result = await behavior.HandleAsync(new AnonymousRequest(), next.Object, _cancellationToken);

        // Assert
        Assert.Equal("allowed", result);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var authContext = new Mock<IAuthenticationContext>();
        authContext.Setup(a => a.UserId).Returns((string?)null);

        var next = new Mock<IRequestHandlerNext<SecuredRequest, string>>();

        var behavior = new AppAuthorizeBehavior<SecuredRequest, string>(authContext.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await behavior.HandleAsync(new SecuredRequest(), next.Object, _cancellationToken));
    }
    
    [Fact]
    public async Task HandleAsync_SShouldPass_WhenAuthorizedWithoutAuthAttribute()
    {
        // Arrange
        var authContext = new Mock<IAuthenticationContext>();
        authContext.Setup(a => a.UserId).Returns("userId");

        var next = new Mock<IRequestHandlerNext<SecuredRequest, string>>();
        next.Setup(n => n.InvokeAsync(It.IsAny<SecuredRequest>(), _cancellationToken))
            .ReturnsAsync("success");

        var behavior = new AppAuthorizeBehavior<SecuredRequest, string>(authContext.Object);

        // Act
        var result = await behavior.HandleAsync(new SecuredRequest(), next.Object, _cancellationToken);

        // Assert
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowForbidden_WhenRoleMismatch()
    {
        // Arrange
        var authContext = new Mock<IAuthenticationContext>();
        authContext.Setup(a => a.UserId).Returns("userId");
        authContext.Setup(a => a.UserRoles).Returns(["user"]);
        authContext.Setup(a => a.TenantIds).Returns(["Tenant1"]);

        var next = new Mock<IRequestHandlerNext<RoleRestrictedRequest, string>>();

        var behavior = new AppAuthorizeBehavior<RoleRestrictedRequest, string>(authContext.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await behavior.HandleAsync(new RoleRestrictedRequest(), next.Object, _cancellationToken));

        Assert.Contains("required roles", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowForbidden_WhenTenantMismatch()
    {
        // Arrange
        var authContext = new Mock<IAuthenticationContext>();
        authContext.Setup(a => a.UserId).Returns("userId");
        authContext.Setup(a => a.UserRoles).Returns(["Admin"]);
        authContext.Setup(a => a.TenantIds).Returns(["123"]);

        var next = new Mock<IRequestHandlerNext<TenantRestrictedRequest, string>>();

        var behavior = new AppAuthorizeBehavior<TenantRestrictedRequest, string>(authContext.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(async () =>
            await behavior.HandleAsync(new TenantRestrictedRequest(), next.Object, _cancellationToken));

        Assert.Contains("Invalid user tenant", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldPass_WhenAuthorized()
    {
        // Arrange
        var authContext = new Mock<IAuthenticationContext>();
        authContext.Setup(a => a.UserId).Returns("userId");
        authContext.Setup(a => a.UserRoles).Returns(["Admin"]);
        authContext.Setup(a => a.TenantIds).Returns(["Tenant1"]);

        var next = new Mock<IRequestHandlerNext<TenantRestrictedRequest, string>>();
        next.Setup(n => n.InvokeAsync(It.IsAny<TenantRestrictedRequest>(), _cancellationToken))
            .ReturnsAsync("success");

        var behavior = new AppAuthorizeBehavior<TenantRestrictedRequest, string>(authContext.Object);

        // Act
        var result = await behavior.HandleAsync(new TenantRestrictedRequest(), next.Object, _cancellationToken);

        // Assert
        Assert.Equal("success", result);
    }

    // ====== Fake IRequest types with attributes ======

    [AppAllowAnonymous]
    public class AnonymousRequest : IRequest<string>
    {
    }

    public class SecuredRequest : IRequest<string>
    {
    }

    [AppAuthorize(new[] { "Admin" })]
    public class RoleRestrictedRequest : IRequest<string>
    {
    }

    [AppAuthorize(new[] { "Admin" }, new[] { "Tenant1" })]
    public class TenantRestrictedRequest : IRequest<string>
    {
    }
}