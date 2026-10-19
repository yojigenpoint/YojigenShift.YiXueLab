using FluentAssertions;
using YojigenShift.YiFramework.Enums;
using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.Tests.BLL
{
    public class CaseServiceBlindLockTests : BllTestBase
    {
        [Fact(DisplayName = "核心盲断锁测试：未交卷者隐藏他人断语；交卷后解锁他人断语，但揭晓前始终隐藏底牌答案")]
        public async Task BlindLock_Should_Enforce_TwoLayer_AntiCheating_Rules()
        {
            // Arrange: 创建发布者与两名普通群友 UserA、UserB
            var publisher = await SeedUserAsync("volunteer_pub", UserRole.Publisher);
            var userA = await SeedUserAsync("user_a", UserRole.User);
            var userB = await SeedUserAsync("user_b", UserRole.User);

            // 发布一个明天才揭晓的八字盲断案例（带有真实反馈底牌）
            var pubReq = new PublishCaseRequestDto(
                DivinationType: DivinationType.Bazi,
                CastTimeUtc: new DateTime(1992, 8, 15, 10, 0, 0, DateTimeKind.Utc),
                BackgroundInfo: "男命，国企职员，请断2018戊戌年吉凶。",
                QuestionText: "2018年发生了什么事？",
                RealFeedback: "【真实底牌】：2018年因原局辰戌冲导致胃病住院。",
                RevealTimeUtc: DateTime.UtcNow.AddDays(1), // 明天才开奖
                Tags: new List<string> { "#辰戌冲", "#疾病" },
                BaziGender: Gender.Male
            );

            var pubResult = await CaseService.PublishCaseAsync(publisher.Id, pubReq);
            pubResult.IsSuccess.Should().BeTrue();
            Guid caseId = pubResult.Data;

            // UserA 率先提交了自己的断语
            var submitA = await CaseService.SubmitReadingAsync(
                userA.Id,
                new SubmitReadingRequestDto(caseId, "我认为2018戊戌年土旺克水，要注意脾胃或肾脏问题。"));
            submitA.IsSuccess.Should().BeTrue();

            // -----------------------------------------------------------------
            // Act 1: 尚未交卷的 UserB 查看案例详情
            // -----------------------------------------------------------------
            var detailBeforeSubmit = (await CaseService.GetCaseDetailAsync(caseId, userB.Id)).Data!;

            // Assert 1: 触发第一层锁！UserB 既看不到真实反馈，也看不到 UserA 的断语
            detailBeforeSubmit.HasCurrentUserSubmitted.Should().BeFalse();
            detailBeforeSubmit.IsReadingsLocked.Should().BeTrue("未交卷前必须锁定他人断语");
            detailBeforeSubmit.Readings.Should().BeEmpty("未交卷前后端必须清空断语列表防止抓包");
            detailBeforeSubmit.RealFeedback.Should().BeNull("盲断期间严禁泄露真实反馈答案");

            // -----------------------------------------------------------------
            // Act 2: UserB 提交自己的断语，然后再次查看案例详情
            // -----------------------------------------------------------------
            var submitB = await CaseService.SubmitReadingAsync(
                userB.Id,
                new SubmitReadingRequestDto(caseId, "辰戌冲动了印星，我断这年换了工作单位。"));
            submitB.IsSuccess.Should().BeTrue();

            var detailAfterSubmit = (await CaseService.GetCaseDetailAsync(caseId, userB.Id)).Data!;

            // Assert 2: 解锁他人断语，但第二层锁依然生效（未到揭晓时间，不准看底牌答案）
            detailAfterSubmit.HasCurrentUserSubmitted.Should().BeTrue();
            detailAfterSubmit.IsReadingsLocked.Should().BeFalse("交卷后应立即解锁群友断语区");
            detailAfterSubmit.Readings.Should().HaveCount(2, "此时应能看到 UserA 和 UserB 自己的共 2 条断语");
            detailAfterSubmit.RealFeedback.Should().BeNull("虽然已交卷，但未到揭晓时间，底牌答案依然必须为 null！");

            // -----------------------------------------------------------------
            // Act 3: 发布者本人查看案例详情（即使发布者自己没写断语）
            // -----------------------------------------------------------------
            var detailForPublisher = (await CaseService.GetCaseDetailAsync(caseId, publisher.Id)).Data!;
            detailForPublisher.IsReadingsLocked.Should().BeFalse("发布者本人可随时查看群友提交的断语");
            detailForPublisher.Readings.Should().HaveCount(2);
        }

        [Fact(DisplayName = "防刷屏与懒加载开奖：禁止重复提交断语，且到达揭晓时间后自动公开底牌答案")]
        public async Task DuplicateReading_Should_Conflict_And_DueCase_Should_AutoReveal()
        {
            // Arrange: 发布一个揭晓时间已过（1分钟前到期）的盲断案例
            var publisher = await SeedUserAsync("pub_master", UserRole.Publisher);
            var userA = await SeedUserAsync("reader_a", UserRole.User);
            var userB = await SeedUserAsync("reader_b", UserRole.User);

            var pubReq = new PublishCaseRequestDto(
                DivinationType: DivinationType.LiuYao,
                CastTimeUtc: DateTime.UtcNow.AddDays(-1),
                BackgroundInfo: "测试到期自动揭晓",
                QuestionText: "失物能否找回？",
                RealFeedback: "【真实底牌】：次日酉时在床头柜找回。",
                RevealTimeUtc: DateTime.UtcNow.AddMinutes(-1), // 1分钟前已到期！
                Tags: new List<string> { "#失物" },
                LiuYaoRawLines: new List<int> { 7, 7, 8, 9, 8, 7 }
            );

            Guid caseId = (await CaseService.PublishCaseAsync(publisher.Id, pubReq)).Data;

            // Act & Assert 1: UserA 第一次提交成功，第二次重复提交应返回 409 Conflict
            var firstSubmit = await CaseService.SubmitReadingAsync(
                userA.Id, new SubmitReadingRequestDto(caseId, "用神旺相，必能找回！"));
            var secondSubmit = await CaseService.SubmitReadingAsync(
                userA.Id, new SubmitReadingRequestDto(caseId, "我想再水一条断语"));

            firstSubmit.IsSuccess.Should().BeTrue();
            secondSubmit.IsSuccess.Should().BeFalse();
            secondSubmit.ErrorCode.Should().Be(ServiceErrorCode.Conflict);

            // Act & Assert 2: 从未交卷的 UserB 访问该已到期案例，应触发懒加载自动开奖！
            var detail = (await CaseService.GetCaseDetailAsync(caseId, userB.Id)).Data!;

            detail.Status.Should().Be(CaseStatus.Revealed, "已过揭晓时间，访问时应自动翻转为 Revealed 状态");
            detail.IsReadingsLocked.Should().BeFalse("已揭晓的案例对所有人开放复盘");
            detail.RealFeedback.Should().Be("【真实底牌】：次日酉时在床头柜找回。");
            detail.Readings.Should().HaveCount(1);
        }
    }
}
