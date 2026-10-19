using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using System.Text.Json.Serialization;
using YojigenShift.YiXueLab.DAL.Context;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.JsonModels;

namespace YojigenShift.YiXueLab.Tests.BLL
{
    public class BllTestDbContext : YiXueDbContext
    {
        public BllTestDbContext(DbContextOptions<YiXueDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            if (!Database.IsNpgsql())
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    IncludeFields = true,
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
