using YojigenShift.YiXueLab.BLL.Models;

namespace YojigenShift.YiXueLab.API.Auth
{
    /// <summary>
    /// 登录/注册成功后的统一返回载体（含 JWT Token 与用户信息）
    /// </summary>
    public record AuthTokenResponseDto(
        string AccessToken,
        DateTime ExpiresAtUtc,
        UserProfileDto User
    );
}
