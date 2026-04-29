using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Serilog;
using SKERPAPI.Core.Interfaces;
using SKERPAPI.Core.Models;
using SKERPAPI.Core.Security;

namespace SKERPAPI.Core.Filters
{
    /// <summary>
    /// RBAC 動態授權過濾器（Layer 2）。
    /// 優先讀取 OWIN Layer 1 設定的 ClaimsPrincipal（支援 OAuth 2.0 Bearer Token 及 mTLS），
    /// 向後相容則回退讀取 X-Api-Key Header；呼叫 IRbacService 解析角色與權限，
    /// 依 Permission 屬性決定是否允許存取。
    /// </summary>
    /// <remarks>
    /// 使用方式（Action 層級）：
    ///   [RbacAuthorize(Permission = RbacPermissions.Aoi.InspectionExecute)]
    ///   public IHttpActionResult Inspect([FromBody] AOIInspectionRequest request) { ... }
    ///
    /// 身份識別優先順序：
    ///   1. ClaimsPrincipal.FindFirst("client_id") — OAuth Bearer Token / mTLS 用戶端
    ///   2. ClaimsPrincipal.FindFirst(ClaimTypes.Name) — 備用 Name claim
    ///   3. X-Api-Key Header — 舊版 API Key 向後相容
    ///
    /// 失敗策略（Fail-Closed）：
    ///   - RBAC API 無法連線 → 503 Service Unavailable
    ///   - 無有效身份識別      → 401 Unauthorized
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
            // ── 身份識別解析：優先 ClaimsPrincipal（Layer 1），回退 X-Api-Key Header ──
            // 1. 嘗試從 OWIN 中間件設定的 ClaimsPrincipal 取得 client_id
            var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;
            var clientId = principal?.FindFirst(SecurityConstants.ClientIdClaimType)?.Value
                        ?? principal?.FindFirst(ClaimTypes.Name)?.Value;

            // 2. Fallback：讀取 X-Api-Key Header（向後相容舊版 API Key 用戶端）
            if (string.IsNullOrEmpty(clientId))
            {
                if (actionContext.Request.Headers.TryGetValues(
                    SecurityConstants.ApiKeyHeaderName, out IEnumerable<string> headerValues))
                    clientId = headerValues.FirstOrDefault();
            }

            if (string.IsNullOrEmpty(clientId))
            {
                SetResponse(actionContext, HttpStatusCode.Unauthorized, "Missing authentication.");
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
                context = await rbacService.ResolveAsync(clientId, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RBAC service call failed for client {MaskedClientId}.", MaskKey(clientId));
                SetResponse(actionContext, HttpStatusCode.ServiceUnavailable,
                    "Authorization service unavailable.");
                return;
            }

            if (!context.IsAuthenticated)
            {
                Log.Warning("Unauthorized access attempt by client {MaskedClientId}.", MaskKey(clientId));
                SetResponse(actionContext, HttpStatusCode.Unauthorized, "Unauthorized.");
                return;
            }

            if (!string.IsNullOrEmpty(Permission) && !context.HasPermission(Permission))
            {
                Log.Warning(
                    "Access denied. Client {MaskedClientId} lacks permission {Permission}.",
                    MaskKey(clientId), Permission);
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
