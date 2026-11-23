using MitMediator.AutoApi.Abstractions;
using MitMediator.AutoApi.Abstractions.Attributes;

namespace MitMediator.AppAuthorize.ClientMediator;

[Pattern("{key}/by-password")]
[Method(MethodType.Post)]
public sealed class CreateJwtByPasswordWithPrefixCommand
    : KeyRequest<string>, IRequest<JwtTokenModel>
{
    public string Login { get; init; } = null!;

    public string Password { get; init; } = null!;
}
