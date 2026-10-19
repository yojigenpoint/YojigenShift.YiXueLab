namespace YojigenShift.YiXueLab.API.Auth
{
    /// <summary>
    /// 提交断语时的精简请求体（CaseId 从路由获取）
    /// </summary>
    public record SubmitReadingBodyDto(string Content);
}
