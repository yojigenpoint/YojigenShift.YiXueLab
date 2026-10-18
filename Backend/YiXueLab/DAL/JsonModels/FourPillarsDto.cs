namespace YojigenShift.YiXueLab.DAL.JsonModels
{
    /// <summary>
    /// 公共时空底座：六十甲子四柱索引 (0-59)
    /// </summary>
    public record FourPillarsDto(
        int YearIdx,
        int MonthIdx,
        int DayIdx,
        int HourIdx
    );
}
