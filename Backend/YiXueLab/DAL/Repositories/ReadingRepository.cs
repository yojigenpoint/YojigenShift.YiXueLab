using Microsoft.EntityFrameworkCore;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Repositories.Base;
using YojigenShift.YiXueLab.DAL.Repositories.Interfaces;

namespace YojigenShift.YiXueLab.DAL.Repositories
{
    public class ReadingRepository : RepositoryBase<ReadingEntity>, IReadingRepository
    {
        public ReadingRepository(YiXueDbContext context) : base(context) { }

        public async Task<bool> HasUserSubmittedAsync(Guid caseId, Guid userId, CancellationToken ct = default)
        {
            return await DbSet.AnyAsync(r => r.CaseId == caseId && r.UserId == userId, ct);
        }

        public async Task<bool> IncrementLikesAsync(Guid readingId, CancellationToken ct = default)
        {
            int affectedRows = await DbSet.Where(r => r.Id == readingId)
                                          .ExecuteUpdateAsync(s => s
                                              .SetProperty(r => r.LikesCount, r => r.LikesCount + 1)
                                              .SetProperty(r => r.ModifiedAt, DateTime.UtcNow),
                                              ct);
            return affectedRows > 0;
        }
    }
}
