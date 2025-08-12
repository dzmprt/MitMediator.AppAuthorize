namespace Books.Application.Abstractions.Infrastructure;

/// <summary>
/// Password hasher.
/// </summary>
public interface IPasswordHasher
{ 
    /// <summary>
    /// Get hash of string.
    /// </summary>
    /// <param name="str">Target string.</param>
    /// <returns>Hash of string.</returns>
    string GetHash(string str);
    
    /// <summary>
    /// Verify hash.
    /// </summary>
    /// <param name="str">Target string.</param>
    /// <param name="hashedStr">Hash of string.</param>
    /// <returns>True if hashedStr is hash of the specified string.</returns>
    bool VerifyHash(string str, string hashedStr);
}