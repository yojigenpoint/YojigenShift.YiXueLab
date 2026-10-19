namespace YojigenShift.YiXueLab.BLL.Common
{
    /// <summary>
    /// 通用分页响应包装器
    /// </summary>
    public record PagedResultDto<T>(
        List<T> Items,
        int TotalCount,
        int PageIndex,
        int PageSize
    );
}
