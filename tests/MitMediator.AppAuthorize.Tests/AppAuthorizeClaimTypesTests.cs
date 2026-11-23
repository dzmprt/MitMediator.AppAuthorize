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
}