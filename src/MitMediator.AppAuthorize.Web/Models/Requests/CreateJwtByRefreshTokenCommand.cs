namespace MitMediator.AppAuthorize.Web.Models.Requests;


internal class CreateJwtByRefreshTokenCommand : IRequest<JwtTokenModel>
{
    public required string RefreshTokenKey { get; init; }

    public required string UserId { get; init; }
}