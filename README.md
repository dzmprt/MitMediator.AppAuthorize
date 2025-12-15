[![Build and Test](https://github.com/dzmprt/MitMediator.AppAuthorize/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dzmprt/MitMediator.AppAuthorize/actions/workflows/dotnet.yml)
![NuGet](https://img.shields.io/nuget/v/MitMediator.AppAuthorize)
![.NET 10](https://img.shields.io/badge/Version-.NET%2010-informational?style=flat&logo=dotnet)

# MitMediator.AppAuthorize

## Extension for [MitMediator](https://github.com/dzmprt/MitMediator) that simplifies authentication and authorization via basic auth or JWT bearer tokens

- [Installation for basic auth](#-installation-for-basic-auth)
- [Installation for jwt bearer auth](#-installation-for-jwt-bearer-auth)
- [How to use](#how-to-use)
- [Example of web api with jwt bearer auth](#example-of-web-api-with-jwt-bearer-auth)
- [Extension for HttpMediator](#extension-for-httpmediator)
- [Samples](./samples)
- [License](LICENSE)

## Installation for basic auth

### 1. Add package

```bash
# for application layer
dotnet add package MitMediator.AppAuthorize -v 10.0.0-alfa-2

# for ASP.NET projects
dotnet add package MitMediator.AppAuthorize.Web -v 10.0.0-alfa-2
```

### 2. Inject services for application layer

```csharp
// Register handlers and IMediator
services.AddMitMediator();

// Register authorization pipe
services.AddAppAuthorize();

// Inject IUserAuthenticator implementation
builder.Services.AddScoped<IUserAuthenticator, UserAuthenticator>();
```

### 3. Inject services for web layer

```csharp
// Add default auth context (or implement and inject IAuthenticationContext)
builder.Services.AddDefaultAuthContext();

````

### 4. Use basic auth middleware

```csharp
app.UseBasicAuth();
```

### 5. (Optional) Use middleware to handle auth exceptions

```csharp
// Unauthorized - 401, Forbidden - 403
app.UseAuthException();
```

## Installation for jwt bearer auth

### 1. Add package

```bash
# for application layer
dotnet add package MitMediator.AppAuthorize -v 10.0.0-alfa-2

# for ASP.NET projects
dotnet add package MitMediator.AppAuthorize.Web -v 10.0.0-alfa-2
```

### 2. Inject services for application layer

```csharp
// Register handlers and IMediator
services.AddMitMediator();

// Register authorization pipe
services.AddAppAuthorize();

// Inject IUserAuthenticator implementation
builder.Services.AddScoped<IUserAuthenticator, UserAuthenticator>();

// Or/and inject IUserAuthenticatorByCode implementation
// if you won't use some code for sing-in (form sms, email, etc)
// builder.Services.AddScoped<IUserAuthenticatorByCode, UserAuthenticatorByCode>();

// Inject IRefreshTokenRepository implementation
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
```

### 3. Inject services for web layer

```csharp
// Add default auth context (or implement and inject IAuthenticationContext)
builder.Services.AddDefaultAuthContext();

// Add auth services and configure TokenValidationParameters
builder.Services.AddJwtAuthServices("your-secure-key-here");
````

### 4. Map jwt endpoints

```csharp
app.MapJwtAuthApi();
```

### 5. (Optional) Use middleware to handle auth exceptions

```csharp
// Unauthorized - 401, Forbidden - 403
app.UseAuthException();
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

You can add a superuser role for an admin or the system:
```csharp
    services.AddAppAuthorize("admin");
```

> Instead of a refresh token key, you may use any alternative code, such as a one-time code received via **SMS** or **email**.  
To support this logic, implement the `IUserAuthenticatorByCode`.

## Example of web api with jwt bearer auth by login and password

```csharp
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
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
// Authorization pipline behavior
builder.Services.AddAppAuthorize();
// Auth and jwt services
builder.Services.AddJwtAuthServices("key1234567890098765433123123123232323");

// Add jwt auth to swagger (use Swashbuckle.AspNetCore and Swashbuckle.AspNetCore.SwaggerUI)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Description = "Username: <b>test</b>, Password: <b>test</b>",
    });
        
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = """
                      JWT Authorization header using the Bearer scheme. \r\n\r\n
                                            Enter 'Bearer' [space] and then your token in the text input below.
                                            \r\n\r\nExample: 'Bearer 12345abcdef'
                      """,
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// Handle auth exceptions(Forbidden, Unauthorized)
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


// Implement IUserAuthenticator for sign-in logic
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

    public ValueTask<RefreshToken?> GetOrDefaultAsync(string RefreshTokenKey, CancellationToken cancellationToken)
    {
        _concurrentDictionary.TryGetValue(RefreshTokenKey, out var value);
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
```

# Extension for HttpMediator

Extension for `MitMediator.AutoApi.HttpMediator` that enables JWT Bearer authorization

### 1. Add package

```bash
dotnet add package MitMediator.AppAuthorize.ClientMediator -v 9.0.0-alfa
```

### 2. Use extension methods

```csharp
// Get jwt token by login and password
var jwtToken = await httpMediator.GetJwtByPasswordAsync("test", "test", CancellationToken.None);

// Get jwt token by code and password
var jwtTokenByCode = await httpMediator.GetJwtByCodeAsync("test", "test", CancellationToken.None);

// Get jwt token by refresh token key
var jwtByRefreshToken = await httpMediator.GetJwtByRefreshAsync(jwtToken.UserId, jwtToken.RefreshToken, CancellationToken.None);
```


### [See more samples](./samples)
