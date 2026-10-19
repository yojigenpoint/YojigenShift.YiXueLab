using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;

namespace YojigenShift.YiXueLab.API.Controllers
{
    [Route("api/meta")]
    public class MetaController : ApiControllerBase
    {
        private readonly IDivinationPlotterService _plotterService;

        public MetaController(IDivinationPlotterService plotterService)
        {
            _plotterService = plotterService;
        }

        /// <summary>
        /// 获取 YiFramework 全局多语言术语字典 (前端启动时拉取一次存入 Pinia)
        /// </summary>
        [HttpGet("dictionary")]
        [AllowAnonymous]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public IActionResult GetGlobalDictionary()
        {
            var dict = _plotterService.GetGlobalLocalizedDictionary();
            return Ok(dict);
        }

        /// <summary>
        /// 实时排盘预览接口：发布者在填写表单时，可随时点击“预览盘面”核对八字/六爻/梅花是否正确
        /// </summary>
        [HttpPost("preview-plot")]
        [Authorize]
        public IActionResult PreviewPlot([FromBody] PublishCaseRequestDto request)
        {
            var result = _plotterService.Plot(request);
            return FromServiceResult(result);
        }
    }
}
