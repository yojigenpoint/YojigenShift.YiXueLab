using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YojigenShift.YiXueLab.API.Auth;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.API.Controllers
{
    [Route("api/cases")]
    [Authorize]
    public class CasesController : ApiControllerBase
    {
        private readonly ICaseService _caseService;

        public CasesController(ICaseService caseService)
        {
            _caseService = caseService;
        }

        /// <summary>
        /// 【演武场首页】：获取最新的一道进行中盲断题（自动携带当前用户的盲断防作弊锁状态）
        /// </summary>
        [HttpGet("latest-blind")]
        public async Task<IActionResult> GetLatestBlindCase(
            [FromQuery] DivinationType? type = null,
            CancellationToken ct = default)
        {
            var result = await _caseService.GetLatestBlindCaseAsync(CurrentUserId, type, ct);
            return FromServiceResult(result);
        }

        /// <summary>
        /// 【案例详情与复盘页】：根据 ID 获取案例详情（未交卷者自动屏蔽他人断语，揭晓前屏蔽底牌反馈）
        /// </summary>
        [HttpGet("{caseId:guid}")]
        public async Task<IActionResult> GetCaseDetail(
            [FromRoute] Guid caseId,
            CancellationToken ct = default)
        {
            var result = await _caseService.GetCaseDetailAsync(caseId, CurrentUserId, ct);
            return FromServiceResult(result);
        }

        /// <summary>
        /// 【藏经阁检索】：支持按状态、门类（八字/六爻等）、关键词、标签 ID 列表分页查询
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchCases(
            [FromQuery] CaseSearchFilterDto filter,
            CancellationToken ct = default)
        {
            var result = await _caseService.SearchCasesAsync(filter, ct);
            return FromServiceResult(result);
        }

        /// <summary>
        /// 【发布新案例】：仅限管理员(Admin)或案例收集志愿者(Publisher)调用
        /// </summary>
        [HttpPost]
        [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.Publisher)}")]
        public async Task<IActionResult> PublishCase(
            [FromBody] PublishCaseRequestDto request,
            CancellationToken ct = default)
        {
            var result = await _caseService.PublishCaseAsync(CurrentUserId, request, ct);
            if (!result.IsSuccess)
            {
                return FromServiceResult(result);
            }

            return CreatedAtAction(
                nameof(GetCaseDetail),
                new { caseId = result.Data },
                new { caseId = result.Data });
        }

        /// <summary>
        /// 【交卷解锁】：群友提交自己的盲断分析，提交成功后即可调用详情接口查看其他易友的断语
        /// </summary>
        [HttpPost("{caseId:guid}/readings")]
        public async Task<IActionResult> SubmitReading(
            [FromRoute] Guid caseId,
            [FromBody] SubmitReadingBodyDto body,
            CancellationToken ct = default)
        {
            var dto = new SubmitReadingRequestDto(caseId, body.Content);
            var result = await _caseService.SubmitReadingAsync(CurrentUserId, dto, ct);
            return FromServiceResult(result);
        }

        /// <summary>
        /// 【断语点赞】：为精彩的群友断语点赞 (+1)
        /// </summary>
        [HttpPost("readings/{readingId:guid}/like")]
        public async Task<IActionResult> LikeReading(
            [FromRoute] Guid readingId,
            CancellationToken ct = default)
        {
            var result = await _caseService.LikeReadingAsync(readingId, ct);
            return FromServiceResult(result);
        }
    }
}
