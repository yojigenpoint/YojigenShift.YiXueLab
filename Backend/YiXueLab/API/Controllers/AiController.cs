using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;

namespace YojigenShift.YiXueLab.API.Controllers
{
    [Route("api/ai")]
    [Authorize]
    public class AiController : ApiControllerBase
    {
        private readonly IAiAssistantService _aiService;

        public AiController(IAiAssistantService aiService)
        {
            _aiService = aiService;
        }

        /// <summary>
        /// 【断语排版助手】：将用户口语化的断卦草稿梳理为对应门类的三段式标准结构
        /// </summary>
        [HttpPost("polish-reading")]
        public async Task<IActionResult> PolishReading(
            [FromBody] PolishReadingRequestDto request,
            CancellationToken ct = default)
        {
            var result = await _aiService.PolishReadingAsync(request, ct);
            return FromServiceResult(result);
        }

        /// <summary>
        /// 【智能标签提取】：根据发布者填写的背景、问题与真实反馈，自动提炼 3~5 个考点标签
        /// </summary>
        [HttpPost("suggest-tags")]
        public async Task<IActionResult> SuggestTags(
            [FromBody] SuggestTagsRequestDto request,
            CancellationToken ct = default)
        {
            var result = await _aiService.SuggestTagsAsync(request, ct);
            return FromServiceResult(result);
        }
    }
}
