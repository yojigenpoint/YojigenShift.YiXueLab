using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.BLL.Models
{
    public record CaseSearchFilterDto(
        CaseStatus? Status = null,
        DivinationType? DivinationType = null,
        string? Keyword = null,
        List<Guid>? TagIds = null,
        int PageIndex = 1,
        int PageSize = 10
    );
}
