using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using MitMediator;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Domain;
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
// Inject IRefreshTokenRepository implementation
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
// Middle authorize logic
builder.Services.AddAppAuthorize();
// Auth and jwt services
builder.Services.AddJwtAuthServices("key1234567890098765433123123123232323");

// Add jwt auth to swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Description = "Username: <b>test</b>, Password: <b>test</b>",
    });
    options.AddJwtAuth();
});

var app = builder.Build();

// Handler auth exceptions (Forbidden, Unauthorized)
app.UseAuthException();

app.UseSwagger();
app.UseSwaggerUI();

// Map jwt endpoints
app.MapJwtAuthApi();

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

// Implement IRefreshTokenRepository
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private static readonly ConcurrentDictionary<string, RefreshToken> _concurrentDictionary = new();

    public ValueTask<RefreshToken> AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        refreshToken.RefreshTokenKey = Guid.NewGuid().ToString();
        if (!_concurrentDictionary.TryAdd(refreshToken.RefreshTokenKey, refreshToken))
        {
            throw new Exception("Add refresh token error.");
        }

        return ValueTask.FromResult(refreshToken);
    }

    public ValueTask<RefreshToken?> GetOrDefaultAsync(string refreshTokenKey, CancellationToken cancellationToken)
    {
        _concurrentDictionary.TryGetValue(refreshTokenKey, out var value);
        return ValueTask.FromResult(value);
    }

    public ValueTask RemoveAsync(RefreshToken entity, CancellationToken cancellationToken)
    {
        _concurrentDictionary.TryRemove(entity.RefreshTokenKey, out var value);
        return ValueTask.CompletedTask;
    }

    public ValueTask<UserInfo?> GetUserInfoByTokenOrDefaultAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        _concurrentDictionary.TryGetValue(refreshToken.RefreshTokenKey, out var token);
        if (token is null)
        {
            return ValueTask.FromResult<UserInfo?>(null);
        }
        
        return ValueTask.FromResult<UserInfo?>(new UserInfo(token.UserId, token.UserId));
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