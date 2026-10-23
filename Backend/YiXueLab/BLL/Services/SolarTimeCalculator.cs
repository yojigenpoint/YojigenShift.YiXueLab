using YojigenShift.YiFramework.Core;

namespace YojigenShift.YiXueLab.BLL.Services
{
    /// <summary>
    /// 国内外城市经度匹配与真太阳时/夏令时校准计算器
    /// </summary>
    public static class SolarTimeCalculator
    {
        // 内置国内外常见城市经度与标准时区表 (Longitude: 东经为正, 西经为负; UtcOffset: 标准冬令时区)
        private static readonly (string[] Keywords, double Longitude, double UtcOffset)[] CityCoordinates =
        {
            // --- 中国主要城市 (统一以东八区 UTC+8, 中央经线 120°E 为准) ---
            (new[] { "北京", "Beijing" }, 116.41, 8.0),
            (new[] { "上海", "Shanghai" }, 121.47, 8.0),
            (new[] { "天津", "Tianjin" }, 117.20, 8.0),
            (new[] { "重庆", "Chongqing" }, 106.55, 8.0),
            (new[] { "成都", "四川", "Chengdu", "Sichuan" }, 104.06, 8.0),
            (new[] { "广州", "广东", "Guangzhou", "Guangdong" }, 113.26, 8.0),
            (new[] { "深圳", "Shenzhen" }, 114.06, 8.0),
            (new[] { "杭州", "浙江", "Hangzhou", "Zhejiang" }, 120.16, 8.0),
            (new[] { "南京", "江苏", "Nanjing", "Jiangsu" }, 118.80, 8.0),
            (new[] { "苏州", "Suzhou" }, 120.58, 8.0),
            (new[] { "武汉", "湖北", "Wuhan", "Hubei" }, 114.31, 8.0),
            (new[] { "西安", "陕西", "Xi'an", "Xian", "Shaanxi" }, 108.94, 8.0),
            (new[] { "长沙", "湖南", "Changsha", "Hunan" }, 112.94, 8.0),
            (new[] { "郑州", "河南", "Zhengzhou", "Henan" }, 113.63, 8.0),
            (new[] { "济南", "山东", "Jinan", "Shandong" }, 117.12, 8.0),
            (new[] { "青岛", "Qingdao" }, 120.38, 8.0),
            (new[] { "沈阳", "辽宁", "Shenyang", "Liaoning" }, 123.43, 8.0),
            (new[] { "大连", "Dalian" }, 121.61, 8.0),
            (new[] { "长春", "吉林", "Changchun", "Jilin" }, 125.32, 8.0),
            (new[] { "哈尔滨", "黑龙江", "Harbin", "Heilongjiang" }, 126.53, 8.0),
            (new[] { "福州", "福建", "Fuzhou", "Fujian" }, 119.30, 8.0),
            (new[] { "厦门", "Xiamen" }, 118.09, 8.0),
            (new[] { "台北", "台湾", "Taipei", "Taiwan" }, 121.56, 8.0),
            (new[] { "香港", "Hong Kong", "HK" }, 114.17, 8.0),
            (new[] { "澳门", "Macau", "Macao" }, 113.54, 8.0),
            (new[] { "昆明", "云南", "Kunming", "Yunnan" }, 102.83, 8.0),
            (new[] { "贵阳", "贵州", "Guiyang", "Guizhou" }, 106.63, 8.0),
            (new[] { "南宁", "广西", "Nanning", "Guangxi" }, 108.37, 8.0),
            (new[] { "海口", "海南", "Haikou", "Hainan" }, 110.20, 8.0),
            (new[] { "南昌", "江西", "Nanchang", "Jiangxi" }, 115.86, 8.0),
            (new[] { "合肥", "安徽", "Hefei", "Anhui" }, 117.23, 8.0),
            (new[] { "太原", "山西", "Taiyuan", "Shanxi" }, 112.55, 8.0),
            (new[] { "石家庄", "河北", "Shijiazhuang", "Hebei" }, 114.51, 8.0),
            (new[] { "呼和浩特", "内蒙古", "Hohhot" }, 111.75, 8.0),
            (new[] { "兰州", "甘肃", "Lanzhou", "Gansu" }, 103.83, 8.0),
            (new[] { "银川", "宁夏", "Yinchuan", "Ningxia" }, 106.23, 8.0),
            (new[] { "西宁", "青海", "Xining", "Qinghai" }, 101.78, 8.0),
            (new[] { "乌鲁木齐", "新疆", "Urumqi", "Xinjiang" }, 87.62, 8.0),
            (new[] { "拉萨", "西藏", "Lhasa", "Tibet" }, 91.14, 8.0),

            // --- 海外主要城市 (北美、欧洲、亚太、澳洲) ---
            (new[] { "纽约", "New York", "NYC", "NY" }, -74.01, -5.0),
            (new[] { "波士顿", "Boston" }, -71.06, -5.0),
            (new[] { "华盛顿", "Washington" }, -77.04, -5.0),
            (new[] { "亚特兰大", "Atlanta" }, -84.39, -5.0),
            (new[] { "迈阿密", "Miami" }, -80.19, -5.0),
            (new[] { "多伦多", "Toronto" }, -79.38, -5.0),
            (new[] { "芝加哥", "Chicago" }, -87.63, -6.0),
            (new[] { "休斯顿", "Houston" }, -95.37, -6.0),
            (new[] { "达拉斯", "Dallas" }, -96.80, -6.0),
            (new[] { "丹佛", "Denver" }, -104.99, -7.0),
            (new[] { "洛杉矶", "Los Angeles", "LA" }, -118.24, -8.0),
            (new[] { "旧金山", "三藩", "San Francisco", "SF", "圣荷西", "San Jose", "硅谷" }, -122.42, -8.0),
            (new[] { "西雅图", "Seattle" }, -122.33, -8.0),
            (new[] { "温哥华", "Vancouver" }, -123.12, -8.0),
            (new[] { "伦敦", "英国", "London", "UK" }, -0.13, 0.0),
            (new[] { "巴黎", "法国", "Paris", "France" }, 2.35, 1.0),
            (new[] { "柏林", "法兰克福", "德国", "Berlin", "Frankfurt", "Germany" }, 13.40, 1.0),
            (new[] { "东京", "日本", "Tokyo", "Japan", "大阪", "Osaka" }, 139.69, 9.0),
            (new[] { "首尔", "韩国", "Seoul", "Korea" }, 126.98, 9.0),
            (new[] { "新加坡", "Singapore" }, 103.82, 8.0),
            (new[] { "吉隆坡", "马来西亚", "Kuala Lumpur", "Malaysia" }, 101.69, 8.0),
            (new[] { "曼谷", "泰国", "Bangkok", "Thailand" }, 100.50, 7.0),
            (new[] { "悉尼", "墨尔本", "澳大利亚", "Sydney", "Melbourne", "Australia" }, 151.21, 10.0),
            (new[] { "奥克兰", "新西兰", "Auckland", "New Zealand" }, 174.76, 12.0)
        };

        /// <summary>
        /// 根据地点名称或显式传入的经度，计算夏令时校准并直接调用 YiFramework.Core.SolarTimeHelper 换算真太阳时
        /// </summary>
        public static (DateTime EffectiveTimeUtc, double? ResolvedLongitude) CalculateEffectiveTime(
            DateTime rawClockTime,
            string? location,
            double? explicitLongitude,
            double? explicitUtcOffset,
            bool isTrueSolarTime,
            bool isDst)
        {
            // 1. 解析经度与当地标准冬令时区 (默认东八区 UTC+8)
            double? lon = explicitLongitude;
            double utcOffset = explicitUtcOffset ?? 8.0;

            if (!string.IsNullOrWhiteSpace(location))
            {
                foreach (var entry in CityCoordinates)
                {
                    if (entry.Keywords.Any(k => location.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    {
                        lon ??= entry.Longitude;
                        if (!explicitUtcOffset.HasValue)
                        {
                            utcOffset = entry.UtcOffset;
                        }
                        break;
                    }
                }
            }

            // 2. 夏令时校准：若勾选夏令时，先将当地钟表时间拨慢 1 小时还原为当地标准时间
            DateTime localStandardTime = isDst ? rawClockTime.AddHours(-1) : rawClockTime;

            // 3. 若启用真太阳时且拥有经度，转化为绝对宇宙时间 (UTC) 后直接调用 YiFramework.Core.SolarTimeHelper
            if (isTrueSolarTime && lon.HasValue)
            {
                DateTime actualUtc = localStandardTime.AddHours(-utcOffset);
                DateTime trueSolarTime = SolarTimeHelper.CalculateTrueSolarTimeFromUtc(actualUtc, lon.Value);
                return (DateTime.SpecifyKind(trueSolarTime, DateTimeKind.Utc), lon);
            }

            return (DateTime.SpecifyKind(localStandardTime, DateTimeKind.Utc), lon);
        }
    }
}
