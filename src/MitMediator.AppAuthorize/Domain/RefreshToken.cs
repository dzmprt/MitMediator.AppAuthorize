namespace MitMediator.AppAuthorize.Domain;

public class RefreshToken
{
    public string RefreshTokenKey { get; set; }
    public string UserId { get; set; }
    public DateTime Expired { get; set; }
}