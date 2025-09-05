namespace MitMediator.AppAuthorize.Web.Models.Requests;

internal class CreateJwtByRefreshTokenCommand
{
    public string RefreshTokenKey { get; init; } = null!;

    public string UserId { get; init; } = null!;
}