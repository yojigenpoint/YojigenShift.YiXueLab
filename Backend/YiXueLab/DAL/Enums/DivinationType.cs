namespace YojigenShift.YiXueLab.DAL.Enums
{
    /// <summary>
    /// 易学门类枚举 (对应 PostgreSQL: divination_type_enum)
    /// </summary>
    public enum DivinationType
    {
        Bazi = 0,       // 四柱八字
        LiuYao = 1,     // 六爻纳甲
        QiMen = 2,      // 奇门遁甲
        MeiHua = 3,     // 梅花易数
        Other = 99      // 其他杂占
    }
}
