namespace YojigenShift.YiXueLab.BLL.Models
{
    public record SubmitReadingRequestDto(
        Guid CaseId,
        string Content
    );
}
