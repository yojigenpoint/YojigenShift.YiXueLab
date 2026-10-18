using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using YojigenShift.YiXueLab.DAL.Entities;

namespace YojigenShift.YiXueLab.Tests.DAL
{
    public class AuditAndSoftDeleteTests : DalTestBase
    {
        [Fact(DisplayName = "新增实体时：应自动填充 CreatedAt (UTC) 且 IsDeleted 为 false")]
        public async Task AddAsync_Should_Set_AuditFields_Automatically()
        {
            // Arrange
            var beforeAdd = DateTime.UtcNow.AddSeconds(-1);
            var tag = new TagEntity
            {
                Name = "伤官见官",
                Category = "八字格局",
                IsDeleted = true // 故意传 true，测试拦截器是否会强制纠正为 false
            };

            // Act
            await Uow.Tags.AddAsync(tag);
            await Uow.SaveChangesAsync();

            // Assert
            tag.Id.Should().NotBeEmpty();
            tag.IsDeleted.Should().BeFalse("新增实体的 IsDeleted 必须被强制设为 false");
            tag.CreatedAt.Should().BeAfter(beforeAdd);
            tag.ModifiedAt.Should().BeNull("新增实体时尚未发生修改，ModifiedAt 应为 null");
        }

        [Fact(DisplayName = "修改实体时：应自动更新 ModifiedAt，且保护 CreatedAt 不被篡改")]
        public async Task Update_Should_Set_ModifiedAt_And_Protect_CreatedAt()
        {
            // Arrange
            var user = await SeedTestUserAsync("zhangsan");
            var originalCreatedAt = user.CreatedAt;

            // Act: 尝试修改昵称，并恶意篡改 CreatedAt
            user.WechatNickname = "张三丰";
            user.CreatedAt = new DateTime(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Uow.Users.Update(user);
            await Uow.SaveChangesAsync();

            // Assert: 重新从数据库读取验证
            DbContext.ChangeTracker.Clear();
            var updatedUser = await Uow.Users.GetByIdAsync(user.Id);

            updatedUser.Should().NotBeNull();
            updatedUser!.WechatNickname.Should().Be("张三丰");
            updatedUser.ModifiedAt.Should().NotBeNull();
            updatedUser.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(10), "CreatedAt 字段应被保护不被修改");
        }

        [Fact(DisplayName = "调用 Remove 时：应自动拦截转化为软删除，且全局查询过滤器自动隐藏该记录")]
        public async Task Remove_Should_Intercept_As_SoftDelete_And_Hide_From_Normal_Queries()
        {
            // Arrange
            var user = await SeedTestUserAsync("to_be_deleted");

            // Act: 调用标准的 Remove 并保存
            Uow.Users.Remove(user);
            await Uow.SaveChangesAsync();

            // Assert 1: 常规 Repository 查询应该查不到（被 HasQueryFilter 过滤）
            var normalQueryResult = await Uow.Users.GetByIdAsync(user.Id);
            normalQueryResult.Should().BeNull("软删除的数据对常规查询应当不可见");

            // Assert 2: 穿透全局过滤器查询底层表，证明数据并未被物理删除，且打上了软删除标记
            var rawDbRecord = await DbContext.Users
                                             .IgnoreQueryFilters()
                                             .FirstOrDefaultAsync(u => u.Id == user.Id);

            rawDbRecord.Should().NotBeNull("数据库物理记录必须依然保留");
            rawDbRecord!.IsDeleted.Should().BeTrue();
            rawDbRecord.ModifiedAt.Should().NotBeNull("软删除时应记录删除时间戳到 ModifiedAt");
        }

        [Fact(DisplayName = "条件唯一索引测试：软删除旧记录后，允许插入相同唯一键的新记录")]
        public async Task PartialUniqueIndex_Should_Allow_Duplicate_Username_After_SoftDelete()
        {
            // Arrange: 创建用户 "master_li" 然后将其软删除
            var firstUser = await SeedTestUserAsync("master_li");
            Uow.Users.Remove(firstUser);
            await Uow.SaveChangesAsync();

            // Act: 再次使用相同的用户名 "master_li" 注册新用户
            Func<Task> act = async () => await SeedTestUserAsync("master_li");

            // Assert: 由于配置了 .HasFilter("is_deleted = false")，不应抛出唯一索引冲突异常
            await act.Should().NotThrowAsync();

            var activeUsers = await Uow.Users.FindAsync(u => u.Username == "master_li");
            activeUsers.Should().HaveCount(1);
            activeUsers[0].Id.Should().NotBe(firstUser.Id);
        }
    }
}
