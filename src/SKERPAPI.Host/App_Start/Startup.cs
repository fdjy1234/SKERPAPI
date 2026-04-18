using Microsoft.Owin;
using Owin;
using System.Web.Http;
using Serilog;
using SKERPAPI.Core.Security.Authentication;
using SKERPAPI.Core.Security.Cors;

[assembly: OwinStartup(typeof(SKERPAPI.Host.Startup))]

namespace SKERPAPI.Host
{
    /// <summary>
    /// OWIN 啟動配置。
    /// 定義完整的中介層管線：CORS → mTLS → OAuth → API Key → Auth Required → Web API。
    /// </summary>
    /// <remarks>
    /// 管線執行順序（由外到內）：
    ///   1. CORS Middleware (處理 preflight OPTIONS)
    ///   2. ClientCertificate Middleware (mTLS，被動式)
    ///   3. OAuth2 Authorization Server (自建 Token 端點 /api/token)
    ///   4. OAuth Bearer Token Validation (驗證 Bearer Token)
    ///   5. API Key Middleware (被動式)
    ///   6. Authentication Required Middleware (閘門：至少一種認證必須通過)
    ///   7. Web API Pipeline (Routing, Filters, Controllers)
    /// </remarks>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 0. 配置 Serilog（必須最先）
            SerilogConfig.Configure();

            Log.Information("SKERPAPI OWIN Startup initializing...");

            // 1. CORS Middleware (Layer 1 - OWIN 全域 preflight)
            CorsConfig.ConfigureOwinCors(app);

            // 2. mTLS Client Certificate Authentication (被動式)
            app.Use<ClientCertificateAuthMiddleware>();

            // 3. OAuth2 Authorization Server (自建 Token 端點)
            OAuthServerConfig.ConfigureOAuthServer(app);

            // 4. API Key Authentication (被動式)
            app.Use<ApiKeyAuthMiddleware>();

            // 5. Authentication Required (閘門)
            app.Use<AuthenticationRequiredMiddleware>();

            // 6. Web API Pipeline
            var config = new HttpConfiguration();
            WebApiConfig.Register(config);
            app.UseWebApi(config);

            Log.Information("SKERPAPI OWIN Startup completed. Pipeline: CORS → mTLS → OAuth → ApiKey → AuthRequired → WebAPI");
        }
    }
}
