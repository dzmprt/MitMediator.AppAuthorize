using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Books.WebApi.BasicAuthorize;

internal static class SwaggerGenOptionsExtension
{
    public static void AddBasicAuth(this SwaggerGenOptions options)
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
        
        options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "basic",
            In = ParameterLocation.Header
        });
        
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("basic", document)] = []
        });
    }
}