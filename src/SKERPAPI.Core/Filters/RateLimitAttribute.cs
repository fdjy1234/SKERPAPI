using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SKERPAPI.Core.Filters
{
    /// <summary>
    /// API 限速過濾器，基於 MemoryCache 實作滑動視窗限速。
    /// </summary>
    /// <remarks>
    /// 使用方式：
    ///   [RateLimit(MaxRequestsPerMinute = 100)]
    ///   public class MyController
    /// 
    /// 限速依據：Client IP + Controller 名稱
    /// </remarks>
    public class RateLimitAttribute : ActionFilterAttribute
    {
        public int MaxRequestsPerMinute { get; set; } = 60;

        public RateLimitAttribute() { }

        public RateLimitAttribute(int maxRequestsPerMinute)
        {
            MaxRequestsPerMinute = maxRequestsPerMinute;
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var clientIp = GetClientIp(actionContext.Request);
            var controllerName = actionContext.ControllerContext.ControllerDescriptor.ControllerName;
            var cacheKey = $"RateLimit_{clientIp}_{controllerName}";

            var cache = MemoryCache.Default;
            var requestCount = cache.Get(cacheKey) as int? ?? 0;

            if (requestCount >= MaxRequestsPerMinute)
            {
                var response = new Models.ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Rate limit exceeded. Max {MaxRequestsPerMinute} requests per minute."
                };
                actionContext.Response = actionContext.Request.CreateResponse(
                    (HttpStatusCode)429, response);
                return;
            }

            cache.Set(cacheKey, requestCount + 1,
                      new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(1) });

            base.OnActionExecuting(actionContext);
        }

        private static string GetClientIp(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            return "UNKNOWN";
        }
    }
}
