using Books.Application.Abstractions.Infrastructure;
using Books.Domain;
using Microsoft.AspNetCore.Identity;

namespace Books.Infrastructure;

public class PasswordHasher : IPasswordHasher
{
    public string GetHash(string str)
    {
        return new PasswordHasher<User>().HashPassword(null, str); 
    }

    public bool VerifyHash(string str, string hashedStr)
    {
        return new PasswordHasher<User>().VerifyHashedPassword(null, hashedStr, str) == PasswordVerificationResult.Success;
    }
}