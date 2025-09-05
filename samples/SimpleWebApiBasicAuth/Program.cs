using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using MitMediator;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Add MitMediator
builder.Services.AddMitMediator();

// Add default auth context
builder.Services.AddDefaultAuthContext();
// Inject IUserAuthenticator implementation
builder.Services.AddScoped<IUserAuthenticator, UserAuthenticator>();
// Middle authorize logic
builder.Services.AddAppAuthorize();

// Add basic auth to swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Description = "Username: <b>test</b>, Password: <b>test</b>",
    });
    options.AddBasicAuth();
});

var app = builder.Build();

// Handler auth exceptions (Forbidden, Unauthorized)
app.UseAuthException();

app.UseSwagger();
app.UseSwaggerUI();

// Add basic auth middleware
app.UseBasicAuth();

app.MapGet("/auth-status",
    async ([FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        await mediator.SendAsync<GetAuthStatusQuery, bool>(new GetAuthStatusQuery(), cancellationToken));


app.MapGet("/test",
    async ([FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        await mediator.SendAsync(new GetTestQuery(), cancellationToken));

app.Run();

// Implement IUserAuthenticator for sing-in logic
public class UserAuthenticator : IUserAuthenticator
{
    public ValueTask<UserInfo> AuthByPasswordAsync(string username, string password,
        CancellationToken cancellationToken)
    {
        if (username == "test" && password == "test")
            return ValueTask.FromResult(new UserInfo("testId", "test"));

        throw new ForbiddenException();
    }
}

[AppAllowAnonymous]
public class GetAuthStatusQuery : IRequest<bool>;

public class GetAuthStatusQueryHandler(IAuthenticationContext authenticationContext)
    : IRequestHandler<GetAuthStatusQuery, bool>
{
    public ValueTask<bool> HandleAsync(GetAuthStatusQuery request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(authenticationContext.IsAuthenticated);
}

public class GetTestQuery : IRequest;

public class GetTestQueryHandler : IRequestHandler<GetTestQuery>
{
    public ValueTask<Unit> HandleAsync(GetTestQuery request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Unit.Value);
}