namespace YojigenShift.YiXueLab.BLL.Models
{
    public record ReadingDto(
        Guid Id,
        Guid UserId,
        string UserNickname,
        string Content,
        int LikesCount,
        DateTime CreatedAt,
        bool IsCurrentUser
    );
}
