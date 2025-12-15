using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleWebApiJwtAuth;

public static class SwaggerGenOptionsExtension
{
    public static void ConfigureSwagger(this SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Description = "Username: <b>test</b>, Password: <b>test</b>, Code: <b>test</b>",
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
    }
}