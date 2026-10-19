using FluentAssertions;
using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.Tests.BLL
{
    public class AuthServiceTests : BllTestBase
    {
        [Fact(DisplayName = "注册与登录闭环：管理员生成邀请码 -> 新用户注册核销 -> PBKDF2 密码验证登录成功")]
        public async Task FullAuthFlow_GenerateInvite_Register_And_Login_Should_Succeed()
        {
            // Arrange: 1. 先植入一位管理员并生成邀请码
            var admin = await SeedUserAsync("group_owner", UserRole.Admin);
            var inviteResult = await AuthService.GenerateInviteCodeAsync(admin.Id);

            inviteResult.IsSuccess.Should().BeTrue();
            string inviteCode = inviteResult.Data!;
            inviteCode.Should().StartWith("YX-");

            // Act 1: 使用该邀请码注册新用户
            var regReq = new RegisterRequestDto(
                Username: "new_disciple",
                Password: "MyStrongPassword!2025",
                WechatNickname: "小师弟",
                InviteCode: inviteCode.ToLowerInvariant() // 故意传小写，测试自动转大写容错
            );
            var regResult = await AuthService.RegisterAsync(regReq);

            // Assert 1: 注册成功，且邀请码已被标记为已使用
            regResult.IsSuccess.Should().BeTrue();
            regResult.Data!.Username.Should().Be("new_disciple");

            var usedInviteResult = await AuthService.RegisterAsync(
                new RegisterRequestDto("another_user", "pwd123", "路人甲", inviteCode));
            usedInviteResult.IsSuccess.Should().BeFalse("同一个邀请码绝不能被二次使用");

            // Act 2: 使用正确密码与错误密码测试登录
            var validLogin = await AuthService.ValidateCredentialsAsync(
                new LoginRequestDto("new_disciple", "MyStrongPassword!2025"));
            var wrongPwdLogin = await AuthService.ValidateCredentialsAsync(
                new LoginRequestDto("new_disciple", "WrongPassword"));

            // Assert 2
            validLogin.IsSuccess.Should().BeTrue();
            validLogin.Data!.WechatNickname.Should().Be("小师弟");

            wrongPwdLogin.IsSuccess.Should().BeFalse();
            wrongPwdLogin.ErrorCode.Should().Be(ServiceErrorCode.Unauthorized);
        }

        [Fact(DisplayName = "权限与冻结拦截：非管理员不可生成邀请码，被封禁账号不可登录")]
        public async Task NonAdmin_Cannot_GenerateInvite_And_FrozenUser_Cannot_Login()
        {
            // Arrange
            var normalUser = await SeedUserAsync("normal_guy", UserRole.User);

            // Act & Assert 1: 普通群友尝试生成邀请码
            var inviteRes = await AuthService.GenerateInviteCodeAsync(normalUser.Id);
            inviteRes.IsSuccess.Should().BeFalse();
            inviteRes.ErrorCode.Should().Be(ServiceErrorCode.Forbidden);

            // Arrange 2: 注册一个账号随后将其冻结 (IsActive = false)
            var admin = await SeedUserAsync("admin_1", UserRole.Admin);
            var code = (await AuthService.GenerateInviteCodeAsync(admin.Id)).Data!;
            var reg = await AuthService.RegisterAsync(new RegisterRequestDto("bad_user", "123456", "违规者", code));

            var userEntity = await Uow.Users.GetByIdAsync(reg.Data!.Id);
            userEntity!.IsActive = false;
            Uow.Users.Update(userEntity);
            await Uow.SaveChangesAsync();

            // Act & Assert 2: 冻结账号尝试登录
            var loginRes = await AuthService.ValidateCredentialsAsync(new LoginRequestDto("bad_user", "123456"));
            loginRes.IsSuccess.Should().BeFalse();
            loginRes.ErrorCode.Should().Be(ServiceErrorCode.Forbidden);
        }
    }
}
