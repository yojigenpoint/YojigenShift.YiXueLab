using YojigenShift.YiFramework.Core;
using YojigenShift.YiFramework.Enums;

namespace YojigenShift.YiXueLab.DAL.JsonModels
{
    /// <summary>
    /// 八字排盘 JSONB 存储结构
    /// </summary>
    public class BaziCaseData : DivinationPayloadBase
    {
        public Gender Gender { get; set; }
        public string? Location { get; set; }
        public bool IsTrueSolarTime { get; set; } = true;
        public DaYunResult DaYun { get; set; } = new();
    }
}
