using FluentAssertions;
using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.Tests.BLL
{
    public class AiAssistantServiceTests : BllTestBase
    {
        [Fact(DisplayName = "AI 断语润色：未配置外部密钥时应自动启用三段式规则降级，过短草稿应返回 400")]
        public async Task PolishReadingAsync_Should_FormatIntoThreeSections_And_ValidateLength()
        {
            // Act 1: 传入过短草稿
            var shortRes = await AiService.PolishReadingAsync(
                new PolishReadingRequestDto(DivinationType.Bazi, "太短"));
            shortRes.IsSuccess.Should().BeFalse();
            shortRes.ErrorCode.Should().Be(ServiceErrorCode.ValidationError);

            // Act 2: 传入正常口语化八字断语草稿
            var validRes = await AiService.PolishReadingAsync(new PolishReadingRequestDto(
                DivinationType.Bazi,
                "戊土日主生在巳月火旺身强。2018戊戌年触发辰戌冲水库。断该年必有肠胃住院及工作降职之灾。"));

            // Assert 2
            validRes.IsSuccess.Should().BeTrue();
            validRes.Data!.IsRuleFallback.Should().BeTrue();
            validRes.Data.PolishedContent.Should().Contain("【1. 原局旺衰与格局】");
            validRes.Data.PolishedContent.Should().Contain("【2. 岁运引发与应期】");
            validRes.Data.PolishedContent.Should().Contain("【3. 综合吉凶结论】");
        }

        [Fact(DisplayName = "AI 智能标签提取：应能从案例背景与反馈中精准识别易学考点标签")]
        public async Task SuggestTagsAsync_Should_Extract_Matching_YiKeywords()
        {
            // Arrange
            var req = new SuggestTagsRequestDto(
                DivinationType: DivinationType.LiuYao,
                BackgroundInfo: "求测人金项链遗失，摇卦寻物。",
                QuestionText: "问能否找回？",
                RealFeedback: "世爻用神旬空，次日冲空找回。"
            );

            // Act
            var res = await AiService.SuggestTagsAsync(req);

            // Assert
            res.IsSuccess.Should().BeTrue();
            res.Data!.Tags.Should().Contain("#六爻纳甲");
            res.Data.Tags.Should().Contain("#失物寻物");
            res.Data.Tags.Should().Contain("#用神旬空");
        }
    }
}
