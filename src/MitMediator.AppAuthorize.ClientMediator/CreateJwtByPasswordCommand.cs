using MitMediator.AutoApi.Abstractions;
using MitMediator.AutoApi.Abstractions.Attributes;

namespace MitMediator.AppAuthorize.ClientMediator;

[Pattern("auth/by-password")]
[Method(MethodType.Post)]
internal class CreateJwtByPasswordCommand : IRequest<JwtTokenModel>
{
    public string Login { get; init; } = null!;

    public string Password { get; init; } = null!;
}