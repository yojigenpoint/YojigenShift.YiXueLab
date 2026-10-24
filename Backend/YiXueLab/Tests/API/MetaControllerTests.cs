using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.JsonModels;

namespace YojigenShift.YiXueLab.Tests.API
{
    public class MetaControllerTests : ApiTestBase
    {
        [Fact(DisplayName = "元数据与排盘预览：应正确返回全局术语字典，且排盘预览能正确区分 200 与 400")]
        public async Task MetaController_Dictionary_And_PreviewPlot_Should_Work()
        {
            // Act 1: 获取全局多语言字典
            var dictResult = MetaController.GetGlobalDictionary();
            var dictOk = dictResult.Should().BeOfType<OkObjectResult>().Subject;
            dictOk.Value.Should().NotBeNull();

            // Act 2: 传入合法的六爻参数进行实时排盘预览
            var validPreviewReq = new PublishCaseRequestDto(
                DivinationType: DivinationType.LiuYao,
                CastTimeUtc: new DateTime(2024, 10, 5, 9, 15, 0, DateTimeKind.Utc),
                BackgroundInfo: "预览背景",
                QuestionText: "预览问题",
                RealFeedback: null,
                RevealTimeUtc: DateTime.UtcNow.AddDays(1),
                Tags: new List<string>(),
                LiuYaoRawLines: new List<int> { 7, 8, 9, 7, 6, 8 }
            );

            var previewOk = MetaController.PreviewPlot(validPreviewReq)
                .Should().BeOfType<OkObjectResult>().Subject;
            previewOk.Value.Should().BeOfType<LiuYaoCaseData>();

            // Act 3: 传入缺失性别的八字参数，应自动映射为 400 Bad Request
            var invalidPreviewReq = new PublishCaseRequestDto(
                DivinationType: DivinationType.Bazi,
                CastTimeUtc: DateTime.UtcNow,
                BackgroundInfo: "预览",
                QuestionText: "预览",
                RealFeedback: null,
                RevealTimeUtc: DateTime.UtcNow.AddDays(1),
                Tags: new List<string>(),
                BaziGender: null // 故意漏填性别
            );

            var previewBad = MetaController.PreviewPlot(invalidPreviewReq)
                .Should().BeOfType<ObjectResult>().Subject;
            previewBad.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            // Act 4: 验证 AiController 断语润色与智能标签提取接口
            var polishOk = (await AiController.PolishReading(new PolishReadingRequestDto(
                DivinationType.LiuYao,
                "世爻酉金临月建极旺但旬空。五爻独发化申金。断在家中正西方抽屉必能找回。")))
                .Should().BeOfType<OkObjectResult>().Subject;
            polishOk.Value.Should().BeOfType<PolishReadingResponseDto>();

            var tagsOk = (await AiController.SuggestTags(new SuggestTagsRequestDto(
                DivinationType.Bazi,
                "男命2018戊戌年辰戌冲",
                "问何事？",
                "工作变动且脾胃手术")))
                .Should().BeOfType<OkObjectResult>().Subject;
            var tagsDto = tagsOk.Value.Should().BeOfType<SuggestTagsResponseDto>().Subject;
            tagsDto.Tags.Should().Contain("#辰戌冲");
        }
    }
}
