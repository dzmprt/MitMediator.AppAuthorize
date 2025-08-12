namespace MitMediator.AppAuthorize;

/// <summary>
/// User info.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// User id.
    /// </summary>
    public string UserId { get; private set; }

    /// <summary>
    /// User name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// User roles.
    /// </summary>
    public string[]? Roles { get;  }

    /// <summary>
    /// User tenants.
    /// </summary>
    public string[]? Tenants { get;}

    /// <summary>
    /// Creat user info.
    /// </summary>
    /// <param name="userId"><see cref="UserId"/>.</param>
    /// <param name="name"><see cref="Name"/>.</param>
    /// <param name="roles"><see cref="Roles"/>.</param>
    /// <param name="tenants"><see cref="Tenants"/>.</param>
    public UserInfo(string userId, string? name, string[]? roles = null, string[]? tenants = null)
    {
        UserId = userId;
        Name = name;
        Roles = roles;
        Tenants = tenants;
    }
}