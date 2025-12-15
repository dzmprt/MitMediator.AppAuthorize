using Books.Application;
using Books.Infrastructure;
using Books.WebApi.JwtBearerAuthorize;
using Books.WebApi.JwtBearerAuthorize.Endpoints;
using Books.WebApi.JwtBearerAuthorize.Middlewares;
using MitMediator.AppAuthorize.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer()
    .AddSwaggerGen(options => options.ConfigureSwagger())
    .AddDefaultAuthContext()
    .AddJwtAuthServices("somekey123456789012345678901234567890")
    .AddApplicationServices()
    .AddPersistenceServices()
    .AddPasswordHasher();

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
    .UseAuthorization();

app.MapControllers();

app.MapAuthApi(useCode:true);

app.Run();