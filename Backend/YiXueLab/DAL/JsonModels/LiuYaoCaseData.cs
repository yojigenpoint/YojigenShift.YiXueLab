using YojigenShift.YiFramework.Enums;
using YojigenShift.YiFramework.LiuYao.Models;

namespace YojigenShift.YiXueLab.DAL.JsonModels
{
    /// <summary>
    /// 六爻排盘 JSONB 存储结构 (包含本卦、变卦纳甲、六兽空亡及日月旺衰评估)
    /// </summary>
    public class LiuYaoCaseData : DivinationPayloadBase
    {
        public List<YaoType> RawLines { get; set; } = new();
        public List<int> MovingLines { get; set; } = new();
        public MountedHexagram Original { get; set; } = new();
        public MountedHexagram Future { get; set; } = new();
        public Dictionary<int, YaoStatus> Evaluation { get; set; } = new();
    }
}
