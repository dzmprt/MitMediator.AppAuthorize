namespace MitMediator.AppAuthorize.Tests;

public class AppAuthorizeClaimTypesTests
{
    [Fact]
    public void TenantClaim_ShouldHaveExpectedValue()
    {
        // Act
        var claimType = AppAuthorizeClaimTypes.Tenant;

        // Assert
        Assert.Equal("tenant", claimType);
    }

    [Fact]
    public void TenantClaim_ShouldBeConstant()
    {
        // Arrange
        var fieldInfo = typeof(AppAuthorizeClaimTypes).GetField(nameof(AppAuthorizeClaimTypes.Tenant));

        // Assert
        Assert.NotNull(fieldInfo);
        Assert.True(fieldInfo.IsLiteral && !fieldInfo.IsInitOnly);
    }
}