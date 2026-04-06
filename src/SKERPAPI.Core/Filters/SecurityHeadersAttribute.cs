using System.Web.Http.Filters;

namespace SKERPAPI.Core.Filters
{
    /// <summary>
    /// 安全標頭過濾器：設定防禦性 HTTP 回應標頭
    /// </summary>
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Response != null)
            {
                var headers = actionExecutedContext.Response.Headers;
                if (!headers.Contains("X-Content-Type-Options")) headers.Add("X-Content-Type-Options", "nosniff");
                if (!headers.Contains("X-Frame-Options")) headers.Add("X-Frame-Options", "DENY");
                if (!headers.Contains("X-XSS-Protection")) headers.Add("X-XSS-Protection", "1; mode=block");
                if (!headers.Contains("Referrer-Policy")) headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            }
        }
    }
}
