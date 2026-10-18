namespace YojigenShift.YiXueLab.DAL.Enums
{
    /// <summary>
    /// 用户角色枚举 (对应 PostgreSQL: user_role_enum)
    /// </summary>
    public enum UserRole
    {
        User = 0,       // 普通群友（参与盲断、检索藏经阁）
        Publisher = 1,  // 案例收集志愿者/发布者
        Admin = 2       // 管理员（生成邀请码、管理用户与标签）
    }
}
