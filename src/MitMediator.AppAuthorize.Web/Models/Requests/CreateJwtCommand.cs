namespace MitMediator.AppAuthorize.Web.Models.Requests;

internal class CreateJwtCommand
{
    public string Login { get; init; } = null!;

    public string Password { get; init; } = null!;
}