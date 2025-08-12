namespace MitMediator.AppAuthorize.Web.Models.Dtos;

internal class JwtTokenModel
{
    public string JwtToken { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;
    
    public DateTime RefreshTokenExpires  { get; set; }
}