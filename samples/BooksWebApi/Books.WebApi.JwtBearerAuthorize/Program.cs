using System.Reflection;
using Books.Application;
using Books.Infrastructure;
using Books.WebApi.JwtBearerAuthorize;
using Books.WebApi.JwtBearerAuthorize.Endpoints;
using Books.WebApi.JwtBearerAuthorize.Middlewares;
using Microsoft.OpenApi.Models;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Web;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Books api", Version = "v1",
        Description = "Sample API project <br/><br/> User by default: Username:\"test\", Password: \"test\"",
        Contact = new OpenApiContact()
        {
            Name = "MitMediator project",
            Url = new Uri("https://github.com/dzmprt/MitMediator"),
        }
    });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    options.AddJwtAuth();
});

builder.Services.AddDefaultAuthContext();
builder.Services.AddJwtAuthServices("somekey123456789012345678901234567890");

builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices();
builder.Services.AddPasswordHasher();

var app = builder.Build();

app.InitDatabase();

app.UseCustomExceptionsHandler();
app.UseAuthException();

app.UseAuthorsApi();
app.UseGenresApi();
app.UseBooksApi();
app.UseUsersApi();

app.UseSwagger(c => { c.RouteTemplate = "swagger/{documentname}/swagger.json"; })
    .UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = "swagger";
    });

app.UseHttpsRedirection();

app.UseAuthentication()
    .UseAuthorization();

app.MapControllers();

app.MapJwtAuthApi();

app.Run();