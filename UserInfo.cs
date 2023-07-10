using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace BNHPortalServices
{
    public class UserInfo
    {
        public Guid UserId { get; private set; }
        public string Email { get; private set; }

        public UserInfo(JwtSecurityToken userToken)
        {
            UserId = Guid.Parse(userToken.Subject);
            Email = userToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        }
    }
}