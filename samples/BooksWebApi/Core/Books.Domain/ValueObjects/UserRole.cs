namespace Books.Domain.ValueObjects;

/// <summary>
/// User role
/// </summary>
public record UserRole
{
    /// <summary>
    /// Max User role length.
    /// </summary>
    public const int MaxUserRoleNameLength = 1000;
    
    /// <summary>
    /// User role name.
    /// </summary>
    public string Role { get; private set; }

    private UserRole(){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRole"/>.
    /// </summary>
    /// <param name="roleName">Role name.</param>
    /// <exception cref="ArgumentException">Incorrect role name.</exception>
    public UserRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException($"{nameof(roleName)} is empty.", nameof(roleName));
        }
        if (roleName.Length > MaxUserRoleNameLength)
        {
            throw new ArgumentException($"{nameof(roleName)} cannot exceed {MaxUserRoleNameLength} characters.", nameof(roleName));
        }
        Role = roleName.Trim().ToUpperInvariant();
    }
}