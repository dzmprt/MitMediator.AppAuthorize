using System.Security.Cryptography;
using System.Text;
using Books.Application.Abstractions.Infrastructure;
using Books.Domain;
using MitMediator.AppAuthorize;
using MitMediator.AppAuthorize.Domain;
using Microsoft.EntityFrameworkCore;

namespace Books.Infrastructure;

public class RefreshTokenRepository(
    IBaseProvider<User> usersProvider,
    DbContext dbContext) : IRefreshTokenRepository
{
    public async ValueTask<RefreshToken> AddAsync(
        string userId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var refreshTokenKey = CreateRefreshTokenKey();
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Set<RefreshToken>().Add(new RefreshToken(
            HashRefreshTokenKey(refreshTokenKey),
            userId,
            expiresAt));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RefreshToken(refreshTokenKey, userId, expiresAt);
    }

    public async ValueTask<RefreshTokenRotation?> RotateAsync(
        string refreshTokenKey,
        string userId,
        DateTimeOffset newTokenExpiration,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var storedTokenKey = HashRefreshTokenKey(refreshTokenKey);
        var storedToken = await dbContext.Set<RefreshToken>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                token =>
                    token.RefreshTokenKey == storedTokenKey &&
                    token.UserId == userId,
                cancellationToken);
        if (storedToken is null)
        {
            return null;
        }

        var deletedCount = await dbContext.Set<RefreshToken>()
            .Where(token => token.RefreshTokenKey == storedTokenKey)
            .ExecuteDeleteAsync(cancellationToken);
        if (deletedCount != 1)
        {
            return null;
        }

        if (storedToken.ExpiresAt <= DateTimeOffset.UtcNow ||
            !int.TryParse(storedToken.UserId, out var parsedUserId))
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var user = await usersProvider.SingleOrDefaultAsync(
            candidate => candidate.UserId == parsedUserId,
            cancellationToken);
        if (user is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var replacementKey = CreateRefreshTokenKey();
        dbContext.Set<RefreshToken>().Add(new RefreshToken(
            HashRefreshTokenKey(replacementKey),
            userId,
            newTokenExpiration));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RefreshTokenRotation(
            new UserInfo(
                user.UserId.ToString(),
                user.Name,
                [user.Role.Role],
                [user.Tenant.TenantId]),
            new RefreshToken(
                replacementKey,
                userId,
                newTokenExpiration));
    }

    private static string CreateRefreshTokenKey() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string HashRefreshTokenKey(string refreshTokenKey) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshTokenKey)));
}
