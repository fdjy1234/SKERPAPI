using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SKERPAPI.Core.Filters;
using SKERPAPI.Core.Interfaces;
using SKERPAPI.Core.Models;
using SKERPAPI.Core.Permissions;
using SKERPAPI.Core.Security;

namespace SKERPAPI.Core.Tests.Filters
{
    [TestClass]
    public class RbacAuthorizeAttributeTests
    {
        // ---------------------------------------------------------------
        // 輔助方法：建立帶有 API Key Header 的 HttpActionContext
        // ---------------------------------------------------------------
        private static HttpActionContext BuildActionContext(
            string apiKey,
            IRbacService rbacService)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");

            if (apiKey != null)
                request.Headers.Add("X-Api-Key", apiKey);

            // 模擬 DI scope（RbacAuthorizeAttribute 透過 GetDependencyScope() 取得 IRbacService）
            var scopeMock = new Mock<IDependencyScope>();
            scopeMock.Setup(s => s.GetService(typeof(IRbacService))).Returns(rbacService);
            request.Properties[HttpPropertyKeys.DependencyScope] = scopeMock.Object;

            var config = new HttpConfiguration();
            var routeData = new HttpRouteData(new HttpRoute());
            var controllerContext = new HttpControllerContext(config, routeData, request);

            // CreateResponse() 需要 request 有關聯的 HttpConfiguration
            request.SetConfiguration(config);

            // RbacAuthorizeAttribute 不存取 ActionDescriptor 的任何成員，使用 Loose mock 即可
            var actionDescriptor = new Mock<HttpActionDescriptor>(MockBehavior.Loose);

            return new HttpActionContext(controllerContext, actionDescriptor.Object);
        }

        // ---------------------------------------------------------------
        // 輔助方法：建立帶有 ClaimsPrincipal 的 HttpActionContext（無 X-Api-Key header）
        // 模擬 OAuth Bearer Token 或 mTLS 認證後的請求情境
        // ---------------------------------------------------------------
        private static HttpActionContext BuildActionContextWithPrincipal(
            string clientId,
            IRbacService rbacService)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
            // 刻意不加 X-Api-Key header，模擬 OAuth / mTLS 用戶端

            var scopeMock = new Mock<IDependencyScope>();
            scopeMock.Setup(s => s.GetService(typeof(IRbacService))).Returns(rbacService);
            request.Properties[HttpPropertyKeys.DependencyScope] = scopeMock.Object;

            var config = new HttpConfiguration();
            var routeData = new HttpRouteData(new HttpRoute());
            var controllerContext = new HttpControllerContext(config, routeData, request);
            request.SetConfiguration(config);

            // 設定 OWIN Layer 1 認證後應存在的 ClaimsPrincipal
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(SecurityConstants.ClientIdClaimType, clientId),
                new Claim(ClaimTypes.Name, clientId)
            }, "TestAuthentication");
            controllerContext.RequestContext.Principal = new ClaimsPrincipal(identity);

            var actionDescriptor = new Mock<HttpActionDescriptor>(MockBehavior.Loose);
            return new HttpActionContext(controllerContext, actionDescriptor.Object);
        }

        private static Task<RbacContext> AuthenticatedWith(params string[] permissions)
        {
            var ctx = new RbacContext
            {
                IsAuthenticated = true,
                ApiKey = "test-key",
                Permissions = new List<string>(permissions)
            };
            return Task.FromResult(ctx);
        }

        // ---------------------------------------------------------------
        // Test 1: 缺少 API Key → 401 Unauthorized
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task ReturnsUnauthorized_WhenApiKeyMissing()
        {
            var rbacMock = new Mock<IRbacService>();
            var attribute = new RbacAuthorizeAttribute { Permission = RbacPermissions.Aoi.StatusRead };
            var ctx = BuildActionContext(apiKey: null, rbacService: rbacMock.Object);

            await attribute.OnActionExecutingAsync(ctx, CancellationToken.None);

            Assert.IsNotNull(ctx.Response);
            Assert.AreEqual(HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
            rbacMock.Verify(s => s.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ---------------------------------------------------------------
        // Test 2: RBAC 回傳 IsAuthenticated = false → 401 Unauthorized
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task ReturnsUnauthorized_WhenRbacContextIsNotAuthenticated()
        {
            var rbacMock = new Mock<IRbacService>();
            rbacMock.Setup(s => s.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RbacContext.Unauthenticated());

            var attribute = new RbacAuthorizeAttribute { Permission = RbacPermissions.Aoi.StatusRead };
            var ctx = BuildActionContext("invalid-key", rbacMock.Object);

            await attribute.OnActionExecutingAsync(ctx, CancellationToken.None);

            Assert.IsNotNull(ctx.Response);
            Assert.AreEqual(HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
        }

        // ---------------------------------------------------------------
        // Test 3: 認證通過但 Permission 不符 → 403 Forbidden
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task ReturnsForbidden_WhenPermissionNotInContext()
        {
            var rbacMock = new Mock<IRbacService>();
            rbacMock.Setup(s => s.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(AuthenticatedWith("car:vehicle:read")); // 有 CAR 權限，但無 AOI

            var attribute = new RbacAuthorizeAttribute { Permission = RbacPermissions.Aoi.InspectionExecute };
            var ctx = BuildActionContext("valid-key", rbacMock.Object);

            await attribute.OnActionExecutingAsync(ctx, CancellationToken.None);

            Assert.IsNotNull(ctx.Response);
            Assert.AreEqual(HttpStatusCode.Forbidden, ctx.Response.StatusCode);
        }

        // ---------------------------------------------------------------
        // Test 4: 認證通過且 Permission 符合 → 允許繼續（response 為 null）
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task AllowsRequest_WhenPermissionPresent()
        {
            var rbacMock = new Mock<IRbacService>();
            rbacMock.Setup(s => s.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(AuthenticatedWith(RbacPermissions.Aoi.InspectionExecute));

            var attribute = new RbacAuthorizeAttribute { Permission = RbacPermissions.Aoi.InspectionExecute };
            var ctx = BuildActionContext("valid-key", rbacMock.Object);

            await attribute.OnActionExecutingAsync(ctx, CancellationToken.None);

            Assert.IsNull(ctx.Response, "Response should be null when request is allowed.");
        }

        // ---------------------------------------------------------------
        // Test 5: RBAC 服務拋出例外 → 503 ServiceUnavailable (Fail-Closed)
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task ReturnsServiceUnavailable_WhenRbacServiceThrows()
        {
            var rbacMock = new Mock<IRbacService>();
            rbacMock.Setup(s => s.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("RBAC service is unavailable."));

            var attribute = new RbacAuthorizeAttribute { Permission = RbacPermissions.Aoi.InspectionExecute };
            var ctx = BuildActionContext("some-key", rbacMock.Object);

            await attribute.OnActionExecutingAsync(ctx, CancellationToken.None);

            Assert.IsNotNull(ctx.Response);
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ctx.Response.StatusCode);
        }

        // ---------------------------------------------------------------
        // Fix-2 TDD — RED Phase
        // 以下三個測試在 Fix-2 實作前 Test 6 & 7 會失敗（返回 401），
        // 實作後所有三個測試均應通過。
        // ---------------------------------------------------------------

        // Test 6: OAuth Bearer Token 認證（ClaimsPrincipal 設定，無 X-Api-Key）→ 允許
        // RED: 目前程式碼只讀 X-Api-Key header → 返回 401 (FAIL)
        // GREEN: Fix-2 優先讀 ClaimsPrincipal.client_id → 通過 RBAC (PASS)
        [TestMethod]
        public async Task AllowsRequest_WhenAuthenticatedViaOAuthBearer_WithoutApiKeyHeader()
        {
            var rbacMock = new Mock<IRbacService>();
            rbacMock.Setup(s => s.ResolveAsync("oauth-client-01", It.IsAny<CancellationToken>()))
                .Returns(AuthenticatedWith(RbacPermissions.Aoi.InspectionExecute));

            var attribute = new RbacAuthorizeAttribute { Permission = RbacPermissions.Aoi.InspectionExecute };
            var ctx = BuildActionContextWithPrincipal("oauth-client-01", rbacMock.Object);

            await attribute.OnActionExecutingAsync(ctx, CancellationToken.None);

            Assert.IsNull(ctx.Response, "OAuth Bearer 用戶端具備有效 RBAC 時應被允許存取。");
        }

        // Test 7: mTLS 憑證認證（ClaimsPrincipal 設定，無 X-Api-Key）→ 允許
        // RED: 目前程式碼只讀 X-Api-Key header → 返回 401 (FAIL)
        // GREEN: Fix-2 優先讀 ClaimsPrincipal.client_id → 通過 RBAC (PASS)
        [TestMethod]
        public async Task AllowsRequest_WhenAuthenticatedViaMtls_WithoutApiKeyHeader()
        {
            var rbacMock = new Mock<IRbacService>();
            rbacMock.Setup(s => s.ResolveAsync("CN=Robot01,O=Corp", It.IsAny<CancellationToken>()))
                .Returns(AuthenticatedWith(RbacPermissions.Aoi.InspectionExecute));

            var attribute = new RbacAuthorizeAttribute { Permission = RbacPermissions.Aoi.InspectionExecute };
            var ctx = BuildActionContextWithPrincipal("CN=Robot01,O=Corp", rbacMock.Object);

            await attribute.OnActionExecutingAsync(ctx, CancellationToken.None);

            Assert.IsNull(ctx.Response, "mTLS 用戶端具備有效 RBAC 時應被允許存取。");
        }

        // Test 8: ClaimsPrincipal 無 client_id 且無 X-Api-Key → 401（迴歸測試）
        // 確保 Fix-2 不會意外放行完全未認證的請求
        [TestMethod]
        public async Task ReturnsUnauthorized_WhenAnonymousPrincipalAndNoApiKeyHeader()
        {
            var rbacMock = new Mock<IRbacService>();
            var attribute = new RbacAuthorizeAttribute { Permission = RbacPermissions.Aoi.StatusRead };

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
            var scopeMock = new Mock<IDependencyScope>();
            scopeMock.Setup(s => s.GetService(typeof(IRbacService))).Returns(rbacMock.Object);
            request.Properties[HttpPropertyKeys.DependencyScope] = scopeMock.Object;

            var config = new HttpConfiguration();
            var controllerContext = new HttpControllerContext(
                config, new HttpRouteData(new HttpRoute()), request);
            request.SetConfiguration(config);

            // 設定匿名身份（無任何 client_id / Name claim）
            controllerContext.RequestContext.Principal =
                new ClaimsPrincipal(new ClaimsIdentity());

            var ctx = new HttpActionContext(
                controllerContext,
                new Mock<HttpActionDescriptor>(MockBehavior.Loose).Object);

            await attribute.OnActionExecutingAsync(ctx, CancellationToken.None);

            Assert.IsNotNull(ctx.Response);
            Assert.AreEqual(HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
            rbacMock.Verify(
                s => s.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
