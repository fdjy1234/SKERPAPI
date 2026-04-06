using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Serilog;

namespace SKERPAPI.Core.Filters
{
    /// <summary>
    /// 全域例外攔截過濾器。
    /// 捕獲未處理的例外，記錄至 Serilog，並回傳統一的 ApiResponse 格式。
    /// </summary>
    /// <remarks>
    /// 在 DEBUG 模式下會回傳完整的例外訊息，
    /// 在 RELEASE 模式下只回傳通用錯誤訊息。
    /// </remarks>
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            var exception = actionExecutedContext.Exception;
            string message = "An unexpected error occurred.";

#if DEBUG
            message = exception.Message;
#endif
            Log.Error(exception, "Unhandled Exception at {Action}",
                      actionExecutedContext.ActionContext.ActionDescriptor.ActionName);

            var responseModel = new Models.ApiResponse<object>
            {
                Success = false,
                ErrorMessage = message,
                TraceId = Guid.NewGuid().ToString("N").Substring(0, 16)
            };

            actionExecutedContext.Response = actionExecutedContext.Request.CreateResponse(
                HttpStatusCode.InternalServerError, responseModel);
        }
    }
}
