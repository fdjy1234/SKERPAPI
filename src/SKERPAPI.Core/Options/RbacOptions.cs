namespace SKERPAPI.Core.Options
{
    /// <summary>
    /// RBAC 服務設定選項。
    /// 從 Web.config appSettings 讀取並透過 DI 注入。
    /// </summary>
    public sealed class RbacOptions
    {
        /// <summary>內部 RBAC REST API 基底 URL（例：http://internal-rbac-api:8080）</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>查詢逾時秒數（預設 10 秒）</summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>授權上下文快取分鐘數（預設 5 分鐘）</summary>
        public int CacheTtlMinutes { get; set; } = 5;
    }
}
