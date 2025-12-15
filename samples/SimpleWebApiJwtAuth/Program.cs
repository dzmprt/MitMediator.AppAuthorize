using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using MitMediator;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Domain;
using MitMediator.AppAuthorize.Exceptions;
using MitMediator.AppAuthorize.Web;
using SimpleWebApiJwtAuth;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();

// Add MitMediator
builder.Services.AddMitMediator();
// Add default auth context
builder.Services.AddDefaultAuthContext();
// Inject IUserAuthenticator implementation
builder.Services.AddScoped<IUserAuthenticator, UserAuthenticator>();
// Inject IUserAuthenticatorByCode implementation
builder.Services.AddScoped<IUserAuthenticatorByCode, UserAuthenticatorByCode>();
// Inject IRefreshTokenRepository implementation
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
// Middle authorize logic
builder.Services.AddAppAuthorize();
// Auth and jwt services
builder.Services.AddJwtAuthServices("key1234567890098765433123123123232323");

// Add swagger
builder.Services.AddSwaggerGen(options => options.ConfigureSwagger());

var app = builder.Build();

// Handler auth exceptions (Forbidden, Unauthorized)
app.UseAuthException();

app.UseSwagger()
    .UseSwaggerUI();

// Map jwt endpoints
app.MapAuthApi(useCode: true);

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

public class UserAuthenticatorByCode : IUserAuthenticatorByCode
{
    public ValueTask<UserInfo> AuthByCodeAsync(string username, string code, CancellationToken cancellationToken)
    {
        if (username == "test" && code == "test")
            return ValueTask.FromResult(new UserInfo("testId", "test"));

        throw new ForbiddenException();
    }
}

// Implement IRefreshTokenRepository
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private static readonly ConcurrentDictionary<string, RefreshToken> ConcurrentDictionary = new();

    public ValueTask<RefreshToken> AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        refreshToken.RefreshTokenKey = Guid.NewGuid().ToString();
        if (!ConcurrentDictionary.TryAdd(refreshToken.RefreshTokenKey, refreshToken))
        {
            throw new Exception("Add refresh token error.");
        }

        return ValueTask.FromResult(refreshToken);
    }

    public ValueTask<RefreshToken?> GetOrDefaultAsync(string refreshTokenKey, CancellationToken cancellationToken)
    {
        ConcurrentDictionary.TryGetValue(refreshTokenKey, out var value);
        return ValueTask.FromResult(value);
    }

    public ValueTask RemoveAsync(RefreshToken entity, CancellationToken cancellationToken)
    {
        ConcurrentDictionary.TryRemove(entity.RefreshTokenKey, out var value);
        return ValueTask.CompletedTask;
    }

    public ValueTask<UserInfo?> GetUserInfoByTokenOrDefaultAsync(RefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        ConcurrentDictionary.TryGetValue(refreshToken.RefreshTokenKey, out var token);
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