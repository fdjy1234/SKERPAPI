using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using SKERPAPI.Core.Models;

namespace SKERPAPI.Core.Controllers
{
    /// <summary>
    /// API 控制器基底類別。
    /// 提供統一的回應格式、驗證邏輯，以及跨模組共用的安全 Attribute。
    /// 所有模組的 Controller 應繼承此類別。
    /// </summary>
    /// <remarks>
    /// 已套用的 Attribute：
    ///   - RateLimit：每分鐘 100 次限速
    ///   - AuditLog：操作審計日誌
    ///   - SecurityHeaders：安全回應標頭
    ///
    /// 認證由 OWIN Pipeline Middleware 處理 (ApiKeyAuth / JwtBearer / mTLS)。
    /// 授權可在 Controller/Action 上加入 [PermissionAuthorize("xxx")] 或 [RbacAuthorize(Permission = ...)] Attribute。
    /// </remarks>
    [Filters.RateLimit(MaxRequestsPerMinute = 100)]
    [Filters.AuditLog]
    [Filters.SecurityHeaders]
    public abstract class ApiBaseController : ApiController
    {
        /// <summary>
        /// 統一驗證 ModelState，子類別可覆寫添加額外驗證邏輯
        /// </summary>
        protected virtual bool ValidateRequest()
        {
            return ModelState.IsValid;
        }

        /// <summary>
        /// 封裝成功的 API 回應 (HTTP 200)
        /// </summary>
        protected IHttpActionResult ApiOk<T>(T data)
        {
            var response = new ApiResponse<T>(data)
            {
                TraceId = Guid.NewGuid().ToString("N").Substring(0, 16)
            };
            return Ok(response);
        }

        /// <summary>
        /// 封裝成功的分頁回應 (HTTP 200)
        /// </summary>
        protected IHttpActionResult ApiPagedOk<T>(IEnumerable<T> items, int totalCount, int page, int pageSize)
        {
            var pagedResult = new PagedResult<T>
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
            return ApiOk(pagedResult);
        }

        /// <summary>
        /// 封裝失敗的 API 回應
        /// </summary>
        protected IHttpActionResult ApiFail(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
        {
            var response = ApiResponse<object>.Fail(message);
            return Content(status, response);
        }

        /// <summary>
        /// 封裝 404 NotFound 回應
        /// </summary>
        protected IHttpActionResult ApiNotFound(string message = "Resource not found.")
        {
            return ApiFail(message, HttpStatusCode.NotFound);
        }
    }
}
