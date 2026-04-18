using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin;
using Serilog;

namespace SKERPAPI.Core.Security.Authentication
{
    /// <summary>
    /// API Key 認證 OWIN 中介層。
    /// 從 HTTP Header "X-Api-Key" 讀取金鑰，對比 Web.config 中的金鑰清單進行認證。
    /// 認證成功後設定 ClaimsPrincipal。
    /// </summary>
    /// <remarks>
    /// Web.config 設定範例（多金鑰以逗號分隔，格式 clientId:key）：
    ///   &lt;add key="Security:ApiKeys" value="client-aoi:key123,client-car:key456" /&gt;
    ///
    /// 相容舊 AppSettings：
    ///   &lt;add key="ApiKey" value="single-key" /&gt;  ← 向後相容
    ///
    /// 此中介層取代舊有的 ApiKeyAttribute (ActionFilter)，
    /// 在 OWIN Pipeline 更早期完成認證。
    /// </remarks>
    public class ApiKeyAuthMiddleware : OwinMiddleware
    {
        public ApiKeyAuthMiddleware(OwinMiddleware next) : base(next) { }

        public override async Task Invoke(IOwinContext context)
        {
            // 如果已經有認證身份（例如 JWT 已通過），直接 pass
            if (context.Authentication?.User?.Identity?.IsAuthenticated == true)
            {
                await Next.Invoke(context);
                return;
            }

            var apiKey = context.Request.Headers.Get(SecurityConstants.ApiKeyHeaderName);

            if (!string.IsNullOrEmpty(apiKey))
            {
                var validationResult = ValidateApiKey(apiKey);
                if (validationResult.IsValid)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, validationResult.ClientId),
                        new Claim(SecurityConstants.ClientIdClaimType, validationResult.ClientId),
                        new Claim(ClaimTypes.AuthenticationMethod, SecurityConstants.ApiKeyScheme)
                    };

                    var identity = new ClaimsIdentity(claims, SecurityConstants.ApiKeyAuthenticationType);
                    context.Authentication.User = new ClaimsPrincipal(identity);

                    Log.Debug("ApiKeyAuth: Client {ClientId} authenticated via API Key", validationResult.ClientId);
                }
            }

            // Passive mode：未攜帶或驗證失敗不立即拒絕，交給後續 Authorization 決定
            await Next.Invoke(context);
        }

        /// <summary>
        /// 驗證 API Key。支援多金鑰模式與向後相容單一金鑰模式。
        /// </summary>
        internal static ApiKeyValidationResult ValidateApiKey(string apiKey)
        {
            // 模式 1：多金鑰 (格式 "clientId:key,clientId:key")
            var multiKeys = ConfigurationManager.AppSettings[SecurityConstants.AppSettingsApiKeysKey];
            if (!string.IsNullOrEmpty(multiKeys))
            {
                var pairs = multiKeys.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in pairs)
                {
                    var parts = pair.Split(new[] { ':' }, 2);
                    if (parts.Length == 2 && parts[1].Trim() == apiKey)
                    {
                        return new ApiKeyValidationResult { IsValid = true, ClientId = parts[0].Trim() };
                    }
                }
                return new ApiKeyValidationResult { IsValid = false };
            }

            // 模式 2：向後相容單一金鑰
            var singleKey = ConfigurationManager.AppSettings["ApiKey"];
            if (string.IsNullOrEmpty(singleKey))
            {
                // 未配置金鑰 = 開發環境，視為有效
                return new ApiKeyValidationResult { IsValid = true, ClientId = "dev-client" };
            }

            if (singleKey == apiKey)
            {
                return new ApiKeyValidationResult { IsValid = true, ClientId = "default-client" };
            }

            return new ApiKeyValidationResult { IsValid = false };
        }
    }

    /// <summary>
    /// API Key 驗證結果
    /// </summary>
    public class ApiKeyValidationResult
    {
        public bool IsValid { get; set; }
        public string ClientId { get; set; }
    }
}
