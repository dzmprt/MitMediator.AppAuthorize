using Books.Application.Abstractions.Infrastructure;
using Books.Domain;
using MitMediator.AppAuthorize;

namespace Books.Application.Services;

public class UserAuthenticator(IBaseProvider<User> userProvider, IPasswordHasher passwordHasher)
    : IUserAuthenticator
{
    public async ValueTask<UserInfo> AuthByPasswordAsync(string username, string password,
        CancellationToken cancellationToken)
    {
        var normalizedName = username?.ToUpperInvariant().Trim();
        var user = await userProvider.FirstOrDefaultAsync(u => u.Name == normalizedName, cancellationToken);
        if (user == null)
        {
            throw new ForbiddenException("Invalid username.");
        }

        if (!passwordHasher.VerifyHash(password, user.PasswordHash))
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