using Books.Application;
using Books.Infrastructure;
using Books.WebApi;
using Books.WebApi.Endpoints;
using Books.WebApi.Middlewares;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer()
    .AddSwaggerGen(options => options.ConfigureSwagger())
    .AddDefaultAuthContext()
    .AddJwtAuthServices(
        "somekey123456789012345678901234567890",
        "books-web-api",
        "books-web-api-client")
    .AddApplicationServices()
    .AddPersistenceServices()
    .AddPasswordHasher();
    
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

var app = builder.Build();

app.InitDatabase();

app.UseCustomExceptionsHandler()
    .UseAuthException();

app.UseAuthorsApi()
    .UseGenresApi()
    .UseBooksApi()
    .UseUsersApi();

app.UseSwagger()
    .UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication()
    .UseAuthorization()
    .UseRateLimiter();

app.MapControllers();

app.MapAuthApi(useCode: true, rateLimitPolicy: "auth");

app.Run();