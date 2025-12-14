using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleWebApiJwtAuth;

public static class SwaggerGenOptionsExtension
{
    public static void AddJwtAuth(this SwaggerGenOptions options)
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
    }
}