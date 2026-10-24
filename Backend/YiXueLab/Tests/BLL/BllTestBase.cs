using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YojigenShift.YiXueLab.BLL;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.Repositories;
using YojigenShift.YiXueLab.DAL.Repositories.Interfaces;
using YojigenShift.YiXueLab.DAL.UOW;

namespace YojigenShift.YiXueLab.Tests.BLL
{
    /// <summary>
    /// BLL 单元测试基类：
    /// 组装完整的 BLL Services + UoW + SQLite 内存关系库，100% 模拟真实生产事务与排盘流转。
    /// </summary>
    public abstract class BllTestBase : IAsyncLifetime
    {
        private SqliteConnection _connection = null!;
        private ServiceProvider _serviceProvider = null!;
        protected IServiceScope Scope = null!;

        protected YiXueDbContext DbContext = null!;
        protected IUnitOfWork Uow = null!;
        protected IDivinationPlotterService PlotterService = null!;
        protected IAuthService AuthService = null!;
        protected ICaseService CaseService = null!;
        protected IAiAssistantService AiService = null!;

        public async ValueTask InitializeAsync()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();

            var services = new ServiceCollection();

            // 1. 注册内存 SQLite DbContext
            services.AddScoped<DbContextOptions<YiXueDbContext>>(_ =>
                new DbContextOptionsBuilder<YiXueDbContext>()
                    .UseSqlite(_connection)
                    .Options);

            services.AddScoped<YiXueDbContext, BllTestDbContext>();

            // 2. 注册 DAL 仓储与工作单元
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICaseRepository, CaseRepository>();
            services.AddScoped<IReadingRepository, ReadingRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 3. 一键注册 BLL 所有业务服务 (测试 BllServiceCollectionExtensions)
            services.AddBusinessLogicLayer();

            _serviceProvider = services.BuildServiceProvider();
            Scope = _serviceProvider.CreateScope();

            DbContext = Scope.ServiceProvider.GetRequiredService<YiXueDbContext>();
            Uow = Scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            PlotterService = Scope.ServiceProvider.GetRequiredService<IDivinationPlotterService>();
            AuthService = Scope.ServiceProvider.GetRequiredService<IAuthService>();
            CaseService = Scope.ServiceProvider.GetRequiredService<ICaseService>();
            AiService = Scope.ServiceProvider.GetRequiredService<IAiAssistantService>();

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
        /// 辅助方法：直接向数据库植入指定角色的测试用户
        /// </summary>
        protected async Task<UserEntity> SeedUserAsync(
            string username,
            UserRole role = UserRole.User,
            bool isActive = true)
        {
            var user = new UserEntity
            {
                Username = username,
                PasswordHash = "dummy_hash",
                WechatNickname = $"易友_{username}",
                Role = role,
                IsActive = isActive
            };
            await Uow.Users.AddAsync(user);
            await Uow.SaveChangesAsync();
            return user;
        }
    }
}
