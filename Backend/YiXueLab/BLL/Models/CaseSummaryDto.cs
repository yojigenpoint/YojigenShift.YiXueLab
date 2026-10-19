using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.BLL.Models
{
    public record CaseSummaryDto(
        Guid Id,
        DivinationType DivinationType,
        string PublisherNickname,
        string BackgroundInfo,
        string QuestionText,
        CaseStatus Status,
        DateTime RevealTime,
        DateTime CreatedAt,
        List<string> Tags
    );
}
