using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.BLL.Services.Interfaces
{
    public interface ICaseService
    {
        Task<ServiceResult<Guid>> PublishCaseAsync(Guid publisherId, PublishCaseRequestDto req, CancellationToken ct = default);
        Task<ServiceResult<CaseDetailDto>> GetCaseDetailAsync(Guid caseId, Guid currentUserId, CancellationToken ct = default);
        Task<ServiceResult<CaseDetailDto>> GetLatestBlindCaseAsync(Guid currentUserId, DivinationType? type = null, CancellationToken ct = default);
        Task<ServiceResult<PagedResultDto<CaseSummaryDto>>> SearchCasesAsync(CaseSearchFilterDto filter, CancellationToken ct = default);
        Task<ServiceResult<ReadingDto>> SubmitReadingAsync(Guid userId, SubmitReadingRequestDto req, CancellationToken ct = default);
        Task<ServiceResult> LikeReadingAsync(Guid readingId, CancellationToken ct = default);
    }
}
