using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Serilog;

namespace SKERPAPI.Core.Security.Authentication
{
    /// <summary>
    /// 認證需求強制中介層。
    /// 置於所有認證中介層之後，檢查是否至少有一種認證方式通過。
    /// 對於未認證的請求回傳 401 Unauthorized。
    /// </summary>
    /// <remarks>
    /// 可透過路徑白名單排除不需認證的端點（如 /api/token, /swagger）。
    /// </remarks>
    public class AuthenticationRequiredMiddleware : OwinMiddleware
    {
        private static readonly string[] _publicPaths = new[]
        {
            "/api/token",        // OAuth2 Token 端點
            "/swagger",          // Swagger UI
            "/api/health"        // 健康檢查
        };

        public AuthenticationRequiredMiddleware(OwinMiddleware next) : base(next) { }

        public override async Task Invoke(IOwinContext context)
        {
            // 跳過 OPTIONS preflight
            if (string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await Next.Invoke(context);
                return;
            }

            // 跳過公開路徑
            var path = context.Request.Path.Value ?? "";
            foreach (var publicPath in _publicPaths)
            {
                if (path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase))
                {
                    await Next.Invoke(context);
                    return;
                }
            }

            // 檢查是否已認證
            if (context.Authentication?.User?.Identity?.IsAuthenticated != true)
            {
                Log.Warning("AuthRequired: Unauthenticated request to {Path} from {IP}",
                    path, context.Request.RemoteIpAddress);

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"success\":false,\"errorMessage\":\"Authentication required. Provide a valid API Key, Bearer token, or client certificate.\"}");
                return;
            }

            await Next.Invoke(context);
        }
    }
}
