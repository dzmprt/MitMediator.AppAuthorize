namespace MitMediator.AppAuthorize.Tests;

public class FakeAuthenticationContext : IAuthenticationContext
{
    public bool IsAuthenticated => UserId is not null;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string[]? UserRoles { get; set; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Claims { get; set; }
    public string[]? TenantIds { get; set; }
}