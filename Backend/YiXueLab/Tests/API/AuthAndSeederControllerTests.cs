using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using YojigenShift.YiXueLab.API.Auth;
using YojigenShift.YiXueLab.API.Common;
using YojigenShift.YiXueLab.API.Setup;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.Tests.API
{
    public class AuthAndSeederControllerTests : ApiTestBase
    {
        [Fact(DisplayName = "冷启动与幂等性测试：DbSeeder 应自动创建初始群主管理员，且重复执行不报错")]
        public async Task DbSeeder_Should_SeedInitialAdmin_Idempotently_And_Allow_AdminLogin()
        {
            // Act 1: 首次执行种子生成器，随后再重复执行一次测试幂等性
            await DbSeeder.EnsureInitialAdminSeededAsync(RootServiceProvider, Configuration);
            await DbSeeder.EnsureInitialAdminSeededAsync(RootServiceProvider, Configuration);

            // Assert 1: 数据库中应当只有 1 个 admin 账号
            var admins = await Uow.Users.FindAsync(u => u.Username == "admin");
            admins.Should().HaveCount(1);
            admins[0].Role.Should().Be(UserRole.Admin);
            admins[0].WechatNickname.Should().Be("测试群主管理员");

            // Act 2: 使用种子管理员的默认账号密码调用 AuthController.Login
            var loginActionResult = await AuthController.Login(
                new LoginRequestDto("admin", "YiXueLab@2026"),
                default);

            // Assert 2: 应返回 200 OK 且包含合法的 JWT AccessToken
            var okResult = loginActionResult.Should().BeOfType<OkObjectResult>().Subject;
            var tokenResponse = okResult.Value.Should().BeOfType<AuthTokenResponseDto>().Subject;

            tokenResponse.AccessToken.Should().NotBeNullOrWhiteSpace();
            tokenResponse.User.Role.Should().Be(UserRole.Admin);
            tokenResponse.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow.AddDays(29));
        }

        [Fact(DisplayName = "鉴权控制器闭环：管理员生成邀请码 -> 新群友注册 -> 错误密码返回 401 -> 重复注册返回 409")]
        public async Task AuthController_FullFlow_And_StatusCodeMapping_Should_Work()
        {
            // Arrange: 先初始化管理员并切换当前登录身份为 Admin
            await DbSeeder.EnsureInitialAdminSeededAsync(RootServiceProvider, Configuration);
            var adminLogin = (OkObjectResult)await AuthController.Login(
                new LoginRequestDto("admin", "YiXueLab@2026"), default);
            var adminProfile = ((AuthTokenResponseDto)adminLogin.Value!).User;

            AuthenticateAs(adminProfile);

            // Act 1: 管理员调用 POST /api/auth/invite-codes 生成邀请码
            var inviteActionResult = await AuthController.GenerateInviteCode(default);
            var inviteOk = inviteActionResult.Should().BeOfType<OkObjectResult>().Subject;

            // 通过反射或序列化提取匿名对象中的 inviteCode 属性
            string json = JsonSerializer.Serialize(inviteOk.Value);
            using var doc = JsonDocument.Parse(json);
            string inviteCode = doc.RootElement.GetProperty("inviteCode").GetString()!;
            inviteCode.Should().StartWith("YX-");

            // Act 2: 新群友凭邀请码调用 POST /api/auth/register 注册
            var regReq = new RegisterRequestDto("chen_yiyou", "Pass!123456", "陈易友", inviteCode);
            var regResult = await AuthController.Register(regReq, default);
            var regOk = regResult.Should().BeOfType<OkObjectResult>().Subject;
            var newUserToken = regOk.Value.Should().BeOfType<AuthTokenResponseDto>().Subject;

            newUserToken.User.Username.Should().Be("chen_yiyou");

            // Act & Assert 3: 再次用已被核销的邀请码注册，应映射为 400 Bad Request (ApiErrorResponse)
            var badRegResult = await AuthController.Register(
                new RegisterRequestDto("other", "Pass!123", "路人", inviteCode), default);
            var badRegObj = badRegResult.Should().BeOfType<ObjectResult>().Subject;
            badRegObj.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
            badRegObj.Value.Should().BeOfType<ApiErrorResponse>();

            // Act & Assert 4: 用错误密码登录，应映射为 401 Unauthorized
            var wrongLoginResult = await AuthController.Login(
                new LoginRequestDto("chen_yiyou", "WrongPassword!"), default);
            var unauthorizedObj = wrongLoginResult.Should().BeOfType<ObjectResult>().Subject;
            unauthorizedObj.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

            // Act & Assert 5: 切换到新群友身份，验证 CurrentUserId 提取与 /api/auth/me 接口
            AuthenticateAs(newUserToken.User);
            var meResult = AuthController.GetCurrentUser().Should().BeOfType<OkObjectResult>().Subject;
            string meJson = JsonSerializer.Serialize(meResult.Value);
            using var meDoc = JsonDocument.Parse(meJson);
            meDoc.RootElement.GetProperty("id").GetGuid().Should().Be(newUserToken.User.Id);
            meDoc.RootElement.GetProperty("nickname").GetString().Should().Be("陈易友");
        }
    }
}
