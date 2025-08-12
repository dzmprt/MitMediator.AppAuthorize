using Books.Application.Abstractions.Infrastructure;
using Books.Domain;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Domain;

namespace Books.Infrastructure;

public class RefreshTokenRepository(IBaseRepository<RefreshToken> refreshTokensRepository, IBaseProvider<User> usersProvider) : IRefreshTokenRepository
{
    public async ValueTask<RefreshToken> AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        refreshToken.RefreshTokenKey = Guid.NewGuid().ToString();
        return await refreshTokensRepository.AddAsync(refreshToken, cancellationToken);
    }

    public async ValueTask<RefreshToken?> GetOrDefaultAsync(string refreshTokenKey, CancellationToken cancellationToken)
    {
        return await refreshTokensRepository.SingleOrDefaultAsync(c => c.RefreshTokenKey == refreshTokenKey, cancellationToken);
    }

    public async ValueTask RemoveAsync(RefreshToken entity, CancellationToken cancellationToken)
    {
        await refreshTokensRepository.RemoveAsync(entity, cancellationToken);
    }

    public async ValueTask<UserInfo?> GetUserInfoByTokenOrDefaultAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        var token = await refreshTokensRepository.SingleOrDefaultAsync(c => c.RefreshTokenKey == refreshToken.RefreshTokenKey, cancellationToken);
        if (token is null)
        {
            return  null;
        }

        var userId = int.Parse(token.UserId);
        var user = await usersProvider.SingleAsync(c => c.UserId == userId, cancellationToken);
        return new UserInfo(user.UserId.ToString(), user.Name, [user.Role.Role], [user.Tenant.TenantId]);
    }
}
