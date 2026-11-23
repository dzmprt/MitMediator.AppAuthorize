using System.Collections.ObjectModel;
using System.Security.Claims;

namespace MitMediator.AppAuthorize.Web;

/// <summary>
/// Default implementation of <see cref="IAuthenticationContext"/> that retrieves user information from the current HTTP context.
/// </summary>
public class DefaultAuthenticationContext(
    IHttpContextAccessor httpContextAccessor) : IAuthenticationContext
{
    /// <inheritdoc />
    public bool IsAuthenticated => GetIdentity() is not null;

    /// <inheritdoc />
    public string? UserId =>
        GetIdentity()?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <inheritdoc />
    public string? UserName => GetIdentity()?.Name;

    /// <inheritdoc />
    public string[]? UserRoles
    {
        get
        {
            var identity = GetIdentity();
            return identity?
                .FindAll(identity.RoleClaimType)
                .Select(claim => claim.Value)
                .ToArray();
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Claims
    {
        get
        {
            var identity = GetIdentity();
            if (identity is null)
            {
                return null;
            }

            return new ReadOnlyDictionary<
                string,
                IReadOnlyList<string>>(
                identity.Claims
                    .GroupBy(claim => claim.Type)
                    .ToDictionary(
                        group => group.Key,
                        group => (IReadOnlyList<string>)Array.AsReadOnly(
                            group
                                .Select(claim => claim.Value)
                                .ToArray())));
        }
    }

    /// <inheritdoc />
    public string[]? TenantIds =>
        GetIdentity()?
            .FindAll(AppAuthorizeClaimTypes.TenantId)
            .Select(claim => claim.Value)
            .ToArray();

    private ClaimsIdentity? GetIdentity() =>
        httpContextAccessor.HttpContext?.User.Identities
            .Where(candidate => candidate.IsAuthenticated)
            .Take(2)
            .ToArray() is { Length: 1 } identities
            && identities[0]
                .FindAll(ClaimTypes.NameIdentifier)
                .Take(2)
                .ToArray() is { Length: 1 } userIds
            && !string.IsNullOrWhiteSpace(userIds[0].Value)
                ? identities[0]
                : null;
}