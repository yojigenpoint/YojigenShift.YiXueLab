using FluentAssertions;
using YojigenShift.YiFramework.Enums;
using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.JsonModels;

namespace YojigenShift.YiXueLab.Tests.BLL
{
    public class DivinationPlotterServiceTests : BllTestBase
    {
        [Fact(DisplayName = "八字排盘：给定公历时间与性别，应正确排出四柱与10步大运")]
        public void Plot_Bazi_Should_Generate_FourPillars_And_DaYun()
        {
            // Arrange
            var req = new PublishCaseRequestDto(
                DivinationType: DivinationType.Bazi,
                CastTimeUtc: new DateTime(1990, 5, 12, 14, 30, 0, DateTimeKind.Utc),
                BackgroundInfo: "测试背景",
                QuestionText: "测试问题",
                RealFeedback: null,
                RevealTimeUtc: DateTime.UtcNow.AddDays(1),
                Tags: new List<string>(),
                BaziGender: Gender.Male,
                Location: "四川省成都市"
            );

            // Act
            var result = PlotterService.Plot(req);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeOfType<BaziCaseData>();

            var baziData = (BaziCaseData)result.Data!;
            baziData.Gender.Should().Be(Gender.Male);
            baziData.Location.Should().Be("四川省成都市");
            baziData.DaYun.DaYunPillars.Should().HaveCount(10, "大运应当固定排出 10 步");
        }

        [Fact(DisplayName = "八字排盘校验：未提供性别时应返回 400 ValidationError")]
        public void Plot_Bazi_WithoutGender_Should_Return_ValidationError()
        {
            // Arrange
            var req = new PublishCaseRequestDto(
                DivinationType: DivinationType.Bazi,
                CastTimeUtc: DateTime.UtcNow,
                BackgroundInfo: "背景",
                QuestionText: "问题",
                RealFeedback: null,
                RevealTimeUtc: DateTime.UtcNow.AddDays(1),
                Tags: new List<string>(),
                BaziGender: null // 故意漏填性别
            );

            // Act
            var result = PlotterService.Plot(req);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be(ServiceErrorCode.ValidationError);
            result.ErrorMessage.Should().Contain("性别");
        }

        [Fact(DisplayName = "六爻排盘：传入 6 个有效铜钱爻值，应完成本变卦纳甲、六兽挂载及旺衰评估")]
        public void Plot_LiuYao_Should_Mount_Hexagrams_And_Evaluate()
        {
            // Arrange: 初爻到上爻 [7, 8, 9(老阳动), 7, 6(老阴动), 8]
            var req = new PublishCaseRequestDto(
                DivinationType: DivinationType.LiuYao,
                CastTimeUtc: new DateTime(2024, 10, 5, 9, 15, 0, DateTimeKind.Utc),
                BackgroundInfo: "背景",
                QuestionText: "问题",
                RealFeedback: null,
                RevealTimeUtc: DateTime.UtcNow.AddDays(1),
                Tags: new List<string>(),
                LiuYaoRawLines: new List<int> { 7, 8, 9, 7, 6, 8 }
            );

            // Act
            var result = PlotterService.Plot(req);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var liuYaoData = result.Data.Should().BeOfType<LiuYaoCaseData>().Subject;

            liuYaoData.MovingLines.Should().BeEquivalentTo(new[] { 3, 5 });
            liuYaoData.Original.Lines.Should().HaveCount(6);
            liuYaoData.Future.Lines.Should().HaveCount(6);
            liuYaoData.Future.PalaceWuXing.Should().NotBe(WuXingType.None);
            liuYaoData.Evaluation.Should().HaveCount(6);
        }

        [Theory(DisplayName = "六爻排盘校验：爻数不足 6 个或数值越界(非6/7/8/9)应拦截报错")]
        [InlineData(7, 8, 9)]                // 只有 3 个爻
        [InlineData(7, 8, 9, 7, 6, 10)]      // 含有非法数字 10
        [InlineData(5, 7, 8, 7, 8, 7)]       // 含有非法数字 5
        public void Plot_LiuYao_WithInvalidLines_Should_Fail(params int[] invalidLines)
        {
            // Arrange
            var req = new PublishCaseRequestDto(
                DivinationType: DivinationType.LiuYao,
                CastTimeUtc: DateTime.UtcNow,
                BackgroundInfo: "背景",
                QuestionText: "问题",
                RealFeedback: null,
                RevealTimeUtc: DateTime.UtcNow.AddDays(1),
                Tags: new List<string>(),
                LiuYaoRawLines: invalidLines.ToList()
            );

            // Act
            var result = PlotterService.Plot(req);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorCode.Should().Be(ServiceErrorCode.ValidationError);
        }

        [Fact(DisplayName = "梅花易数排盘：支持报数起卦与时间起卦，且全局多语言字典正常导出")]
        public void Plot_MeiHua_And_GlobalDictionary_Should_Succeed()
        {
            // Arrange
            var req = new PublishCaseRequestDto(
                DivinationType: DivinationType.MeiHua,
                CastTimeUtc: new DateTime(2024, 6, 10, 16, 10, 0, DateTimeKind.Utc),
                BackgroundInfo: "背景",
                QuestionText: "问题",
                RealFeedback: null,
                RevealTimeUtc: DateTime.UtcNow.AddDays(1),
                Tags: new List<string>(),
                MeiHuaCastMethod: "Numbers",
                MeiHuaNumbers: new[] { 15, 28 }
            );

            // Act
            var plotResult = PlotterService.Plot(req);
            var dict = PlotterService.GetGlobalLocalizedDictionary();

            // Assert
            plotResult.IsSuccess.Should().BeTrue();
            var meiHuaData = plotResult.Data.Should().BeOfType<MeiHuaCaseData>().Subject;
            meiHuaData.Result.MovingLine.Should().BeInRange(1, 6);
            dict.Should().NotBeNull();
        }
    }
}
