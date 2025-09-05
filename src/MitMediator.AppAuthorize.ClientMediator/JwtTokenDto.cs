using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MitMediator.AppAuthorize.ClientMediator;


public class JwtTokenModel
{
    public string JwtToken { get; set; } = null!;

    public string RefreshToken { get; set; } = null!;

    public DateTime JwtTokenExpires => new JwtSecurityToken(JwtToken).ValidTo;
    
    public DateTime RefreshTokenExpires  { get; set; }
    
    public string UserId => new JwtSecurityToken(JwtToken).Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value;

}