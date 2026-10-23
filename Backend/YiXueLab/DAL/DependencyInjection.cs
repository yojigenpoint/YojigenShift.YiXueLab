using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.Repositories;
using YojigenShift.YiXueLab.DAL.Repositories.Interfaces;
using YojigenShift.YiXueLab.DAL.UOW;

namespace YojigenShift.YiXueLab.DAL
{
    public static class DalServiceCollectionExtensions
    {
        public static IServiceCollection AddDataAccessLayer(
            this IServiceCollection services,
            string connectionString)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

            dataSourceBuilder.MapEnum<UserRole>("user_role_enum");
            dataSourceBuilder.MapEnum<DivinationType>("divination_type_enum");
            dataSourceBuilder.MapEnum<CaseStatus>("case_status_enum");

            dataSourceBuilder.EnableDynamicJson();

            dataSourceBuilder.ConfigureJsonOptions(new JsonSerializerOptions
            {
                IncludeFields = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                AllowOutOfOrderMetadataProperties = true
            });

            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<YiXueDbContext>(options =>
            {
                options.UseNpgsql(dataSource, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(YiXueDbContext).Assembly.FullName);
                });
            });

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICaseRepository, CaseRepository>();
            services.AddScoped<IReadingRepository, ReadingRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
