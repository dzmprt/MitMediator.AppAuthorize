using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MitMediator.AppAuthorize.Web.Tests;

public class SwaggerGenOptionsExtensionTests
{
    [Fact]
    public void AddJwtAuth_ShouldRegisterBearerSecurityDefinition()
    {
        // Arrange
        var options = new SwaggerGenOptions();

        // Act
        options.AddJwtAuth();

        var securitySchemes = options.SwaggerGeneratorOptions.SecuritySchemes;
        
        // Assert
        Assert.NotNull(securitySchemes);
        Assert.True(securitySchemes.ContainsKey("Bearer"));

        var scheme = securitySchemes["Bearer"];
        Assert.Equal("Bearer", scheme.Scheme);
        Assert.Equal(SecuritySchemeType.ApiKey, scheme.Type);
        Assert.Equal(ParameterLocation.Header, scheme.In);
        Assert.Equal("Authorization", scheme.Name);
    }

    [Fact]
    public void AddBasicAuth_ShouldRegisterBasicSecurityDefinition()
    {
        // Arrange
        var options = new SwaggerGenOptions();

        // Act
        options.AddBasicAuth();

        var securitySchemes = options.SwaggerGeneratorOptions.SecuritySchemes;
        
        // Assert
        Assert.NotNull(securitySchemes);
        Assert.True(securitySchemes.ContainsKey("basic"));

        var scheme = securitySchemes["basic"];
        Assert.Equal("basic", scheme.Scheme);
        Assert.Equal(SecuritySchemeType.Http, scheme.Type);
        Assert.Equal(ParameterLocation.Header, scheme.In);
        Assert.Equal("Authorization", scheme.Name);
    }
}