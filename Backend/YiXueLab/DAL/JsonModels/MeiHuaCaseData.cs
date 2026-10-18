using YojigenShift.YiFramework.Core;

namespace YojigenShift.YiXueLab.DAL.JsonModels
{
    /// <summary>
    /// 梅花易数 JSONB 存储结构
    /// </summary>
    public class MeiHuaCaseData : DivinationPayloadBase
    {
        public string CastMethod { get; set; } = "Time"; // "Time" 或 "Numbers"
        public int[]? RawNumbers { get; set; }
        public MeiHuaResult Result { get; set; } = new();
    }
}
