namespace MitMediator.AppAuthorize.Web.Models.Requests;

internal class CreateJwtByPasswordCommand : IRequest<JwtTokenModel>
{
    public required string Login { get; init; }

    public required string Password { get; init; }
}