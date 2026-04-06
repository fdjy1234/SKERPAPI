using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SKERPAPI.Core.Filters
{
    /// <summary>
    /// 自動 ModelState 驗證過濾器。
    /// 當 ModelState 無效時，自動回傳 400 BadRequest 並列出驗證錯誤。
    /// </summary>
    public class ModelValidationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (!actionContext.ModelState.IsValid)
            {
                var errors = actionContext.ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .ToDictionary(
                        ms => ms.Key,
                        ms => ms.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var response = new Models.ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Validation failed.",
                    Data = errors
                };

                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.BadRequest, response);
            }

            base.OnActionExecuting(actionContext);
        }
    }
}
