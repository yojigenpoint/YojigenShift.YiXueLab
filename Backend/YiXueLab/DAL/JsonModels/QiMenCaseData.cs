using YojigenShift.YiFramework.QiMen.Models;

namespace YojigenShift.YiXueLab.DAL.JsonModels
{
    /// <summary>
    /// 奇门遁甲 JSONB 存储结构
    /// </summary>
    public class QiMenCaseData : DivinationPayloadBase
    {
        public QiMenChart Chart { get; set; } = new();
    }
}
