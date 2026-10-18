using YojigenShift.YiXueLab.DAL.Repositories.Interfaces;

namespace YojigenShift.YiXueLab.DAL.UOW
{
    public interface IUnitOfWork : IAsyncDisposable, IDisposable
    {
        IUserRepository Users { get; }
        ICaseRepository Cases { get; }
        IReadingRepository Readings { get; }
        ITagRepository Tags { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
        Task BeginTransactionAsync(CancellationToken ct = default);
        Task CommitTransactionAsync(CancellationToken ct = default);
        Task RollbackTransactionAsync(CancellationToken ct = default);
    }
}
