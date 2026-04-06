using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Serilog;

namespace SKERPAPI.Core.Filters
{
    /// <summary>
    /// 審計日誌過濾器：紀錄 API 呼叫的完整生命週期
    /// </summary>
    public class AuditLogAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var request = actionContext.Request;
            var method = request.Method.Method;
            var url = request.RequestUri.AbsoluteUri;
            var clientIp = GetClientIp(request);

            Log.Information("API CALL START: [{Method}] {Url} from {ClientIp}", method, url, clientIp);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var responseStatus = actionExecutedContext.Response?.StatusCode.ToString() ?? "EXCEPTION";
            Log.Information("API CALL END: Status={Status}", responseStatus);
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
