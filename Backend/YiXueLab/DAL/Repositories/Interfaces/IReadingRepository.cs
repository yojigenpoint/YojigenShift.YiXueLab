using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Repositories.Base;

namespace YojigenShift.YiXueLab.DAL.Repositories.Interfaces
{
    public interface IReadingRepository : IRepository<ReadingEntity>
    {
        Task<bool> HasUserSubmittedAsync(Guid caseId, Guid userId, CancellationToken ct = default);
        Task<bool> IncrementLikesAsync(Guid readingId, CancellationToken ct = default);
    }
}
