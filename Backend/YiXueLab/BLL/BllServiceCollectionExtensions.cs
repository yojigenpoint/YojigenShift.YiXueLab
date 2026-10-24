using Microsoft.Extensions.DependencyInjection;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.BLL.Services;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;

namespace YojigenShift.YiXueLab.BLL
{
    public static class BllServiceCollectionExtensions
    {
        public static IServiceCollection AddBusinessLogicLayer(
            this IServiceCollection services,
            AiSettings? aiSettings = null)
        {
            // 排盘服务无状态，可注册为 Singleton 提升性能
            services.AddSingleton<IDivinationPlotterService, DivinationPlotterService>();

            // 注册 AI 配置与 HttpClient（若上层未传入 AiSettings，则注册默认空配置触发智能兜底模式）
            services.AddSingleton(aiSettings ?? new AiSettings());
            services.AddSingleton<HttpClient>();
            services.AddScoped<IAiAssistantService, AiAssistantService>();

            // 业务服务依赖 Scoped 的 IUnitOfWork，注册为 Scoped
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICaseService, CaseService>();

            return services;
        }
    }
}
