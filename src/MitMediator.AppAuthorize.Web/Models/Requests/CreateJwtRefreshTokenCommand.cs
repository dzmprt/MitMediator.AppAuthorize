
namespace MitMediator.AppAuthorize.Web.Models.Requests;


public class CreateJwtByRefreshTokenCommand : IRequest<JwtTokenModel>
{
    public string RefreshTokenKey { get; init; } = null!;

    public string UserId { get; init; } = null!;
}