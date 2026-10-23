using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using System.Text.Json.Serialization;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.JsonModels;

namespace YojigenShift.YiXueLab.DAL.Context
{
    public class YiXueDbContext : DbContext
    {
        private static readonly JsonSerializerOptions JsonbSerializerOptions = new()
        {
            IncludeFields = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowOutOfOrderMetadataProperties = true
        };

        public YiXueDbContext(DbContextOptions<YiXueDbContext> options) : base(options)
        {
        }

        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<InviteCodeEntity> InviteCodes => Set<InviteCodeEntity>();
        public DbSet<DivinationCaseEntity> Cases => Set<DivinationCaseEntity>();
        public DbSet<ReadingEntity> Readings => Set<ReadingEntity>();
        public DbSet<TagEntity> Tags => Set<TagEntity>();
        public DbSet<CaseTagEntity> CaseTags => Set<CaseTagEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("uuid-ossp");
            modelBuilder.HasPostgresEnum<UserRole>("user_role_enum");
            modelBuilder.HasPostgresEnum<DivinationType>("divination_type_enum");
            modelBuilder.HasPostgresEnum<CaseStatus>("case_status_enum");

            // --- 1. 配置 Users 表 ---
            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.HasQueryFilter(u => !u.IsDeleted);

                // PostgreSQL 条件唯一索引：仅对未软删除的记录约束唯一性
                entity.HasIndex(u => u.Username)
                      .IsUnique()
                      .HasFilter("is_deleted = false");

                entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(u => u.IsActive).HasDefaultValue(true);
                entity.Property(u => u.IsDeleted).HasDefaultValue(false);
            });

            // --- 2. 配置 InviteCodes 表 ---
            modelBuilder.Entity<InviteCodeEntity>(entity =>
            {
                entity.HasQueryFilter(ic => !ic.IsDeleted);

                entity.HasIndex(ic => ic.Code)
                      .IsUnique()
                      .HasFilter("is_deleted = false");

                entity.Property(ic => ic.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(ic => ic.IsDeleted).HasDefaultValue(false);

                entity.HasOne(ic => ic.Generator)
                      .WithMany()
                      .HasForeignKey(ic => ic.GeneratedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ic => ic.UsedByUser)
                      .WithMany()
                      .HasForeignKey(ic => ic.UsedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // --- 3. 配置 Cases 表 ---
            var payloadConverter = new ValueConverter<DivinationPayloadBase, string>(
                v => SerializeDivinationPayload(v),
                v => DeserializeDivinationPayload(v)
            );

            modelBuilder.Entity<DivinationCaseEntity>(entity =>
            {
                entity.HasQueryFilter(c => !c.IsDeleted);

                entity.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(c => c.IsDeleted).HasDefaultValue(false);
                entity.Property(c => c.DivinationData)
                      .HasColumnType("jsonb")
                      .HasConversion(payloadConverter);

                entity.HasIndex(c => new { c.Status, c.DivinationType, c.CreatedAt })
                      .HasFilter("is_deleted = false");

                entity.HasOne(c => c.Publisher)
                      .WithMany(u => u.PublishedCases)
                      .HasForeignKey(c => c.PublisherId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // --- 4. 配置 Readings 表 ---
            modelBuilder.Entity<ReadingEntity>(entity =>
            {
                entity.HasQueryFilter(r => !r.IsDeleted);

                entity.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(r => r.IsDeleted).HasDefaultValue(false);

                // 同一案例下，每个用户只能有一条未删除的主断语（软删除后允许重发）
                entity.HasIndex(r => new { r.CaseId, r.UserId })
                      .IsUnique()
                      .HasFilter("is_deleted = false");

                entity.HasOne(r => r.Case)
                      .WithMany(c => c.Readings)
                      .HasForeignKey(r => r.CaseId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                      .WithMany(u => u.Readings)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // --- 5. 配置 Tags 与 CaseTags 表 ---
            modelBuilder.Entity<TagEntity>(entity =>
            {
                entity.HasQueryFilter(t => !t.IsDeleted);

                entity.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(t => t.IsDeleted).HasDefaultValue(false);

                entity.HasIndex(t => t.Name)
                      .IsUnique()
                      .HasFilter("is_deleted = false");
            });

            modelBuilder.Entity<CaseTagEntity>(entity =>
            {
                entity.HasKey(ct => new { ct.CaseId, ct.TagId });
                entity.Property(ct => ct.CreatedAt).HasDefaultValueSql("NOW()");

                // 配合主表全局过滤器，避免关联查询警告
                entity.HasQueryFilter(ct => !ct.Case.IsDeleted && !ct.Tag.IsDeleted);

                entity.HasOne(ct => ct.Case)
                      .WithMany(c => c.CaseTags)
                      .HasForeignKey(ct => ct.CaseId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ct => ct.Tag)
                      .WithMany(t => t.CaseTags)
                      .HasForeignKey(ct => ct.TagId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// 强制以抽象基类 DivinationPayloadBase 进行序列化，确保 100% 写入 "$type" 多态鉴别器
        /// </summary>
        private static string SerializeDivinationPayload(DivinationPayloadBase payload)
        {
            return JsonSerializer.Serialize<DivinationPayloadBase>(payload, JsonbSerializerOptions);
        }

        /// <summary>
        /// 多态反序列化（兼容 PostgreSQL jsonb 字段乱序，并自动兜底修复缺少 "$type" 的历史记录）
        /// </summary>
        private static DivinationPayloadBase DeserializeDivinationPayload(string json)
        {
            if (json.Contains("\"$type\"", StringComparison.Ordinal))
            {
                return JsonSerializer.Deserialize<DivinationPayloadBase>(json, JsonbSerializerOptions)!;
            }

            // 兜底兼容：若数据库中已存在未带 "$type" 的旧记录，按特征字段自动识别子类
            if (json.Contains("\"daYun\"", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<BaziCaseData>(json, JsonbSerializerOptions)!;
            if (json.Contains("\"original\"", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<LiuYaoCaseData>(json, JsonbSerializerOptions)!;
            if (json.Contains("\"chart\"", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<QiMenCaseData>(json, JsonbSerializerOptions)!;
            if (json.Contains("\"castMethod\"", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Deserialize<MeiHuaCaseData>(json, JsonbSerializerOptions)!;

            return JsonSerializer.Deserialize<DivinationPayloadBase>(json, JsonbSerializerOptions)!;
        }

        /// <summary>
        /// 重写 SaveChangesAsync：全自动处理 CreatedAt、ModifiedAt 以及将 Remove 转化为软删除
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditAndSoftDeleteRules();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            ApplyAuditAndSoftDeleteRules();
            return base.SaveChanges();
        }

        private void ApplyAuditAndSoftDeleteRules()
        {
            var utcNow = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = utcNow;
                        entry.Entity.IsDeleted = false;
                        break;

                    case EntityState.Modified:
                        entry.Entity.ModifiedAt = utcNow;
                        // 防止 CreatedAt 被意外篡改
                        entry.Property(x => x.CreatedAt).IsModified = false;
                        break;

                    case EntityState.Deleted:
                        // 拦截物理删除，自动转换为软删除！
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.ModifiedAt = utcNow;
                        entry.Property(x => x.CreatedAt).IsModified = false;
                        break;
                }
            }
        }
    }
}
