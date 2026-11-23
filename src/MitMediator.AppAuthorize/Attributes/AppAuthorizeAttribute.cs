namespace MitMediator;

/// <summary>
/// Specifies required roles and/or tenants for accessing a request.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false)]
public class AppAuthorizeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new authorization attribute.
    /// </summary>
    /// <param name="roles">The roles required to access the request.</param>
    /// <param name="tenantsIds">The tenant identifiers required to access the request.</param>
    public AppAuthorizeAttribute(
        string[]? roles = null,
        string[]? tenantsIds = null)
    {
        Roles = Normalize(
            roles,
            nameof(roles),
            "role");
        TenantsIds = Normalize(
            tenantsIds,
            nameof(tenantsIds),
            "tenant identifier");
    }

    /// <summary>
    /// Roles required to access the request.
    /// </summary>
    public string[]? Roles { get; }

    /// <summary>
    /// Tenants identifiers required to access the request.
    /// </summary>
    public string[]? TenantsIds { get; }

    private static string[]? Normalize(
        string[]? values,
        string parameterName,
        string valueName)
    {
        if (values is null)
        {
            return null;
        }

        var normalized = new string[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"The {valueName} values cannot contain blank entries.",
                    parameterName);
            }

            normalized[index] = value.Trim();
        }

        if (normalized
            .GroupBy(value => value, StringComparer.Ordinal)
            .Any(group => group.Skip(1).Any()))
        {
            throw new ArgumentException(
                $"The {valueName} values cannot contain duplicates.",
                parameterName);
        }

        return normalized;
    }
}