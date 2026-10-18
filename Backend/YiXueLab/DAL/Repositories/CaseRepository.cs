using Microsoft.EntityFrameworkCore;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.Repositories.Base;
using YojigenShift.YiXueLab.DAL.Repositories.Interfaces;

namespace YojigenShift.YiXueLab.DAL.Repositories
{
    public class CaseRepository : RepositoryBase<DivinationCaseEntity>, ICaseRepository
    {
        public CaseRepository(YiXueDbContext context) : base(context) { }

        public async Task<DivinationCaseEntity?> GetLatestBlindCaseAsync(DivinationType? type = null, CancellationToken ct = default)
        {
            var query = DbSet.AsNoTracking()
                             .Include(c => c.Publisher)
                             .Include(c => c.CaseTags)
                                 .ThenInclude(ct => ct.Tag)
                             .Where(c => c.Status == CaseStatus.Blind);

            if (type.HasValue)
            {
                query = query.Where(c => c.DivinationType == type.Value);
            }

            return await query.OrderByDescending(c => c.CreatedAt)
                              .FirstOrDefaultAsync(ct);
        }

        public async Task<DivinationCaseEntity?> GetCaseWithDetailsAsync(Guid caseId, CancellationToken ct = default)
        {
            return await DbSet.AsNoTracking()
                              .Include(c => c.Publisher)
                              .Include(c => c.CaseTags)
                                  .ThenInclude(ct => ct.Tag)
                              .Include(c => c.Readings.OrderByDescending(r => r.LikesCount).ThenBy(r => r.CreatedAt))
                                  .ThenInclude(r => r.User)
                              .FirstOrDefaultAsync(c => c.Id == caseId, ct);
        }

        public async Task<(List<DivinationCaseEntity> Items, int TotalCount)> SearchCasesAsync(
            CaseStatus? status,
            DivinationType? type,
            string? keyword,
            List<Guid>? tagIds,
            int pageIndex,
            int pageSize,
            CancellationToken ct = default)
        {
            var query = DbSet.AsNoTracking()
                             .Include(c => c.Publisher)
                             .Include(c => c.CaseTags)
                                 .ThenInclude(ct => ct.Tag)
                             .AsQueryable();

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (type.HasValue)
                query = query.Where(c => c.DivinationType == type.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string pattern = $"%{keyword.Trim()}%";
                query = query.Where(c =>
                    EF.Functions.ILike(c.BackgroundInfo, pattern) ||
                    EF.Functions.ILike(c.QuestionText, pattern) ||
                    (c.RealFeedback != null && EF.Functions.ILike(c.RealFeedback, pattern)));
            }

            if (tagIds != null && tagIds.Count > 0)
            {
                foreach (var tagId in tagIds)
                {
                    query = query.Where(c => c.CaseTags.Any(ct => ct.TagId == tagId));
                }
            }

            int totalCount = await query.CountAsync(ct);

            var items = await query.OrderByDescending(c => c.CreatedAt)
                                   .Skip((pageIndex - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<int> RevealDueCasesAsync(DateTime currentUtcTime, CancellationToken ct = default)
        {
            // 注意：ExecuteUpdateAsync 绕过 ChangeTracker，因此手动一并更新 ModifiedAt
            return await DbSet.Where(c => c.Status == CaseStatus.Blind && c.RevealTime <= currentUtcTime)
                              .ExecuteUpdateAsync(setters => setters
                                  .SetProperty(c => c.Status, CaseStatus.Revealed)
                                  .SetProperty(c => c.ModifiedAt, currentUtcTime),
                                  ct);
        }
    }
}
