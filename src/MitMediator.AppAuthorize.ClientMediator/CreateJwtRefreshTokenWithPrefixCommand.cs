using MitMediator.AutoApi.Abstractions;
using MitMediator.AutoApi.Abstractions.Attributes;

namespace MitMediator.AppAuthorize.ClientMediator;

[Pattern("{key}/by-refresh")]
[Method(MethodType.Post)]
public sealed class CreateJwtByRefreshTokenWithPrefixCommand
    : KeyRequest<string>, IRequest<JwtTokenModel>
{
    public string RefreshTokenKey { get; init; } = null!;

    public string UserId { get; init; } = null!;
}
