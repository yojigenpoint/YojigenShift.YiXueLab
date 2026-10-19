using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using YojigenShift.YiXueLab.BLL.Models;

namespace YojigenShift.YiXueLab.API.Auth
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _settings;

        public JwtTokenService(JwtSettings settings)
        {
            _settings = settings;
        }

        public AuthTokenResponseDto GenerateToken(UserProfileDto user)
        {
            var expiresAt = DateTime.UtcNow.AddDays(_settings.ExpiryDays);
            var keyBytes = Encoding.UTF8.GetBytes(_settings.SecretKey);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("nickname", user.WechatNickname)
            };

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: credentials
            );

            string token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
            return new AuthTokenResponseDto(token, expiresAt, user);
        }
    }
}
