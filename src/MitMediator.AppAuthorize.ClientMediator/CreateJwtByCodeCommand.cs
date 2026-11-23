using MitMediator.AutoApi.Abstractions;
using MitMediator.AutoApi.Abstractions.Attributes;

namespace MitMediator.AppAuthorize.ClientMediator;

[Pattern("auth/by-code")]
[Method(MethodType.Post)]
public sealed class CreateJwtByCodeCommand : IRequest<JwtTokenModel>
{
    public string Login { get; init; } = null!;

    public string Code { get; init; } = null!;
}