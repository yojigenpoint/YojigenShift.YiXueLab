using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Repositories.Interfaces;

namespace YojigenShift.YiXueLab.DAL.UOW
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly YiXueDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private IDbContextTransaction? _currentTransaction;

        private IUserRepository? _users;
        private ICaseRepository? _cases;
        private IReadingRepository? _readings;
        private ITagRepository? _tags;

        public UnitOfWork(YiXueDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public IUserRepository Users => _users ??= _serviceProvider.GetRequiredService<IUserRepository>();
        public ICaseRepository Cases => _cases ??= _serviceProvider.GetRequiredService<ICaseRepository>();
        public IReadingRepository Readings => _readings ??= _serviceProvider.GetRequiredService<IReadingRepository>();
        public ITagRepository Tags => _tags ??= _serviceProvider.GetRequiredService<ITagRepository>();

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct);
        }

        public async Task BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction != null)
                return;

            _currentTransaction = await _context.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            try
            {
                await _context.SaveChangesAsync(ct);
                if (_currentTransaction != null)
                {
                    await _currentTransaction.CommitAsync(ct);
                }
            }
            catch
            {
                await RollbackTransactionAsync(ct);
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken ct = default)
        {
            try
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.RollbackAsync(ct);
                }
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public void Dispose()
        {
            _currentTransaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (_currentTransaction != null)
                await _currentTransaction.DisposeAsync();

            await _context.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
