using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http.Cors;
using Microsoft.Owin.Cors;
using Owin;

namespace SKERPAPI.Core.Security.Cors
{
    /// <summary>
    /// CORS 配置模型與 OWIN 擴展方法。
    /// 支援從 Web.config AppSettings 動態載入允許的 Origin 白名單。
    /// </summary>
    /// <remarks>
    /// 雙層 CORS 策略：
    ///   Layer 1 - OWIN Middleware (處理 preflight OPTIONS 請求)
    ///   Layer 2 - Web API [EnableCors] Attribute (Controller/Action 精細控制)
    ///
    /// Web.config 設定範例：
    ///   &lt;add key="Cors:AllowedOrigins" value="https://erp.company.com,https://mes.company.com" /&gt;
    ///   &lt;add key="Cors:AllowCredentials" value="true" /&gt;
    ///   &lt;add key="Cors:MaxAge" value="3600" /&gt;
    /// </remarks>
    public static class CorsConfig
    {
        /// <summary>
        /// 在 OWIN Pipeline 啟用全域 CORS (Layer 1)。
        /// 處理 preflight OPTIONS 請求，並從 Web.config 讀取白名單。
        /// </summary>
        public static void ConfigureOwinCors(IAppBuilder app)
        {
            var corsOptions = new CorsOptions
            {
                PolicyProvider = new Microsoft.Owin.Cors.CorsPolicyProvider
                {
                    PolicyResolver = context =>
                    {
                        var policy = new System.Web.Cors.CorsPolicy
                        {
                            AllowAnyHeader = true,
                            AllowAnyMethod = true,
                            SupportsCredentials = GetAllowCredentials()
                        };

                        var origins = GetAllowedOrigins();
                        if (origins != null && origins.Length > 0)
                        {
                            foreach (var origin in origins)
                            {
                                var trimmed = origin.Trim();
                                if (!string.IsNullOrEmpty(trimmed))
                                {
                                    policy.Origins.Add(trimmed);
                                }
                            }
                        }
                        else
                        {
                            // 未設定時預設允許所有 (開發環境)
                            policy.AllowAnyOrigin = true;
                        }

                        var maxAge = GetMaxAge();
                        if (maxAge > 0)
                        {
                            policy.PreflightMaxAge = maxAge;
                        }

                        return Task.FromResult(policy);
                    }
                }
            };

            app.UseCors(corsOptions);

            Serilog.Log.Information("CORS configured via OWIN middleware. AllowedOrigins={Origins}",
                ConfigurationManager.AppSettings[SecurityConstants.AppSettingsCorsOriginsKey] ?? "*");
        }

        /// <summary>
        /// 在 Web API HttpConfiguration 啟用 CORS (Layer 2)。
        /// 允許 Controller/Action 層級使用 [EnableCors] 和 [DisableCors]。
        /// </summary>
        public static void EnableWebApiCors(System.Web.Http.HttpConfiguration config)
        {
            System.Web.Http.CorsHttpConfigurationExtensions.EnableCors(config);
        }

        /// <summary>
        /// 取得允許的 Origin 白名單（從 Web.config）
        /// </summary>
        public static string[] GetAllowedOrigins()
        {
            var originsStr = ConfigurationManager.AppSettings[SecurityConstants.AppSettingsCorsOriginsKey];
            if (string.IsNullOrEmpty(originsStr))
                return null;
            return originsStr.Split(',');
        }

        /// <summary>
        /// 取得是否允許 Credentials（從 Web.config）
        /// </summary>
        public static bool GetAllowCredentials()
        {
            var val = ConfigurationManager.AppSettings[SecurityConstants.AppSettingsCorsCredentialsKey];
            return !string.IsNullOrEmpty(val) && bool.TryParse(val, out bool result) && result;
        }

        /// <summary>
        /// 取得 Preflight MaxAge（從 Web.config）
        /// </summary>
        public static long GetMaxAge()
        {
            var val = ConfigurationManager.AppSettings[SecurityConstants.AppSettingsCorsMaxAgeKey];
            if (!string.IsNullOrEmpty(val) && long.TryParse(val, out long result))
                return result;
            return 3600; // 預設 1 小時
        }
    }
}
