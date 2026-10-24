namespace YojigenShift.YiXueLab.BLL.Models
{
    /// <summary>
    /// 对应 appsettings.json 中的 "AiSettings" 节点（兼容所有 OpenAI 格式接口：OpenAI / DeepSeek / OpenRouter / Ollama）
    /// </summary>
    public class AiSettings
    {
        public const string SectionName = "AiSettings";
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        public int TimeoutSeconds { get; set; } = 25;
    }
}
