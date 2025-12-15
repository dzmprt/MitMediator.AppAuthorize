using Microsoft.Extensions.DependencyInjection;
using MitMediator;
using MitMediator.AppAuthorize;

var services = new ServiceCollection();
services
    .AddScoped<IAuthenticationContext, AuthenticationContext>()
    .AddMitMediator()
    .AddAppAuthorize();

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();

// AnonymousPingRequestHandler: Pong
var result = await mediator.SendAsync<AnonymousPingRequest, string>(new AnonymousPingRequest(), CancellationToken.None);
Console.WriteLine(result); // AnonymousPingRequest

// UserRolePingRequestHandler: Pong
result = await mediator.SendAsync<UserRolePingRequest, string>(new UserRolePingRequest(), CancellationToken.None);
Console.WriteLine(result); // UserRolePingRequest

// TestTenantPingRequestHandler: Pong
result = await mediator.SendAsync<TestTenantPingRequest, string>(new TestTenantPingRequest(), CancellationToken.None);
Console.WriteLine(result); // TestTenantPingRequest

try
{ 
    result = await mediator.SendAsync<AdminRolePingRequest, string>(new AdminRolePingRequest(), CancellationToken.None);
}
catch(Exception ex)
{
    // User must possess at least one of the required roles: admin.
    Console.WriteLine(ex.Message);
}

try
{ 
    await mediator.SendAsync<SecondTenantPingRequest, string>(new SecondTenantPingRequest(), CancellationToken.None);
}
catch(Exception ex)
{
    // Invalid user tenant.
    Console.WriteLine(ex.Message);
}

result = await mediator.SendAsync<NoAttributeRequest, string>(new NoAttributeRequest(), CancellationToken.None);
Console.WriteLine(result);

[AppAllowAnonymous]
public class AnonymousPingRequest : IRequest<string> { }

public class AnonymousPingRequestHandler : IRequestHandler<AnonymousPingRequest, string>
{
    public ValueTask<string> HandleAsync(AnonymousPingRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("AnonymousPingRequestHandler: Pong");
        return ValueTask.FromResult("AnonymousPingRequest result");
    }
}

[AppAuthorize(["user"])]
public class UserRolePingRequest : IRequest<string> { }
public class UserRolePingRequestHandler : IRequestHandler<UserRolePingRequest, string>
{
    public ValueTask<string> HandleAsync(UserRolePingRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("UserRolePingRequestHandler: Pong");
        return ValueTask.FromResult("UserRolePingRequest");
    }
}

[AppAuthorize(["admin"])]
public class AdminRolePingRequest : IRequest<string> { }
public class AdminRolePingRequestHandler : IRequestHandler<AdminRolePingRequest, string>
{
    public ValueTask<string> HandleAsync(AdminRolePingRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("AdminRolePingRequestHandler: Pong");
        return ValueTask.FromResult("AdminRolePingRequest");
    }
}

[AppAuthorize(null, ["test-tenant"])]
public class TestTenantPingRequest : IRequest<string> { }
public class TestTenantPingRequestHandler : IRequestHandler<TestTenantPingRequest, string>
{
    public ValueTask<string> HandleAsync(TestTenantPingRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("TestTenantPingRequestHandler: Pong");
        return ValueTask.FromResult("TestTenantPingRequest");
    }
}

[AppAuthorize(null, ["second-tenant"])]
public class SecondTenantPingRequest : IRequest<string> { }
public class SecondTenantPingRequestHandler : IRequestHandler<SecondTenantPingRequest, string>
{
    public ValueTask<string> HandleAsync(SecondTenantPingRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("SecondTenantPingRequestHandler: Pong");
        return ValueTask.FromResult("SecondTenantPingRequest");
    }
}

public class NoAttributeRequest : IRequest<string> {}
public class NoAttributeRequestHandler: IRequestHandler<NoAttributeRequest, string>
{
    public ValueTask<string> HandleAsync(NoAttributeRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("NoAttributeRequestHandler");
        return ValueTask.FromResult("NoAttributeRequest");
    }
}

public class AuthenticationContext : IAuthenticationContext
{
    public string? UserId => "123";
    public string? UserName => "testuser";
    public string[]? UserRoles => Claims?.Where(c => c.Key == "role").Select(c => c.Value).ToArray();
    public Dictionary<string, string>? Claims => new() { { "tenant", "test-tenant" }, { "role", "user" } };
    public string[]? TenantIds => Claims?.Where(c => c.Key == "tenant").Select(c => c.Value).ToArray();
}

