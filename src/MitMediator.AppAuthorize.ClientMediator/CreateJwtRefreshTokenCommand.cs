using MitMediator.AutoApi.Abstractions;
using MitMediator.AutoApi.Abstractions.Attributes;

namespace MitMediator.AppAuthorize.ClientMediator;

[Pattern("auth/by-refresh")]
[Method(MethodType.Post)]
public class CreateJwtByRefreshTokenCommand : IRequest<JwtTokenModel>
{
    public string RefreshTokenKey { get; init; } = null!;

    public string UserId { get; init; } = null!;
}