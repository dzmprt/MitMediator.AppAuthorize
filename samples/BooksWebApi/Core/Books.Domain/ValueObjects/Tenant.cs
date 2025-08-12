
namespace Books.Domain.ValueObjects;

/// <summary>
/// Tenant.
/// </summary>
public record Tenant
{
    /// <summary>
    /// Max company name length.
    /// </summary>
    public const int MaxCompanyNameLength = 1000;
    
    /// <summary>
    /// Tenant id.
    /// </summary>
    public string TenantId { get; private set; }
    
    /// <summary>
    /// Company name.
    /// </summary>
    public string CompanyName { get; private set; }

    private Tenant(){}
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Tenant"/>.
    /// </summary>
    /// <param name="companyName">Company name.</param>
    /// <exception cref="ArgumentException">Incorrect company name.</exception>
    public Tenant(string companyName)
    {
        SetName(companyName);
    }
    
    /// <summary>
    /// Set company name.
    /// </summary>
    /// <param name="companyName">Company name.</param>
    /// <exception cref="ArgumentException">Incorrect company name.</exception>
    private void SetName(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new ArgumentException($"{nameof(companyName)} is empty.", nameof(companyName));
        }

        if (companyName.Length > MaxCompanyNameLength)
        {
            throw new ArgumentException($"{nameof(companyName)} cannot exceed {MaxCompanyNameLength} characters.", nameof(companyName));
        }
        CompanyName = companyName.Trim().ToUpperInvariant();
    }
}