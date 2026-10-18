using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using System.Text.Json.Serialization;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.JsonModels;

namespace YojigenShift.YiXueLab.Tests.DAL
{
    /// <summary>
    /// 专为单元测试扩展的 DbContext：
    /// 在使用 SQLite 内存模式跑测试时，利用 ValueConverter 模拟 PostgreSQL 的 JSONB 多态序列化（含 IncludeFields = true）
    /// </summary>
    /// <summary>
    /// 专为单元测试扩展的 DbContext：
    /// 在使用 SQLite 内存模式跑测试时，利用 ValueConverter 模拟 PostgreSQL 的 JSONB 多态序列化（含 IncludeFields = true）
    /// </summary>
    public class TestYiXueDbContext : YiXueDbContext
    {
        public TestYiXueDbContext(DbContextOptions<YiXueDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 如果当前测试环境不是 PostgreSQL (例如使用 SQLite 内存库)，则配置 JSON 转换器以测试多态序列化
            if (!Database.IsNpgsql())
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    IncludeFields = true, // 关键：确保 YiFramework 的 readonly struct 字段被正确序列化
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var payloadConverter = new ValueConverter<DivinationPayloadBase, string>(
                    v => JsonSerializer.Serialize(v, jsonOptions),
                    v => JsonSerializer.Deserialize<DivinationPayloadBase>(v, jsonOptions)!
                );

                modelBuilder.Entity<DivinationCaseEntity>()
                            .Property(c => c.DivinationData)
                            .HasConversion(payloadConverter);
            }
        }
    }
}
