using System.Reflection;
using Books.Application;
using Books.Infrastructure;
using Books.WebApi.BasicAuthorize;
using Microsoft.OpenApi.Models;
using Books.WebApi.BasicAuthorize.Endpoints;
using Books.WebApi.BasicAuthorize.Middlewares;
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
    // Add basic auth to swagger
    options.AddBasicAuth();
});

builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices();
builder.Services.AddPasswordHasher();

// Add default auth context
builder.Services.AddDefaultAuthContext();

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

app.UseAuthorization();
app.UseBasicAuth();

app.MapControllers();

app.Run();