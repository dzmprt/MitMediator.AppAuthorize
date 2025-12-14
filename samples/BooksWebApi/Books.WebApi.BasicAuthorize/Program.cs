using Books.Application;
using Books.Infrastructure;
using Books.WebApi.BasicAuthorize;
using Books.WebApi.BasicAuthorize.Endpoints;
using Books.WebApi.BasicAuthorize.Middlewares;
using MitMediator.AppAuthorize.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer()
    .AddSwaggerGen(options => options.AddBasicAuth())
    .AddDefaultAuthContext()
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
app.UseBasicAuth();

app.Run();