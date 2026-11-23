[![Build and Test](https://github.com/dzmprt/MitMediator.AppAuthorize/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dzmprt/MitMediator.AppAuthorize/actions/workflows/dotnet.yml)
![NuGet](https://img.shields.io/nuget/v/MitMediator.AppAuthorize)
![.NET 10](https://img.shields.io/badge/Version-.NET%2010-informational?style=flat&logo=dotnet)

# MitMediator.AppAuthorize

## Extension for [MitMediator](https://github.com/dzmprt/MitMediator) that simplifies authentication and authorization

- [Installation](#installation)
- [How to use](#how-to-use)
- [Additional settings](#additional-settings)
- [Example of web API with login and password](#example-of-web-api-with-login-and-password)
- [Extension for HttpMediator](#extension-for-httpmediator)
- [Samples](./samples)
- [License](LICENSE)

## Installation

### 1. Add package

```bash
# for application layer
dotnet add package MitMediator.AppAuthorize -v 10.0.1-rc-3

# for ASP.NET projects
dotnet add package MitMediator.AppAuthorize.Web -v 10.0.1-rc-3
```

### 2. Register application services

```csharp
// Register handlers and IMediator
builder.Services.AddMitMediator();

// Register the authorization pipeline
builder.Services.AddAppAuthorize();

// Inject IUserAuthenticator implementation
builder.Services.AddScoped<IUserAuthenticator, UserAuthenticator>();

// Inject IRefreshTokenRepository implementation
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
```

### 3. Register web services

```csharp
// Add default auth context (or implement and inject IAuthenticationContext)
builder.Services.AddDefaultAuthContext();

// Add auth services and configure TokenValidationParameters
builder.Services.AddJwtAuthServices(signingKey, "my-api", "my-api-client");
```

### 4. Configure middleware

```csharp
//Optional: Behavior for mapping custom auth exceptions to correct http status response: Unauthorized - 401, Forbidden - 403
app.UseAuthException();

app.UseAuthentication();
app.UseAuthorization();
```

### 5. Map authentication endpoints

```csharp
app.MapAuthApi();
```

## How to use

By default, all requests require authorization

To customize a required role or tenant, use attribute `AppAuthorizeAttribute`
```csharp
[AppAuthorize(["SomeRoleName", "SecondRoleName"], ["TestTenantId"])]
public class GetTestRequest : IRequest { }
```

To allow anonymous access to a request, use attribute `AppAllowAnonymousAttribute`
```csharp
[AppAllowAnonymous]
public class GetTestRequest : IRequest { }
```

To get access to user info in the application layer use `IAuthenticationContext`
```csharp
public class GetAuthStatusQueryHandler(IAuthenticationContext authenticationContext)
    : IRequestHandler<GetAuthStatusQuery, bool>
{
    public ValueTask<bool> HandleAsync(GetAuthStatusQuery request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(authenticationContext.IsAuthenticated);
}
```

## Additional settings

**Change the route prefix** from the default /auth:

```csharp
app.MapAuthApi(routePrefix: "identity");
```

**Enable code sign-in** alongside or instead of password sign-in. Register your implementation before building the app:

```csharp
builder.Services.AddScoped<IUserAuthenticatorByCode, UserAuthenticatorByCode>();
```

To keep both methods:

```csharp
app.MapAuthApi(useCode: true);
```

For code sign-in only, omit the `IUserAuthenticator` registration and use:

```csharp
app.MapAuthApi(usePassword: false, useCode: true);
```

**Disable refresh tokens** and omit the `IRefreshTokenRepository` registration:

```csharp
app.MapAuthApi(useRefreshToken: false);
```

You can add a superuser role for an admin or the system:
```csharp
services.AddAppAuthorize("admin");
```

To enable rate limiting, pass a registered policy name to `MapAuthApi`, for example `"auth"`:

```csharp
app.UseRateLimiter();

app.MapAuthApi(rateLimitPolicy: "auth");
```

## Example of web API with login and password

This local example uses in-memory SQLite. For rate limiting and Swagger, see [SimpleWebApi](./samples/SimpleWebApi).

```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using MitMediator;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Domain;
using MitMediator.AppAuthorize.Web;

var builder = WebApplication.CreateBuilder(args);

// Add MitMediator
builder.Services.AddMitMediator();
// Authorization pipline behavior
builder.Services.AddAppAuthorize();
// Add default auth context
builder.Services.AddDefaultAuthContext();
// Inject IUserAuthenticator implementation
builder.Services.AddScoped<IUserAuthenticator, UserAuthenticator>();
// Inject IRefreshTokenRepository implementation
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
// Auth and jwt services
builder.Services.AddJwtAuthServices(
    "JwtSigningKey1234567890098765433123123123232323",
    "sample-api",
    "sample-api-client");

var app = builder.Build();

// Keep the in-memory database alive while the app runs.
using var keepAlive = new SqliteConnection(RefreshTokenRepository.ConnectionString);
keepAlive.Open();
RefreshTokenRepository.InitializeDatabase(keepAlive);

app.UseAuthException();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthApi();
app.MapGet("/auth-status", (IMediator mediator, CancellationToken cancellationToken) =>
    mediator.SendAsync<GetAuthStatusQuery, bool>(new GetAuthStatusQuery(), cancellationToken));
app.Run();
```

<details>
<summary>Demo authenticator, SQLite repository, and request handler</summary>

Add these types below the startup code:

```csharp

// Implement IUserAuthenticator for sign-in logic
public class UserAuthenticator : IUserAuthenticator
{
    internal const string DemoUserId = "testId";

    internal static UserInfo CreateDemoUser() => new(DemoUserId, "test");

    public ValueTask<UserInfo?> AuthByPasswordAsync(string username, string password,
        CancellationToken cancellationToken)
    {
        if (username == "test" && password == "test")
            return ValueTask.FromResult<UserInfo?>(CreateDemoUser());

        return ValueTask.FromResult<UserInfo?>(null);
    }
}

// Implement IRefreshTokenRepository
public class RefreshTokenRepository : IRefreshTokenRepository
{
    internal const string ConnectionString =
        "Data Source=refresh-tokens;Mode=Memory;Cache=Shared;Pooling=False";

    public static void InitializeDatabase(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS RefreshTokens (
                TokenHash TEXT NOT NULL PRIMARY KEY,
                UserId TEXT NOT NULL,
                ExpiresAtUtcTicks INTEGER NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public ValueTask<RefreshToken> AddAsync(
        string userId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        var token = InsertToken(connection, transaction, userId, expiresAt);
        cancellationToken.ThrowIfCancellationRequested();
        transaction.Commit();
        return ValueTask.FromResult(token);
    }

    public ValueTask<RefreshTokenRotation?> RotateAsync(
        string refreshTokenKey,
        string userId,
        DateTimeOffset newTokenExpiration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId != UserAuthenticator.DemoUserId)
        {
            return ValueTask.FromResult<RefreshTokenRotation?>(null);
        }

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        using var consume = connection.CreateCommand();
        consume.Transaction = transaction;
        consume.CommandText = """
            DELETE FROM RefreshTokens
            WHERE TokenHash = $hash AND UserId = $userId AND ExpiresAtUtcTicks > $now;
            """;
        consume.Parameters.AddWithValue("$hash", HashToken(refreshTokenKey));
        consume.Parameters.AddWithValue("$userId", userId);
        consume.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.UtcTicks);
        if (consume.ExecuteNonQuery() != 1)
        {
            return ValueTask.FromResult<RefreshTokenRotation?>(null);
        }

        var replacement = InsertToken(connection, transaction, userId, newTokenExpiration);
        cancellationToken.ThrowIfCancellationRequested();
        transaction.Commit();
        return ValueTask.FromResult<RefreshTokenRotation?>(
            new RefreshTokenRotation(UserAuthenticator.CreateDemoUser(), replacement));
    }

    private static RefreshToken InsertToken(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string userId,
        DateTimeOffset expiresAt)
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO RefreshTokens (TokenHash, UserId, ExpiresAtUtcTicks)
            VALUES ($hash, $userId, $expiresAt);
            """;
        insert.Parameters.AddWithValue("$hash", HashToken(key));
        insert.Parameters.AddWithValue("$userId", userId);
        insert.Parameters.AddWithValue("$expiresAt", expiresAt.UtcTicks);
        insert.ExecuteNonQuery();
        return new RefreshToken(key, userId, expiresAt);
    }

    private static string HashToken(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
}


[AppAllowAnonymous]
public class GetAuthStatusQuery : IRequest<bool>;

public class GetAuthStatusQueryHandler(IAuthenticationContext authenticationContext)
    : IRequestHandler<GetAuthStatusQuery, bool>
{
    public ValueTask<bool> HandleAsync(GetAuthStatusQuery request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(authenticationContext.IsAuthenticated);
}

```

</details>

# Extension for HttpMediator

Extension for `MitMediator.AutoApi.HttpMediator` with helpers for authorization tokens

### 1. Add package

```bash
dotnet add package MitMediator.AppAuthorize.ClientMediator -v 10.0.1-rc-3
```

### 2. Use extension methods

```csharp
// Get a token by login and password
var token = await httpMediator.GetJwtByPasswordAsync("test", "test");

// Get a token by code
var tokenByCode = await httpMediator.GetJwtByCodeAsync("test", "test");

// Get a token by refresh token key
var refreshedToken = await httpMediator.GetJwtByRefreshAsync(
    token.UserId,
    token.RefreshToken!);

// The server and client can use a custom auth route prefix
var tokenByCustomPrefix = await httpMediator.GetJwtByPasswordAsync(
    "test",
    "test",
    routePrefix: "api/identity");
```

The client decodes JWTs for convenience; the server validates them.

### [See more samples](./samples)
