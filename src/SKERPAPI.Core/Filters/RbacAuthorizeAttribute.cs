using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Serilog;
using SKERPAPI.Core.Interfaces;
using SKERPAPI.Core.Models;

namespace SKERPAPI.Core.Filters
{
    /// <summary>
    /// RBAC 動態授權過濾器。
    /// 讀取 X-Api-Key Header，呼叫 IRbacService 解析角色與權限，
    /// 依 Permission 屬性決定是否允許存取。
    /// </summary>
    /// <remarks>
    /// 使用方式（Action 層級）：
    ///   [RbacAuthorize(Permission = RbacPermissions.Aoi.InspectionExecute)]
    ///   public IHttpActionResult Inspect([FromBody] AOIInspectionRequest request) { ... }
    ///
    /// 失敗策略（Fail-Closed）：
    ///   - RBAC API 無法連線 → 503 Service Unavailable
    ///   - API Key 不存在/無效 → 401 Unauthorized
    ///   - 無對應 Permission    → 403 Forbidden
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class RbacAuthorizeAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 此端點所需的 Permission（格式：module:resource:action）。
        /// 建議使用 <see cref="Permissions.RbacPermissions"/> 中的常量。
        /// </summary>
        public string Permission { get; set; }

        /// <inheritdoc />
        public override async Task OnActionExecutingAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken)
        {
            string apiKey = null;

            if (actionContext.Request.Headers.TryGetValues("X-Api-Key", out IEnumerable<string> headerValues))
                apiKey = headerValues.FirstOrDefault();

            if (string.IsNullOrEmpty(apiKey))
            {
                SetResponse(actionContext, HttpStatusCode.Unauthorized, "Missing API key.");
                return;
            }

            IRbacService rbacService;
            try
            {
                rbacService = actionContext.Request
                    .GetDependencyScope()
                    .GetService(typeof(IRbacService)) as IRbacService;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to resolve IRbacService from DI container.");
                SetResponse(actionContext, HttpStatusCode.ServiceUnavailable,
                    "Authorization service configuration error.");
                return;
            }

            if (rbacService == null)
            {
                Log.Error("IRbacService is not registered in the DI container.");
                SetResponse(actionContext, HttpStatusCode.ServiceUnavailable,
                    "Authorization service not configured.");
                return;
            }

            RbacContext context;
            try
            {
                context = await rbacService.ResolveAsync(apiKey, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RBAC service call failed for key {MaskedKey}.", MaskKey(apiKey));
                SetResponse(actionContext, HttpStatusCode.ServiceUnavailable,
                    "Authorization service unavailable.");
                return;
            }

            if (!context.IsAuthenticated)
            {
                Log.Warning("Unauthorized access attempt with key {MaskedKey}.", MaskKey(apiKey));
                SetResponse(actionContext, HttpStatusCode.Unauthorized, "Unauthorized.");
                return;
            }

            if (!string.IsNullOrEmpty(Permission) && !context.HasPermission(Permission))
            {
                Log.Warning(
                    "Access denied. Key {MaskedKey} lacks permission {Permission}.",
                    MaskKey(apiKey), Permission);
                SetResponse(actionContext, HttpStatusCode.Forbidden,
                    $"Forbidden. Required permission: {Permission}");
                return;
            }

            await base.OnActionExecutingAsync(actionContext, cancellationToken).ConfigureAwait(false);
        }

        private static void SetResponse(HttpActionContext ctx, HttpStatusCode status, string message)
        {
            var body = new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = message,
                TraceId = Guid.NewGuid().ToString("N").Substring(0, 16)
            };
            ctx.Response = ctx.Request.CreateResponse(status, body);
        }

        private static string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length <= 8) return "***";
            return key.Substring(0, 4) + "****" + key.Substring(key.Length - 4);
        }
    }
}
