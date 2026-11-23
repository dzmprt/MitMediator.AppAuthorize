using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MitMediator;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Domain;
using MitMediator.AppAuthorize.Web;
using SimpleWebApi;

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
// Auth services
builder.Services.AddJwtAuthServices(
    "key1234567890098765433123123123232323",
    "simple-web-api",
    "simple-web-api-client",
    inputLimits: new AuthInputLimits(codeLength: 32));
    
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress.ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});
// Add swagger
builder.Services.AddSwaggerGen(options => options.ConfigureSwagger());

var app = builder.Build();

// Handler auth exceptions (Forbidden, Unauthorized)
app.UseAuthException();

app.UseSwagger()
    .UseSwaggerUI();

app.UseAuthentication()
    .UseAuthorization()
    .UseRateLimiter();

// Map authorization endpoints
app.MapAuthApi(useCode: true, rateLimitPolicy: "auth");

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
    public ValueTask<UserInfo?> AuthByPasswordAsync(string username, string password,
        CancellationToken cancellationToken)
    {
        if (username == "test" && password == "test")
            return ValueTask.FromResult<UserInfo?>(
                new UserInfo("testId", "test"));

        return ValueTask.FromResult<UserInfo?>(null);
    }
}

public class UserAuthenticatorByCode : IUserAuthenticatorByCode
{
    public ValueTask<UserInfo?> AuthByCodeAsync(string username, string code, CancellationToken cancellationToken)
    {
        if (username == "test" && code == "test")
            return ValueTask.FromResult<UserInfo?>(
                new UserInfo("testId", "test"));

        return ValueTask.FromResult<UserInfo?>(null);
    }
}

// Implement IRefreshTokenRepository
public class RefreshTokenRepository : IRefreshTokenRepository
{
    // Example only. In production use an atomic database operation or transaction.
    private static readonly ConcurrentDictionary<string, RefreshToken> Tokens = new();
    private static readonly object TokensLock = new();

    public ValueTask<RefreshToken> AddAsync(
        string userId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        lock (TokensLock)
        {
            while (true)
            {
                var refreshToken = new RefreshToken(
                    CreateRefreshTokenKey(),
                    userId,
                    expiresAt);
                if (Tokens.TryAdd(refreshToken.RefreshTokenKey, refreshToken))
                {
                    return ValueTask.FromResult(refreshToken);
                }
            }
        }
    }

    public ValueTask<RefreshTokenRotation?> RotateAsync(
        string refreshTokenKey,
        string userId,
        DateTimeOffset newTokenExpiration,
        CancellationToken cancellationToken)
    {
        lock (TokensLock)
        {
            if (!Tokens.TryGetValue(refreshTokenKey, out var oldToken) ||
                oldToken.UserId != userId ||
                oldToken.ExpiresAt <= DateTimeOffset.UtcNow ||
                !Tokens.TryRemove(refreshTokenKey, out _))
            {
                return ValueTask.FromResult<RefreshTokenRotation?>(null);
            }

            RefreshToken replacement;
            do
            {
                replacement = new RefreshToken(
                    CreateRefreshTokenKey(),
                    userId,
                    newTokenExpiration);
            }
            while (!Tokens.TryAdd(
                       replacement.RefreshTokenKey,
                       replacement));

            return ValueTask.FromResult<RefreshTokenRotation?>(
                new RefreshTokenRotation(
                    new UserInfo(oldToken.UserId, oldToken.UserId),
                    replacement));
        }
    }

    private static string CreateRefreshTokenKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
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