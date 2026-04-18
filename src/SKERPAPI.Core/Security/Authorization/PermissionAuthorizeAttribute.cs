using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Controllers;
using Serilog;

namespace SKERPAPI.Core.Security.Authorization
{
    /// <summary>
    /// 權限授權 Attribute。
    /// 宣告式授權，檢查當前使用者是否具有指定的權限代碼。
    /// 內部透過 Autofac 解析 IAuthorizationProvider 進行授權檢查。
    /// </summary>
    /// <remarks>
    /// 使用方式：
    ///   [PermissionAuthorize("aoi:workorder:create")]
    ///   public IHttpActionResult CreateOrder(...) { ... }
    ///
    /// 權限代碼命名慣例：
    ///   "{module}:{resource}:{action}"
    ///   例如：aoi:workorder:read, car:vehicle:delete, admin:config:write
    ///
    /// 行為：
    ///   1. 未認證 → 401 Unauthorized
    ///   2. 已認證但未授權 → 403 Forbidden
    ///   3. 無 IAuthorizationProvider 註冊 → 僅檢查是否已認證（Fallback）
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class PermissionAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// 所需的權限代碼
        /// </summary>
        public string Permission { get; }

        public PermissionAuthorizeAttribute(string permission)
        {
            Permission = permission;
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;
            if (principal == null || !principal.Identity.IsAuthenticated)
                return false;

            // 從 DI 容器取得 IAuthorizationProvider
            var dependencyScope = actionContext.Request.GetDependencyScope();
            var provider = dependencyScope?.GetService(typeof(IAuthorizationProvider)) as IAuthorizationProvider;

            if (provider == null)
            {
                // 未註冊 Provider = Fallback，僅檢查是否已認證
                Log.Warning("PermissionAuthorize: No IAuthorizationProvider registered. Falling back to authentication-only check for permission '{Permission}'",
                    Permission);
                return true;
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? principal.FindFirst(ClaimTypes.Name)?.Value
                      ?? principal.Identity.Name;

            if (string.IsNullOrEmpty(userId))
            {
                Log.Warning("PermissionAuthorize: Cannot determine userId from ClaimsPrincipal");
                return false;
            }

            var hasPermission = provider.HasPermission(userId, Permission);

            if (!hasPermission)
            {
                Log.Warning("PermissionAuthorize: User {UserId} denied permission '{Permission}'", userId, Permission);
            }

            return hasPermission;
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            var principal = actionContext.RequestContext.Principal as ClaimsPrincipal;
            if (principal != null && principal.Identity.IsAuthenticated)
            {
                // 已認證但無權限 → 403 Forbidden
                var response = new Models.ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = $"Forbidden. Permission '{Permission}' is required."
                };
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Forbidden, response);
            }
            else
            {
                // 未認證 → 401 Unauthorized
                var response = new Models.ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Authentication required."
                };
                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Unauthorized, response);
            }
        }
    }
}
