using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.JsonModels;

namespace YojigenShift.YiXueLab.BLL.Models
{
    /// <summary>
    /// 案例详情视图：包含排盘 JSON 数据、盲断锁状态及断语列表
    /// </summary>
    public record CaseDetailDto(
        Guid Id,
        DivinationType DivinationType,
        string PublisherNickname,
        DivinationPayloadBase DivinationData,
        string BackgroundInfo,
        string QuestionText,
        CaseStatus Status,
        DateTime RevealTime,
        DateTime CreatedAt,
        List<string> Tags,

        // --- 盲断逻辑锁控制字段 ---
        bool HasCurrentUserSubmitted,
        bool IsReadingsLocked,
        string? RealFeedback,          // 盲断中且未揭晓时，此字段强制为 null！
        List<ReadingDto> Readings      // 若 IsReadingsLocked 为 true，此列表强制为空！
    );
}
