using System.Text.Json.Serialization;

namespace YojigenShift.YiXueLab.DAL.JsonModels
{
    /// <summary>
    /// cases.divination_data (JSONB) 的多态抽象基类。
    /// System.Text.Json 会自动在 JSON 中写入 "$type" 字段以区分四大门类。
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(BaziCaseData), typeDiscriminator: "bazi")]
    [JsonDerivedType(typeof(LiuYaoCaseData), typeDiscriminator: "liuyao")]
    [JsonDerivedType(typeof(QiMenCaseData), typeDiscriminator: "qimen")]
    [JsonDerivedType(typeof(MeiHuaCaseData), typeDiscriminator: "meihua")]
    public abstract class DivinationPayloadBase
    {
        /// <summary>
        /// 核心排盘/起卦公历时间 (UTC)
        /// </summary>
        public DateTime CastTimeUtc { get; set; }

        /// <summary>
        /// 起卦/出生时的四柱干支索引底座
        /// </summary>
        public FourPillarsDto Pillars { get; set; } = new(0, 0, 0, 0);
    }
}
