namespace MitMediator.AppAuthorize;

/// <summary>
/// Represents user information for authentication and authorization.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Gets the user name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the user roles.
    /// </summary>
    public IReadOnlyList<string>? Roles { get; }

    /// <summary>
    /// Gets the user tenants.
    /// </summary>
    public IReadOnlyList<string>? Tenants { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserInfo"/> class.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The user name.</param>
    /// <param name="roles">The user roles.</param>
    /// <param name="tenants">The user tenants.</param>
    public UserInfo(string userId, string? name, string[]? roles = null, string[]? tenants = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        UserId = userId.Trim();
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Roles = NormalizeValues(roles);
        Tenants = NormalizeValues(tenants);
    }

    private static IReadOnlyList<string>? NormalizeValues(string[]? values)
    {
        var normalized = values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return normalized is null ? null : Array.AsReadOnly(normalized);
    }
}