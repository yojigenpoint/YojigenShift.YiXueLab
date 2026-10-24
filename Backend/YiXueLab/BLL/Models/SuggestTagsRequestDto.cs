using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.BLL.Models
{
    public record SuggestTagsRequestDto(
        DivinationType DivinationType,
        string BackgroundInfo,
        string QuestionText,
        string? RealFeedback = null
    );
}
