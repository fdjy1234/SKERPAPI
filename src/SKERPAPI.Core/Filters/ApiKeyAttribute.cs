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
    /// [已過時] API 金鑰驗證過濾器。
    /// 認證已遷移至 OWIN Pipeline 的 ApiKeyAuthMiddleware。
    /// 此類別保留供向後相容，新程式碼請勿使用。
    /// </summary>
    /// <remarks>
    /// 已被 SKERPAPI.Core.Security.Authentication.ApiKeyAuthMiddleware 取代。
    /// </remarks>
    [System.Obsolete("ApiKeyAttribute 已過時。認證請改用 OWIN Pipeline 的 ApiKeyAuthMiddleware；授權請改用 [RbacAuthorize(Permission = ...)] 或 [PermissionAuthorize]。此 Filter 將在未來版本移除。")]
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
