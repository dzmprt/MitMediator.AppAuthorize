namespace MitMediator.AppAuthorize.Web.Models.Requests;

internal class CreateJwtByPasswordCommand : IRequest<JwtTokenModel>
{
    public string Login { get; init; } = null!;

    public string Password { get; init; } = null!;
}