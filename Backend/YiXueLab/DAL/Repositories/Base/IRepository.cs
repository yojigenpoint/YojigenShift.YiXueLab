using System.Linq.Expressions;
using YojigenShift.YiXueLab.DAL.Entities;

namespace YojigenShift.YiXueLab.DAL.Repositories.Base
{
    public interface IRepository<TEntity> where TEntity : BaseEntity
    {
        Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = true, CancellationToken ct = default);
        Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = true, CancellationToken ct = default);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

        Task AddAsync(TEntity entity, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
        void Update(TEntity entity);

        /// <summary>
        /// 软删除实体（在 SaveChanges 时会自动将 IsDeleted 设为 true 并更新 ModifiedAt）
        /// </summary>
        void Remove(TEntity entity);

        /// <summary>
        /// 批量软删除（直接生成一条高效 SQL UPDATE，不加载到内存）
        /// </summary>
        Task<int> SoftDeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    }
}
