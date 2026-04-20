using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SKERPAPI.Core.Filters
{
    /// <summary>
    /// API 金鑰驗證過濾器。
    /// 從 Request Header "X-Api-Key" 讀取金鑰並驗證。
    /// </summary>
    /// <remarks>
    /// 使用方式：套用在 Controller 或 Action 上
    ///   [ApiKey]
    ///   public class MyController
    /// 
    /// 金鑰設定在 Web.config 的 appSettings：
    ///   &lt;add key="ApiKey" value="your-secret-key" /&gt;
    /// </remarks>
    [System.Obsolete("ApiKeyAttribute 已由 RbacAuthorizeAttribute 取代，請改用 [RbacAuthorize(Permission = ...)]。")]
    public class ApiKeyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            string apiKey = null;

            if (actionContext.Request.Headers.TryGetValues("X-Api-Key", out IEnumerable<string> headerValues))
            {
                apiKey = headerValues.FirstOrDefault();
            }

            var configuredKey = ConfigurationManager.AppSettings["ApiKey"];

            // 如果未配置金鑰，則跳過驗證 (開發環境)
            if (string.IsNullOrEmpty(configuredKey))
            {
                base.OnActionExecuting(actionContext);
                return;
            }

            if (string.IsNullOrEmpty(apiKey) || apiKey != configuredKey)
            {
                var response = new Models.ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Invalid or missing API key."
                };
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Unauthorized, response);
                return;
            }

            base.OnActionExecuting(actionContext);
        }
    }
}
