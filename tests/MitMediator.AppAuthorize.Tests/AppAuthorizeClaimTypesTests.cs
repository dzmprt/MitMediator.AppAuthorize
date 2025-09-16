namespace MitMediator.AppAuthorize.Tests;

public class AppAuthorizeClaimTypesTests
{
    [Fact]
    public void TenantClaim_ShouldHaveExpectedValue()
    {
        // Act
        var claimType = AppAuthorizeClaimTypes.TenantId;

        // Assert
        Assert.Equal("http://schemas.microsoft.com/identity/claims/tenantid", claimType);
    }

    [Fact]
    public void TenantClaim_ShouldBeConstant()
    {
        // Arrange
        var fieldInfo = typeof(AppAuthorizeClaimTypes).GetField(nameof(AppAuthorizeClaimTypes.TenantId));

        // Assert
        Assert.NotNull(fieldInfo);
        Assert.True(fieldInfo.IsLiteral && !fieldInfo.IsInitOnly);
    }
}