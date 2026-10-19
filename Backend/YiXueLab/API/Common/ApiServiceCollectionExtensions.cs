using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using YojigenShift.YiXueLab.API.Auth;

namespace YojigenShift.YiXueLab.API.Common
{
    public static class ApiServiceCollectionExtensions
    {
        /// <summary>
        /// 配置 API 层核心服务（控制器、JSON struct 字段支持、JWT 鉴权与跨域 CORS）
        /// 抽离为扩展方法，既让 Program.cs 极度干净，又方便 API 单元测试直接复用配置！
        /// </summary>
        public static IServiceCollection AddApiPresentationLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. 绑定并注册 JwtSettings 与 Token 服务
            var jwtSettings = new JwtSettings();
            configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
            services.AddSingleton(jwtSettings);
            services.AddSingleton<IJwtTokenService, JwtTokenService>();

            // 2. 注册控制器并全局开启 IncludeFields = true (完美序列化 YiFramework 的 readonly struct)
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.IncludeFields = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // 3. 配置 JWT Bearer 认证
            var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };
            });

            services.AddAuthorization();
            services.AddResponseCaching();

            // 4. 配置 CORS 跨域策略，允许 Vue 3 (Vite 默认端口 5173) 联调访问
            services.AddCors(options =>
            {
                options.AddPolicy("VueFrontendPolicy", policy =>
                {
                    var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                                      ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173" };

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }
    }
}
