using YojigenShift.YiFramework.Core;
using YojigenShift.YiFramework.Enums;
using YojigenShift.YiFramework.Extensions;
using YojigenShift.YiFramework.LiuYao.Logic;
using YojigenShift.YiFramework.LiuYao.Models;
using YojigenShift.YiFramework.QiMen.Models;
using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.JsonModels;

namespace YojigenShift.YiXueLab.BLL.Services
{
    public class DivinationPlotterService : IDivinationPlotterService
    {
        public ServiceResult<DivinationPayloadBase> Plot(PublishCaseRequestDto req)
        {
            // 统一確保时间为 UTC
            var utcTime = req.CastTimeUtc.Kind == DateTimeKind.Utc
                ? req.CastTimeUtc
                : DateTime.SpecifyKind(req.CastTimeUtc, DateTimeKind.Utc);

            // 1. 调用 YiFramework.Core.BaziPlotter 排出四柱底座 (所有门类通用)
            var (yIdx, mIdx, dIdx, hIdx) = BaziPlotter.Plot(utcTime);
            var pillarsDto = new FourPillarsDto(yIdx, mIdx, dIdx, hIdx);

            return req.DivinationType switch
            {
                DivinationType.Bazi => PlotBazi(utcTime, pillarsDto, req),
                DivinationType.LiuYao => PlotLiuYao(utcTime, pillarsDto, req),
                DivinationType.MeiHua => PlotMeiHua(utcTime, pillarsDto, req),
                DivinationType.QiMen => PlotQiMen(utcTime, pillarsDto, req),
                _ => ServiceResult<DivinationPayloadBase>.Failure("暂不支持该易学门类的自动排盘。")
            };
        }

        private static ServiceResult<DivinationPayloadBase> PlotBazi(
            DateTime utcTime,
            FourPillarsDto pillars,
            PublishCaseRequestDto req)
        {
            if (!req.BaziGender.HasValue)
                return ServiceResult<DivinationPayloadBase>.Failure("八字案例必须提供性别 (BaziGender)。");

            var yearStem = GanZhiMath.GetStem(pillars.YearIdx);
            var daYun = BaziHelpers.CalculateDaYun(utcTime, req.BaziGender.Value, yearStem, pillars.MonthIdx);

            return ServiceResult<DivinationPayloadBase>.Success(new BaziCaseData
            {
                CastTimeUtc = utcTime,
                Pillars = pillars,
                Gender = req.BaziGender.Value,
                Location = req.Location,
                IsTrueSolarTime = req.IsTrueSolarTime,
                DaYun = daYun
            });
        }

        private static ServiceResult<DivinationPayloadBase> PlotLiuYao(
            DateTime utcTime,
            FourPillarsDto pillars,
            PublishCaseRequestDto req)
        {
            if (req.LiuYaoRawLines == null || req.LiuYaoRawLines.Count != 6)
                return ServiceResult<DivinationPayloadBase>.Failure("六爻案例必须提供完整的 6 个爻数值 (初爻到上爻，值为 6/7/8/9)。");

            var yaoLines = new List<YaoType>(6);
            foreach (var val in req.LiuYaoRawLines)
            {
                if (val < 6 || val > 9)
                    return ServiceResult<DivinationPayloadBase>.Failure($"非法的六爻数值: {val}。只允许 6(老阴), 7(少阳), 8(少阴), 9(老阳)。");

                yaoLines.Add((YaoType)val);
            }

            // 1. 构造起卦结果并装卦 (纳甲、寻宫、定世应、配六亲)
            var castResult = new LiuYaoResult { Lines = yaoLines };
            var (orig, fut) = NaJiaLogic.Mount(castResult);

            // 2. 挂载六兽与旬空
            var dayStem = GanZhiMath.GetStem(pillars.DayIdx);
            var dayBranch = GanZhiMath.GetBranch(pillars.DayIdx);
            var monthBranch = GanZhiMath.GetBranch(pillars.MonthIdx);

            SixBeastsLogic.AttachContext(orig, dayStem, dayBranch);
            SixBeastsLogic.AttachContext(fut, dayStem, dayBranch);

            // 3. 评估本卦各爻在日月的旺衰状态 (月破、暗动、日合等)
            var evaluation = LiuYaoEvaluator.Evaluate(orig, monthBranch, dayBranch);

            return ServiceResult<DivinationPayloadBase>.Success(new LiuYaoCaseData
            {
                CastTimeUtc = utcTime,
                Pillars = pillars,
                RawLines = yaoLines,
                MovingLines = castResult.MovingLines,
                Original = orig,
                Future = fut,
                Evaluation = evaluation
            });
        }

        private static ServiceResult<DivinationPayloadBase> PlotMeiHua(
            DateTime utcTime,
            FourPillarsDto pillars,
            PublishCaseRequestDto req)
        {
            // 梅花易数地支索引为 1-12 (子=1, 丑=2...)，而 EarthlyBranch 枚举为 0-11
            int hourBranchOneBased = (int)GanZhiMath.GetBranch(pillars.HourIdx) + 1;
            int yearBranchOneBased = (int)GanZhiMath.GetBranch(pillars.YearIdx) + 1;

            MeiHuaResult result;
            string method = req.MeiHuaCastMethod ?? "Time";

            if (string.Equals(method, "Numbers", StringComparison.OrdinalIgnoreCase))
            {
                if (req.MeiHuaNumbers == null || req.MeiHuaNumbers.Length < 2)
                    return ServiceResult<DivinationPayloadBase>.Failure("梅花报数起卦必须提供至少 2 个数字。");

                result = MeiHuaCaster.CastByNumbers(req.MeiHuaNumbers[0], req.MeiHuaNumbers[1], hourBranchOneBased);
            }
            else
            {
                int month = req.LunarMonth ?? utcTime.Month;
                int day = req.LunarDay ?? utcTime.Day;
                result = MeiHuaCaster.CastByTime(yearBranchOneBased, month, day, hourBranchOneBased);
            }

            return ServiceResult<DivinationPayloadBase>.Success(new MeiHuaCaseData
            {
                CastTimeUtc = utcTime,
                Pillars = pillars,
                CastMethod = method,
                RawNumbers = req.MeiHuaNumbers,
                Result = result
            });
        }

        private static ServiceResult<DivinationPayloadBase> PlotQiMen(
            DateTime utcTime,
            FourPillarsDto pillars,
            PublishCaseRequestDto req)
        {
            var chart = req.PreCalculatedQiMenChart ?? new QiMenChart
            {
                HourStem = GanZhiMath.GetStem(pillars.HourIdx),
                HourBranch = GanZhiMath.GetBranch(pillars.HourIdx)
            };

            return ServiceResult<DivinationPayloadBase>.Success(new QiMenCaseData
            {
                CastTimeUtc = utcTime,
                Pillars = pillars,
                Chart = chart
            });
        }

        /// <summary>
        /// 调用 YiExtensions 生成前端全局多语言术语字典
        /// </summary>
        public object GetGlobalLocalizedDictionary()
        {
            return new
            {
                WuXing = Enum.GetValues<WuXingType>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                Stems = Enum.GetValues<HeavenlyStem>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                Branches = Enum.GetValues<EarthlyBranch>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                Trigrams = Enum.GetValues<TrigramName>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                TenGods = Enum.GetValues<TenGod>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                LifeStages = Enum.GetValues<LifeStage>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                SixRelations = Enum.GetValues<SixRelation>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                SixBeasts = Enum.GetValues<SixBeast>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                YaoTypes = Enum.GetValues<YaoType>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                HexagramNames = Enum.GetValues<HexagramName>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                QiMenStars = Enum.GetValues<QiMenStar>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                QiMenDoors = Enum.GetValues<QiMenDoor>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                QiMenGods = Enum.GetValues<QiMenGod>().ToDictionary(x => (int)x, x => x.GetLocalizedName()),
                SolarTerms = Enum.GetValues<SolarTerm>().ToDictionary(x => (int)x, x => x.GetLocalizedName())
            };
        }
    }
}
