using Books.Application.Abstractions.Infrastructure;
using Books.Domain;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Exceptions;

namespace Books.Application.Services;

public class AuthenticatorByCode(IBaseProvider<User> userProvider, IPasswordHasher passwordHasher)
    : IUserAuthenticatorByCode
{
    public async ValueTask<UserInfo> AuthByCodeAsync(string username, string code, CancellationToken cancellationToken)
    {
        var normalizedName = username?.ToUpperInvariant().Trim();
        var user = await userProvider.FirstOrDefaultAsync(u => u.Name == normalizedName, cancellationToken);
        if (user == null)
        {
            throw new ForbiddenException("Invalid username.");
        }

        if (!passwordHasher.VerifyHash(code, user.PasswordHash))
        {
            throw new ForbiddenException("Invalid password.");
        }

        return new UserInfo(
            user.UserId.ToString(),
            user.Name,
            [user.Role.Role],
            [user.Tenant.TenantId]);
    }
}