using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using YojigenShift.YiXueLab.API.Auth;
using YojigenShift.YiXueLab.API.Common;
using YojigenShift.YiXueLab.API.Controllers;
using YojigenShift.YiXueLab.BLL;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Repositories;
using YojigenShift.YiXueLab.DAL.Repositories.Interfaces;
using YojigenShift.YiXueLab.DAL.UOW;

namespace YojigenShift.YiXueLab.Tests.API
{
    /// <summary>
    /// API 表现层测试基类：
    /// 无需启动真实的 PostgreSQL 或繁重的 Kestrel 服务器，直接通过 DI 容器组装全套
    /// Controller + JwtTokenService + BLL + SQLite 内存库，并支持一键模拟当前登录用户的 HttpContext！
    /// </summary>
    public abstract class ApiTestBase : IAsyncLifetime
    {
        private SqliteConnection _connection = null!;
        protected ServiceProvider RootServiceProvider = null!;
        protected IServiceScope Scope = null!;
        protected IConfiguration Configuration = null!;

        protected YiXueDbContext DbContext = null!;
        protected IUnitOfWork Uow = null!;
        protected IJwtTokenService JwtTokenService = null!;

        // 被测控制器实例
        protected MetaController MetaController = null!;
        protected AuthController AuthController = null!;
        protected CasesController CasesController = null!;
        protected AiController AiController = null!;

        public async ValueTask InitializeAsync()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();

            // 1. 构造内存配置 (模拟 appsettings.json)
            var configDict = new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "YiXueLab_UnitTest_SuperSecretKey_AtLeast32Bytes_2026!",
                ["JwtSettings:Issuer"] = "YiXueLab.Test.API",
                ["JwtSettings:Audience"] = "YiXueLab.Test.Client",
                ["JwtSettings:ExpiryDays"] = "30",
                ["InitialAdmin:Username"] = "admin",
                ["InitialAdmin:Password"] = "YiXueLab@2026",
                ["InitialAdmin:Nickname"] = "测试群主管理员"
            };

            Configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            var services = new ServiceCollection();

            // 2. 注册内存 SQLite DAL
            services.AddScoped<DbContextOptions<YiXueDbContext>>(_ =>
                new DbContextOptionsBuilder<YiXueDbContext>()
                    .UseSqlite(_connection)
                    .Options);
            services.AddScoped<YiXueDbContext, ApiTestDbContext>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICaseRepository, CaseRepository>();
            services.AddScoped<IReadingRepository, ReadingRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 3. 注册 BLL 与 API 表现层服务
            services.AddBusinessLogicLayer();
            services.AddApiPresentationLayer(Configuration);

            // 4. 将控制器本身也注册进容器以便解析
            services.AddScoped<MetaController>();
            services.AddScoped<AuthController>();
            services.AddScoped<CasesController>();
            services.AddScoped<AiController>();

            RootServiceProvider = services.BuildServiceProvider();
            Scope = RootServiceProvider.CreateScope();

            DbContext = Scope.ServiceProvider.GetRequiredService<YiXueDbContext>();
            Uow = Scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            JwtTokenService = Scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

            MetaController = Scope.ServiceProvider.GetRequiredService<MetaController>();
            AuthController = Scope.ServiceProvider.GetRequiredService<AuthController>();
            CasesController = Scope.ServiceProvider.GetRequiredService<CasesController>();
            AiController = Scope.ServiceProvider.GetRequiredService<AiController>();

            // 默认绑定匿名的 HttpContext
            AttachAnonymousContext(MetaController, AuthController, CasesController, AiController);

            await DbContext.Database.EnsureCreatedAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Uow.DisposeAsync();
            Scope.Dispose();
            await RootServiceProvider.DisposeAsync();
            await _connection.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 为控制器绑定未登录（匿名）的 HttpContext
        /// </summary>
        protected static void AttachAnonymousContext(params ControllerBase[] controllers)
        {
            foreach (var controller in controllers)
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };
            }
        }

        /// <summary>
        /// 核心测试辅助方法：模拟指定用户携带合法 JWT Token 访问控制器
        /// （真实调用 JwtTokenService 生成 Token，并解析出 ClaimsPrincipal 挂载到 Controller.User）
        /// </summary>
        protected void AuthenticateAs(UserProfileDto user)
        {
            var tokenDto = JwtTokenService.GenerateToken(user);
            var handler = new JwtSecurityTokenHandler();
            var jwtSettings = Scope.ServiceProvider.GetRequiredService<JwtSettings>();

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true
            };

            // 真实校验并还原 ClaimsPrincipal
            ClaimsPrincipal principal = handler.ValidateToken(tokenDto.AccessToken, validationParams, out _);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            foreach (var controller in new ControllerBase[] { MetaController, AuthController, CasesController, AiController })
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                };
            }
        }
    }
}
