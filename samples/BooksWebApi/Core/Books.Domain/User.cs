using Books.Domain.ValueObjects;

namespace Books.Domain;

public class User
{
    /// <summary>
    /// Max name length.
    /// </summary>
    public const int MaxNameLength = 1000;
    
    public int UserId { get; set; }
    
    public string Name { get; private set; }
    
    public string PasswordHash { get; private set; }
    
    public UserRole Role { get; private set; }

    public Tenant Tenant { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private User(){}
    public User(string name, string passwordHash, UserRole role, Tenant tenant)
    {
        SetName(name);
        PasswordHash = passwordHash;
        Role = role;
        Tenant = tenant;
        CreatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Set name.
    /// </summary>
    /// <param name="name">Title.</param>
    /// <exception cref="ArgumentException">Incorrect name.</exception>
    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"{nameof(name)} is empty.", nameof(name));
        }

        if (name.Length > MaxNameLength)
        {
            throw new ArgumentException($"{nameof(name)} cannot exceed {MaxNameLength} characters.", nameof(name));
        }
        Name = name.Trim().ToUpperInvariant();
    }
}