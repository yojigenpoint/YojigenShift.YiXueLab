using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YojigenShift.YiFramework.Enums;
using YojigenShift.YiXueLab.API.Auth;
using YojigenShift.YiXueLab.API.Setup;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.Tests.API
{
    public class CasesControllerTests : ApiTestBase
    {
        [Fact(DisplayName = "演武场 API 全链路：发布案例(201) -> 获取盲断题(200且锁定) -> 交卷解锁(200) -> 重复交卷(409) -> 点赞(204)")]
        public async Task CasesController_EndToEnd_BlindReading_Lifecycle_Should_Succeed()
        {
            // Arrange: 创建一位管理员与一位普通群友
            await DbSeeder.EnsureInitialAdminSeededAsync(RootServiceProvider, Configuration);
            var adminEntity = (await Uow.Users.GetByUsernameAsync("admin"))!;
            var adminProfile = new UserProfileDto(
                adminEntity.Id, adminEntity.Username, adminEntity.WechatNickname, adminEntity.Role, adminEntity.CreatedAt);

            var memberEntity = new UserEntity
            {
                Username = "disciple_wang",
                PasswordHash = "hash",
                WechatNickname = "王师兄",
                Role = UserRole.User,
                IsActive = true
            };
            await Uow.Users.AddAsync(memberEntity);
            await Uow.SaveChangesAsync();
            var memberProfile = new UserProfileDto(
                memberEntity.Id, memberEntity.Username, memberEntity.WechatNickname, memberEntity.Role, memberEntity.CreatedAt);

            // -----------------------------------------------------------------
            // Act 1: 管理员调用 POST /api/cases 发布一道八字盲断题
            // -----------------------------------------------------------------
            AuthenticateAs(adminProfile);

            var publishReq = new PublishCaseRequestDto(
                DivinationType: DivinationType.Bazi,
                CastTimeUtc: new DateTime(1990, 5, 12, 14, 30, 0, DateTimeKind.Utc),
                BackgroundInfo: "男命，问2018戊戌年发生何事？",
                QuestionText: "请断2018年吉凶事项",
                RealFeedback: "【真实底牌】：辰戌冲导致工作变动及胃病住院。",
                RevealTimeUtc: DateTime.UtcNow.AddDays(1),
                Tags: new List<string> { "#辰戌冲", "#工作变动" },
                BaziGender: Gender.Male
            );

            var publishActionResult = await CasesController.PublishCase(publishReq);

            // Assert 1: 应返回 201 CreatedAtActionResult 并指向 GetCaseDetail
            var createdResult = publishActionResult.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(CasesController.GetCaseDetail));
            Guid caseId = (Guid)createdResult.RouteValues!["caseId"]!;
            caseId.Should().NotBeEmpty();

            // -----------------------------------------------------------------
            // Act 2: 切换为普通群友“王师兄”，调用 GET /api/cases/latest-blind
            // -----------------------------------------------------------------
            AuthenticateAs(memberProfile);

            var latestBlindResult = await CasesController.GetLatestBlindCase(DivinationType.Bazi);
            var latestOk = latestBlindResult.Should().BeOfType<OkObjectResult>().Subject;
            var detailBefore = latestOk.Value.Should().BeOfType<CaseDetailDto>().Subject;

            // Assert 2: 交卷前盲断锁必须开启
            detailBefore.Id.Should().Be(caseId);
            detailBefore.IsReadingsLocked.Should().BeTrue();
            detailBefore.RealFeedback.Should().BeNull();
            detailBefore.Readings.Should().BeEmpty();

            // -----------------------------------------------------------------
            // Act 3: 王师兄调用 POST /api/cases/{caseId}/readings 交卷
            // -----------------------------------------------------------------
            var submitResult = await CasesController.SubmitReading(
                caseId,
                new SubmitReadingBodyDto("原局辰土逢戊戌流年天克地冲，必有工作或肠胃之灾。"));

            var submitOk = submitResult.Should().BeOfType<OkObjectResult>().Subject;
            var readingDto = submitOk.Value.Should().BeOfType<ReadingDto>().Subject;
            readingDto.UserNickname.Should().Be("王师兄");

            // 再次调用详情接口验证解锁状态
            var detailAfterOk = (OkObjectResult)await CasesController.GetCaseDetail(caseId);
            var detailAfter = (CaseDetailDto)detailAfterOk.Value!;
            detailAfter.HasCurrentUserSubmitted.Should().BeTrue();
            detailAfter.IsReadingsLocked.Should().BeFalse();
            detailAfter.Readings.Should().ContainSingle();
            detailAfter.RealFeedback.Should().BeNull("未到揭晓时间，底牌答案依然不可见");

            // -----------------------------------------------------------------
            // Act 4: 王师兄尝试重复交卷，应返回 409 Conflict
            // -----------------------------------------------------------------
            var duplicateResult = await CasesController.SubmitReading(
                caseId,
                new SubmitReadingBodyDto("我想再交一次卷"));
            var conflictObj = duplicateResult.Should().BeOfType<ObjectResult>().Subject;
            conflictObj.StatusCode.Should().Be(StatusCodes.Status409Conflict);

            // -----------------------------------------------------------------
            // Act 5: 为断语点赞应返回 204 NoContent；对不存在的断语点赞应返回 404 NotFound
            // -----------------------------------------------------------------
            var likeResult = await CasesController.LikeReading(readingDto.Id);
            likeResult.Should().BeOfType<NoContentResult>();

            var notFoundLike = await CasesController.LikeReading(Guid.NewGuid());
            var notFoundObj = notFoundLike.Should().BeOfType<ObjectResult>().Subject;
            notFoundObj.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }
    }
}
