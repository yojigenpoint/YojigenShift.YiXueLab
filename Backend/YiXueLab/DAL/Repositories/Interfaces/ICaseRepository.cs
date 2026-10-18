using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.Repositories.Base;

namespace YojigenShift.YiXueLab.DAL.Repositories.Interfaces
{
    public interface ICaseRepository : IRepository<DivinationCaseEntity>
    {
        Task<DivinationCaseEntity?> GetLatestBlindCaseAsync(DivinationType? type = null, CancellationToken ct = default);
        Task<DivinationCaseEntity?> GetCaseWithDetailsAsync(Guid caseId, CancellationToken ct = default);
        Task<(List<DivinationCaseEntity> Items, int TotalCount)> SearchCasesAsync(
            CaseStatus? status,
            DivinationType? type,
            string? keyword,
            List<Guid>? tagIds,
            int pageIndex,
            int pageSize,
            CancellationToken ct = default);
        Task<int> RevealDueCasesAsync(DateTime currentUtcTime, CancellationToken ct = default);
    }
}
