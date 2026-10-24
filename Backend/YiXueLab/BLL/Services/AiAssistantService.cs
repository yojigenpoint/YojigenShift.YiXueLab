using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.BLL.Services
{
    public class AiAssistantService : IAiAssistantService
    {
        private readonly AiSettings _settings;
        private readonly HttpClient _httpClient;

        // 内置易学高频考点规则库：将标准归档标签映射到多个自然语言触发词（用于未配置外部大模型时的智能提取与兜底）
        private static readonly (string Tag, string[] Triggers)[] KnownYiTagRules =
        {
            ("辰戌冲", new[] { "辰戌冲", "辰戌相冲" }),
            ("子午冲", new[] { "子午冲", "子午相冲" }),
            ("丑未冲", new[] { "丑未冲", "丑未相冲" }),
            ("寅申冲", new[] { "寅申冲", "寅申相冲" }),
            ("卯酉冲", new[] { "卯酉冲", "卯酉相冲" }),
            ("巳亥冲", new[] { "巳亥冲", "巳亥相冲" }),
            ("伤官见官", new[] { "伤官见官" }),
            ("枭神夺食", new[] { "枭神夺食", "偏印夺食" }),
            ("食神制杀", new[] { "食神制杀" }),
            ("官印相生", new[] { "官印相生", "杀印相生" }),
            ("伤官佩印", new[] { "伤官佩印" }),
            ("比劫夺财", new[] { "比劫夺财", "比劫争财" }),
            ("岁运并临", new[] { "岁运并临" }),
            ("用神旬空", new[] { "旬空", "空亡", "出空", "冲空", "填实" }),
            ("月破", new[] { "月破" }),
            ("暗动", new[] { "暗动" }),
            ("独发", new[] { "独发" }),
            ("反吟", new[] { "反吟" }),
            ("伏吟", new[] { "伏吟" }),
            ("体用生克", new[] { "体用", "体克用", "用克体", "用生体", "体生用" }),
            ("报数起卦", new[] { "报数" }),
            ("工作变动", new[] { "工作变动", "换工作", "调动", "平调", "跳槽", "新部门" }),
            ("升职", new[] { "升职", "提拔", "晋升", "升迁" }),
            ("降职", new[] { "降职", "撤职", "记过", "处分" }),
            ("求财", new[] { "求财", "财运", "投资", "破财", "亏损", "赚钱" }),
            ("商业谈判", new[] { "商业谈判", "合同", "谈判", "工程承包", "签约" }),
            ("失物寻物", new[] { "失物", "寻物", "遗失", "丢失", "不见了", "找回" }),
            ("婚恋感情", new[] { "婚姻", "感情", "正缘", "离婚", "结婚", "未婚", "妻子", "男友", "女友" }),
            ("考试升学", new[] { "考试", "升学", "复试", "录取", "考研", "上岸" }),
            ("脾胃之疾", new[] { "脾胃", "胃病", "胃穿孔", "肠胃" }),
            ("疾病手术", new[] { "疾病", "手术", "住院", "开刀", "生病" })
        };

        public AiAssistantService(AiSettings settings, HttpClient httpClient)
        {
            _settings = settings;
            _httpClient = httpClient;
        }

        public async Task<ServiceResult<PolishReadingResponseDto>> PolishReadingAsync(
            PolishReadingRequestDto req,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(req.RawContent) || req.RawContent.Trim().Length < 5)
            {
                return ServiceResult<PolishReadingResponseDto>.Failure("请先输入至少 5 个字的断卦思路草稿，再交由 AI 梳理排版。");
            }

            string raw = req.RawContent.Trim();
            if (raw.Length > 2500)
            {
                return ServiceResult<PolishReadingResponseDto>.Failure("断语草稿过长，请控制在 2500 字以内。");
            }

            // 若配置了真实的 LLM API Key，则优先调用大模型接口
            if (IsApiKeyConfigured())
            {
                var llmText = await TryCallChatCompletionAsync(
                    BuildPolishSystemPrompt(req.DivinationType),
                    $"【盲断考题背景】：{req.QuestionContext ?? "常规盲断"}\n【易友原始断语草稿】：\n{raw}",
                    ct);

                if (!string.IsNullOrWhiteSpace(llmText))
                {
                    return ServiceResult<PolishReadingResponseDto>.Success(
                        new PolishReadingResponseDto(llmText.Trim(), IsRuleFallback: false));
                }
            }

            // 本地结构化梳理兜底（当本地未配置 ApiKey 或网络超时时，自动按门类拆解为三段式标准排版）
            string fallbackText = BuildRuleBasedPolish(req.DivinationType, raw);
            return ServiceResult<PolishReadingResponseDto>.Success(
                new PolishReadingResponseDto(fallbackText, IsRuleFallback: true));
        }

        public async Task<ServiceResult<SuggestTagsResponseDto>> SuggestTagsAsync(
            SuggestTagsRequestDto req,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(req.BackgroundInfo) && string.IsNullOrWhiteSpace(req.QuestionText))
            {
                return ServiceResult<SuggestTagsResponseDto>.Failure("请先填写求测背景或盲断核心问题，再提取标签。");
            }

            string combinedText = $"{req.BackgroundInfo}\n{req.QuestionText}\n{req.RealFeedback}";

            if (IsApiKeyConfigured())
            {
                string sysPrompt =
                    "你是一位专业的中国传统易学（八字/六爻/奇门/梅花）案例归档专家。" +
                    "请根据用户提供的案例背景、问题与反馈，提炼出 3 到 5 个最精准的易学格局技法或事项标签。" +
                    "只返回以英文逗号分隔的标签名（不带#号，每个标签2~5个汉字，例如：辰戌冲,工作变动,脾胃之疾），不要输出任何其他解释文字。";

                var llmTagsRaw = await TryCallChatCompletionAsync(sysPrompt, combinedText, ct);
                if (!string.IsNullOrWhiteSpace(llmTagsRaw))
                {
                    var parsedTags = llmTagsRaw
                        .Split(new[] { ',', '，', '、', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => $"#{t.Trim().TrimStart('#')}")
                        .Where(t => t.Length > 1 && t.Length <= 10)
                        .Distinct()
                        .Take(5)
                        .ToList();

                    if (parsedTags.Count > 0)
                    {
                        return ServiceResult<SuggestTagsResponseDto>.Success(
                            new SuggestTagsResponseDto(parsedTags, IsRuleFallback: false));
                    }
                }
            }

            // 本地同义触发词扫描兜底提取
            var matchedTags = KnownYiTagRules
                .Where(rule => rule.Triggers.Any(t => combinedText.Contains(t, StringComparison.OrdinalIgnoreCase)))
                .Select(rule => $"#{rule.Tag}")
                .Distinct()
                .Take(4)
                .ToList();

            string typeDefaultTag = req.DivinationType switch
            {
                DivinationType.Bazi => "#八字命理",
                DivinationType.LiuYao => "#六爻纳甲",
                DivinationType.QiMen => "#奇门遁甲",
                DivinationType.MeiHua => "#梅花易数",
                _ => "#盲断实证"
            };

            if (!matchedTags.Contains(typeDefaultTag))
            {
                matchedTags.Insert(0, typeDefaultTag);
            }

            if (matchedTags.Count == 1)
            {
                matchedTags.Add("#应期实证");
            }

            return ServiceResult<SuggestTagsResponseDto>.Success(
                new SuggestTagsResponseDto(matchedTags.Take(5).ToList(), IsRuleFallback: true));
        }

        private bool IsApiKeyConfigured()
        {
            return !string.IsNullOrWhiteSpace(_settings.ApiKey)
                && !_settings.ApiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildPolishSystemPrompt(DivinationType type)
        {
            string structureHint = type switch
            {
                DivinationType.Bazi =>
                    "【1. 原局旺衰与格局】\n...\n【2. 岁运引发与应期】\n...\n【3. 综合吉凶结论】\n...",
                DivinationType.LiuYao =>
                    "【1. 用神选取与世应旺衰】\n...\n【2. 动变生克与旬空月破】\n...\n【3. 综合吉凶与应期结论】\n...",
                DivinationType.QiMen =>
                    "【1. 局象与用神落宫】\n...\n【2. 星门神仪生克格局】\n...\n【3. 综合判断结论】\n...",
                DivinationType.MeiHua =>
                    "【1. 本卦体用生克】\n...\n【2. 互卦过程与变卦终局】\n...\n【3. 综合断语结论】\n...",
                _ =>
                    "【1. 盘面核心象义】\n...\n【2. 生克推演过程】\n...\n【3. 综合断语结论】\n..."
            };

            return
                "你是一位严谨、资深的易学练功房教务与文字排版助手。\n" +
                "你的唯一任务是将易友口语化、零散的断卦草稿，润色并梳理为条理清晰的【三段式标准专业断语】。\n" +
                "严格遵守以下铁律：\n" +
                "1. 绝对忠于易友原有的吉凶判断、应期与方向结论，严禁篡改易友的观点或凭空捏造草稿中未提及的断语！\n" +
                "2. 将口语词汇转化为规范、雅致的命理/卜筮术语，保留原有的干支、用神与流年细节。\n" +
                "3. 严格按照以下三段式标题输出纯文本（不要使用额外的 Markdown 星号加粗）：\n" +
                structureHint;
        }

        private static string BuildRuleBasedPolish(DivinationType type, string raw)
        {
            // 若已经是三段式结构，避免重复包裹
            if (raw.Contains("【1.") && raw.Contains("【2."))
            {
                return raw;
            }

            var sentences = Regex.Split(raw, @"(?<=[。！？；\n])")
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var (title1, title2, title3) = type switch
            {
                DivinationType.Bazi => ("【1. 原局旺衰与格局】", "【2. 岁运引发与应期】", "【3. 综合吉凶结论】"),
                DivinationType.LiuYao => ("【1. 用神与世应旺衰】", "【2. 动变生克与旬空】", "【3. 综合吉凶与应期结论】"),
                DivinationType.QiMen => ("【1. 局象与用神落宫】", "【2. 星门神仪生克格局】", "【3. 综合判断结论】"),
                DivinationType.MeiHua => ("【1. 本卦体用生克】", "【2. 互卦过程与变卦终局】", "【3. 综合断语结论】"),
                _ => ("【1. 盘面核心依据】", "【2. 生克推演分析】", "【3. 综合断语结论】")
            };

            if (sentences.Count >= 3)
            {
                string part1 = sentences[0];
                string part2 = string.Join("", sentences.Skip(1).Take(sentences.Count - 2));
                string part3 = sentences.Last();
                return $"{title1}：{part1}\n{title2}：{part2}\n{title3}：{part3}";
            }

            if (sentences.Count == 2)
            {
                return $"{title1}：{sentences[0]}\n{title2}：结合盘面生克与流转综合推演。\n{title3}：{sentences[1]}";
            }

            return $"{title1}：依盘面核心用神与旺衰切入分析。\n{title2}：{raw}\n{title3}：综合上述象义定吉凶应期。";
        }

        private async Task<string?> TryCallChatCompletionAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken ct)
        {
            try
            {
                string endpoint = $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions";
                using var reqMsg = new HttpRequestMessage(HttpMethod.Post, endpoint);
                reqMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

                var payload = new
                {
                    model = _settings.Model,
                    temperature = 0.3,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    }
                };

                reqMsg.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_settings.TimeoutSeconds, 5, 60)));

                using var response = await _httpClient.SendAsync(reqMsg, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync(cts.Token);
                using var doc = JsonDocument.Parse(json);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    return choices[0].GetProperty("message").GetProperty("content").GetString();
                }

                return null;
            }
            catch
            {
                // 网络或第三方 API 异常时静默返回 null，由上层无缝切换为规则引擎兜底
                return null;
            }
        }
    }
}
