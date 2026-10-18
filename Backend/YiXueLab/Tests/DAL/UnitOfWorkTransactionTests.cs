using FluentAssertions;
using YojigenShift.YiXueLab.DAL.Entities;

namespace YojigenShift.YiXueLab.Tests.DAL
{
    public class UnitOfWorkTransactionTests : DalTestBase
    {
        [Fact(DisplayName = "事务回滚测试：显式事务中途回滚时，已调用 SaveChangesAsync 的数据也必须全部撤销")]
        public async Task RollbackTransactionAsync_Should_Revert_All_Intermediate_Saves()
        {
            // Arrange
            await Uow.BeginTransactionAsync();

            // Act: 在事务中插入一个用户并调用了 SaveChangesAsync，随后触发回滚
            var tempUser = new UserEntity
            {
                Username = "rollback_user",
                PasswordHash = "pwd",
                WechatNickname = "回滚测试员"
            };
            await Uow.Users.AddAsync(tempUser);
            await Uow.SaveChangesAsync(); // 此时在事务内已落盘

            // 模拟业务后半段出错，执行回滚
            await Uow.RollbackTransactionAsync();

            // Assert: 清空上下文后查询，该用户应当完全不存在
            DbContext.ChangeTracker.Clear();
            var found = await Uow.Users.GetByUsernameAsync("rollback_user");
            found.Should().BeNull("事务回滚后，之前 SaveChanges 写入的数据必须被数据库撤销");
        }
    }
}
