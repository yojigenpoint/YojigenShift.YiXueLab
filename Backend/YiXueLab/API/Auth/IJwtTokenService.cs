using YojigenShift.YiXueLab.BLL.Models;

namespace YojigenShift.YiXueLab.API.Auth
{
    public interface IJwtTokenService
    {
        AuthTokenResponseDto GenerateToken(UserProfileDto user);
    }
}
