using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.Repositories;
using YojigenShift.YiXueLab.DAL.Repositories.Interfaces;
using YojigenShift.YiXueLab.DAL.UOW;

namespace YojigenShift.YiXueLab.Tests.DAL
{
    /// <summary>
    /// DAL 单元测试抽象基类：
    /// 每个测试方法执行前都会创建一个独立的 SQLite 内存数据库与 DI 容器作用域，测试结束后自动销毁，保证 100% 隔离。
    /// </summary>
    public abstract class DalTestBase : IAsyncLifetime
    {
        private SqliteConnection _connection = null!;
        private ServiceProvider _serviceProvider = null!;
        protected IServiceScope Scope = null!;
        protected YiXueDbContext DbContext = null!;
        protected IUnitOfWork Uow = null!;

        public async ValueTask InitializeAsync()
        {
            // 1. 创建并打开 SQLite 内存连接（只要连接不断开，内存库就一直存在）
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();

            var services = new ServiceCollection();

            // 2. 注册 TestYiXueDbContext 指向内存 SQLite
            services.AddScoped<DbContextOptions<YiXueDbContext>>(_ =>
                new DbContextOptionsBuilder<YiXueDbContext>()
                    .UseSqlite(_connection)
                    .Options);

            services.AddScoped<YiXueDbContext, TestYiXueDbContext>();

            // 3. 注册所有仓储与 UnitOfWork (与生产环境完全一致)
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICaseRepository, CaseRepository>();
            services.AddScoped<IReadingRepository, ReadingRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            _serviceProvider = services.BuildServiceProvider();
            Scope = _serviceProvider.CreateScope();

            DbContext = Scope.ServiceProvider.GetRequiredService<YiXueDbContext>();
            Uow = Scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // 4. 在内存中建表及索引
            await DbContext.Database.EnsureCreatedAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Uow.DisposeAsync();
            Scope.Dispose();
            await _serviceProvider.DisposeAsync();
            await _connection.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 辅助方法：快速在库里创建一个测试用户
        /// </summary>
        protected async Task<UserEntity> SeedTestUserAsync(string username = "test_master", UserRole role = UserRole.Publisher)
        {
            var user = new UserEntity
            {
                Username = username,
                PasswordHash = "hashed_pwd_123",
                WechatNickname = $"群友_{username}",
                Role = role
            };
            await Uow.Users.AddAsync(user);
            await Uow.SaveChangesAsync();
            return user;
        }
    }
}
