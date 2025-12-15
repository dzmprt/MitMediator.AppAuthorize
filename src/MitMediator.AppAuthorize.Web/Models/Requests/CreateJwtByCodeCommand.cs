namespace MitMediator.AppAuthorize.Web.Models.Requests;


internal class CreateJwtByCodeCommand : IRequest<JwtTokenModel>
{
    public string Login { get; init; } = null!;

    public string Code { get; init; } = null!;
}