namespace YojigenShift.YiXueLab.BLL.Models
{
    public record RegisterRequestDto(
        string Username,
        string Password,
        string WechatNickname,
        string InviteCode
    );
}
