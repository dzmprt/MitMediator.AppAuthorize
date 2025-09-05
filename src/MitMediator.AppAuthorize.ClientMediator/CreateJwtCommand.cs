using MitMediator.AutoApi.Abstractions;
using MitMediator.AutoApi.Abstractions.Attributes;

namespace MitMediator.AppAuthorize.ClientMediator;

[Pattern("auth")]
[Method(MethodType.Post)]
internal class CreateJwtCommand : IRequest<JwtTokenModel>
{
    public string Login { get; init; } = null!;

    public string Password { get; init; } = null!;
}