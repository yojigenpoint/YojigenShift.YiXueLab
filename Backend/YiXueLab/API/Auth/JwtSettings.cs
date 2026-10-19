namespace YojigenShift.YiXueLab.API.Auth
{
    /// <summary>
    /// 对应 appsettings.json 中的 "JwtSettings" 节点
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";
        public string SecretKey { get; set; } = "YiXueLab_SuperSecretKey_MustBeAtLeast32BytesLong_2026!";
        public string Issuer { get; set; } = "YiXueLab.API";
        public string Audience { get; set; } = "YiXueLab.WebClient";
        public int ExpiryDays { get; set; } = 30; // 内部练功房默认 30 天免登录，提升群友移动端体验
    }
}
