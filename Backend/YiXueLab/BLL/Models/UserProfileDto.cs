using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.BLL.Models
{
    public record UserProfileDto(
        Guid Id,
        string Username,
        string WechatNickname,
        UserRole Role,
        DateTime CreatedAt
    );
}
