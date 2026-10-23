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
        /// 核心排盘/起卦公历时间 (用户输入钟表时间)
        /// </summary>
        public DateTime CastTimeUtc { get; set; }

        /// <summary>
        /// 经夏令时与经度换算后的实际排盘真太阳时 (若启用真太阳时)
        /// </summary>
        public DateTime? TrueSolarTimeUtc { get; set; }

        /// <summary>
        /// 出生地/起卦地名称 (如：四川省成都市 / 美国纽约 New York)
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// 实际用于排盘的地理经度 (东经为正，西经为负，如成都 104.06, 纽约 -74.01)
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// 是否启用了真太阳时校准
        /// </summary>
        public bool IsTrueSolarTime { get; set; } = true;

        /// <summary>
        /// 是否扣除了夏令时 (-1小时)
        /// </summary>
        public bool IsDaylightSavingTime { get; set; } = false;

        /// <summary>
        /// 起卦/出生时的四柱干支索引底座
        /// </summary>
        public FourPillarsDto Pillars { get; set; } = new(0, 0, 0, 0);
    }
}
