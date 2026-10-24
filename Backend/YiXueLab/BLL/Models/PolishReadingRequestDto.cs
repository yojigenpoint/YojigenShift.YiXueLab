using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.BLL.Models
{
    public record PolishReadingRequestDto(
        DivinationType DivinationType,
        string RawContent,
        string? QuestionContext = null
    );
}
