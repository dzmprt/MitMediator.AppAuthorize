using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace MitMediator.AppAuthorize.Web;

internal class CreateJwtTokenService(JwtTokenConfiguration jwtTokenConfiguration)
{
    public string CreateJwtToken(UserInfo user, DateTime jwtTokenDateExpires)
    {
        var claims = new List<Claim>();
        if (user.UserId is null)
        {
            throw new ArgumentNullException(nameof(user.UserId));
        }
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.UserId));

        if (user.Name is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, user.Name));
        }
         
        if (user.Roles is not null)
        {
            foreach (var userRole in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }
        }

        if (user.Tenants is not null)
        {
            foreach (var tenant in user.Tenants)
            {
                claims.Add(new Claim(AppAuthorizeClaimTypes.TenantId, tenant));
            }
        }
        
        var securityKey = jwtTokenConfiguration.TokenValidationParameters.IssuerSigningKey;
        var credentials = new SigningCredentials(securityKey,
            SecurityAlgorithms.HmacSha256Signature);
        var tokenDescriptor = new JwtSecurityToken(jwtTokenConfiguration.TokenValidationParameters.ValidIssuer, jwtTokenConfiguration.TokenValidationParameters.ValidAudience, claims,
            expires: jwtTokenDateExpires, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor)!;
    }
}