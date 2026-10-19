namespace YojigenShift.YiXueLab.BLL.Common
{
    public enum ServiceErrorCode
    {
        None = 0,
        ValidationError = 400,  // 参数不合法
        Unauthorized = 401,     // 账号密码错误或未登录
        Forbidden = 403,        // 权限不足或账号被封禁
        NotFound = 404,         // 资源不存在
        Conflict = 409          // 唯一键冲突（如用户名已存在、已提交过断语）
    }
}
