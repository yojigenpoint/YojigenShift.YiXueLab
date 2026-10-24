namespace YojigenShift.YiXueLab.BLL.Models
{
    public record SuggestTagsResponseDto(
        List<string> Tags,
        bool IsRuleFallback
    );
}
