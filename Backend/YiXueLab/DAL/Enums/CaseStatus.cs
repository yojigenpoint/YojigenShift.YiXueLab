namespace YojigenShift.YiXueLab.DAL.Enums
{
    /// <summary>
    /// 案例状态枚举 (对应 PostgreSQL: case_status_enum)
    /// </summary>
    public enum CaseStatus
    {
        Blind = 0,      // 盲断进行中（隐藏真实反馈，未提交者不可看他人断语）
        Revealed = 1    // 已揭晓答案（公开真实反馈，开放复盘讨论）
    }
}
