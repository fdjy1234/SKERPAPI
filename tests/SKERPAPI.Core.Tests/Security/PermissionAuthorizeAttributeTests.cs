using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SKERPAPI.Core.Security.Authorization;

namespace SKERPAPI.Core.Tests.Security
{
    /// <summary>
    /// PermissionAuthorizeAttribute 單元測試。
    /// 測試宣告式授權邏輯。
    /// </summary>
    [TestClass]
    public class PermissionAuthorizeAttributeTests
    {
        [TestMethod]
        public void Permission_Property_IsSetCorrectly()
        {
            // Arrange & Act
            var attr = new PermissionAuthorizeAttribute("aoi:workorder:create");

            // Assert
            Assert.AreEqual("aoi:workorder:create", attr.Permission);
        }

        [TestMethod]
        public void IsAuthorized_NoProvider_FallsBackToAuthenticationCheck()
        {
            // Arrange
            var attr = new PermissionAuthorizeAttribute("aoi:workorder:read");
            var actionContext = CreateActionContext(
                authenticated: true,
                userName: "testuser",
                provider: null
            );

            // Act - 使用 reflection 呼叫 protected method
            var method = typeof(PermissionAuthorizeAttribute)
                .GetMethod("IsAuthorized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method.Invoke(attr, new object[] { actionContext });

            // Assert - 無 Provider 時 fallback 為僅檢查認證
            Assert.IsTrue(result, "沒有 IAuthorizationProvider 時，已認證的使用者應通過授權");
        }

        [TestMethod]
        public void IsAuthorized_UnauthenticatedUser_ReturnsFalse()
        {
            // Arrange
            var attr = new PermissionAuthorizeAttribute("aoi:workorder:read");
            var actionContext = CreateActionContext(
                authenticated: false,
                userName: null,
                provider: null
            );

            // Act
            var method = typeof(PermissionAuthorizeAttribute)
                .GetMethod("IsAuthorized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method.Invoke(attr, new object[] { actionContext });

            // Assert
            Assert.IsFalse(result, "未認證的使用者不應通過授權");
        }

        [TestMethod]
        public void IsAuthorized_WithProvider_HasPermission_ReturnsTrue()
        {
            // Arrange
            var mockProvider = new Mock<IAuthorizationProvider>();
            mockProvider.Setup(p => p.HasPermission("testuser", "aoi:workorder:read")).Returns(true);

            var attr = new PermissionAuthorizeAttribute("aoi:workorder:read");
            var actionContext = CreateActionContext(
                authenticated: true,
                userName: "testuser",
                provider: mockProvider.Object
            );

            // Act
            var method = typeof(PermissionAuthorizeAttribute)
                .GetMethod("IsAuthorized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method.Invoke(attr, new object[] { actionContext });

            // Assert
            Assert.IsTrue(result);
            mockProvider.Verify(p => p.HasPermission("testuser", "aoi:workorder:read"), Times.Once);
        }

        [TestMethod]
        public void IsAuthorized_WithProvider_NoPermission_ReturnsFalse()
        {
            // Arrange
            var mockProvider = new Mock<IAuthorizationProvider>();
            mockProvider.Setup(p => p.HasPermission("testuser", "admin:config:write")).Returns(false);

            var attr = new PermissionAuthorizeAttribute("admin:config:write");
            var actionContext = CreateActionContext(
                authenticated: true,
                userName: "testuser",
                provider: mockProvider.Object
            );

            // Act
            var method = typeof(PermissionAuthorizeAttribute)
                .GetMethod("IsAuthorized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method.Invoke(attr, new object[] { actionContext });

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// 建立測試用 HttpActionContext
        /// </summary>
        private static HttpActionContext CreateActionContext(bool authenticated, string userName, IAuthorizationProvider provider)
        {
            var config = new HttpConfiguration();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = new HttpRouteData(new HttpRoute());

            if (provider != null)
            {
                var mockScope = new Mock<System.Web.Http.Dependencies.IDependencyScope>();
                mockScope.Setup(s => s.GetService(typeof(IAuthorizationProvider))).Returns(provider);

                var mockResolver = new Mock<System.Web.Http.Dependencies.IDependencyResolver>();
                mockResolver.Setup(r => r.BeginScope()).Returns(mockScope.Object);
                config.DependencyResolver = mockResolver.Object;

                // 也需要在 request 層級設定 scope
                request.Properties["MS_DependencyScope"] = mockScope.Object;
            }

            var controllerContext = new HttpControllerContext(config, new HttpRouteData(new HttpRoute()), request);

            if (authenticated && userName != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userName)
                };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                controllerContext.RequestContext.Principal = new ClaimsPrincipal(identity);
            }
            else
            {
                controllerContext.RequestContext.Principal = new ClaimsPrincipal(new ClaimsIdentity());
            }

            var actionDescriptor = new Mock<HttpActionDescriptor>();
            actionDescriptor.Setup(a => a.ActionName).Returns("TestAction");

            return new HttpActionContext(controllerContext, actionDescriptor.Object);
        }
    }
}
