namespace MitMediator.AppAuthorize.Web.Models.Requests;


internal class CreateJwtByCodeCommand : IRequest<JwtTokenModel>
{
    public required string Login { get; init; }

    public required string Code { get; init; }
}