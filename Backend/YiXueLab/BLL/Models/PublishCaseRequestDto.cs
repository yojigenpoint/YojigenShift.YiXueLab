using YojigenShift.YiFramework.Enums;
using YojigenShift.YiFramework.QiMen.Models;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.BLL.Models
{
    public record PublishCaseRequestDto(
        DivinationType DivinationType,
        DateTime CastTimeUtc,
        string BackgroundInfo,
        string QuestionText,
        string? RealFeedback,
        DateTime RevealTimeUtc,
        List<string> Tags,

        // 八字专属参数
        Gender? BaziGender = null,
        string? Location = null,
        bool IsTrueSolarTime = true,

        // 六爻专属参数：长度为 6 的数组，对应初爻到上爻 (6=老阴, 7=少阳, 8=少阴, 9=老阳)
        List<int>? LiuYaoRawLines = null,

        // 梅花易数专属参数："Time" 或 "Numbers"
        string? MeiHuaCastMethod = null,
        int[]? MeiHuaNumbers = null,
        int? LunarMonth = null,
        int? LunarDay = null,

        // 奇门遁甲专属参数（若 YiFramework 有 QiMenPlotter 可自动计算，也支持外部传入）
        QiMenChart? PreCalculatedQiMenChart = null
    );
}
