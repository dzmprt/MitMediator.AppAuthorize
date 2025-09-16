namespace MitMediator.AppAuthorize;

/// <summary>
/// Represents user information for authentication and authorization.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public string UserId { get; private set; }

    /// <summary>
    /// Gets the user name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the user roles.
    /// </summary>
    public string[]? Roles { get; }

    /// <summary>
    /// Gets the user tenants.
    /// </summary>
    public string[]? Tenants { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserInfo"/> class.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The user name.</param>
    /// <param name="roles">The user roles.</param>
    /// <param name="tenants">The user tenants.</param>
    public UserInfo(string userId, string? name, string[]? roles = null, string[]? tenants = null)
    {
        UserId = userId;
        Name = name;
        Roles = roles;
        Tenants = tenants;
    }
}