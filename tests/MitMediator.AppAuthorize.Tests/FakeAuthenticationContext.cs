namespace MitMediator.AppAuthorize.Tests;

public class FakeAuthenticationContext : IAuthenticationContext
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string[]? UserRoles { get; set; }
    public Dictionary<string, string>? Claims { get; set; }
    public string[]? TenantIds { get; set; }
}