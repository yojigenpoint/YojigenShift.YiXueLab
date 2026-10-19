namespace YojigenShift.YiXueLab.API.Common
{
    /// <summary>
    /// 标准 API 错误响应格式
    /// </summary>
    public record ApiErrorResponse(
        int Code,
        string Message,
        DateTime TimestampUtc
    );
}
