using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Entities;

namespace YojigenShift.YiXueLab.DAL.Repositories.Base
{
    public class RepositoryBase<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
    {
        protected readonly YiXueDbContext Context;
        protected readonly DbSet<TEntity> DbSet;

        public RepositoryBase(YiXueDbContext context)
        {
            Context = context;
            DbSet = context.Set<TEntity>();
        }

        public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            // 使用 FirstOrDefaultAsync 以确保触发 HasQueryFilter (!IsDeleted)
            return await DbSet.FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public virtual async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool asNoTracking = true,
            CancellationToken ct = default)
        {
            var query = asNoTracking ? DbSet.AsNoTracking() : DbSet;
            return await query.FirstOrDefaultAsync(predicate, ct);
        }

        public virtual async Task<List<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool asNoTracking = true,
            CancellationToken ct = default)
        {
            var query = asNoTracking ? DbSet.AsNoTracking() : DbSet;
            return await query.Where(predicate).ToListAsync(ct);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
        {
            return await DbSet.AnyAsync(predicate, ct);
        }

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
        {
            return predicate == null
                ? await DbSet.CountAsync(ct)
                : await DbSet.CountAsync(predicate, ct);
        }

        public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default)
        {
            await DbSet.AddAsync(entity, ct);
        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
        {
            await DbSet.AddRangeAsync(entities, ct);
        }

        public virtual void Update(TEntity entity)
        {
            DbSet.Update(entity);
        }

        public virtual void Remove(TEntity entity)
        {
            // 调用 DbSet.Remove 会将状态设为 Deleted，随后被 YiXueDbContext.ApplyAuditAndSoftDeleteRules 拦截转为软删除
            DbSet.Remove(entity);
        }

        public virtual async Task<int> SoftDeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
        {
            var utcNow = DateTime.UtcNow;
            return await DbSet.Where(predicate)
                              .ExecuteUpdateAsync(setters => setters
                                  .SetProperty(e => e.IsDeleted, true)
                                  .SetProperty(e => e.ModifiedAt, utcNow), ct);
        }
    }
}
