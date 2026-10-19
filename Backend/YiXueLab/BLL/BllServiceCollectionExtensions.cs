using Microsoft.Extensions.DependencyInjection;
using YojigenShift.YiXueLab.BLL.Services;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;

namespace YojigenShift.YiXueLab.BLL
{
    public static class BllServiceCollectionExtensions
    {
        public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection services)
        {
            // 排盘服务无状态，可注册为 Singleton 提升性能
            services.AddSingleton<IDivinationPlotterService, DivinationPlotterService>();

            // 业务服务依赖 Scoped 的 IUnitOfWork，注册为 Scoped
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICaseService, CaseService>();

            return services;
        }
    }
}
