using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKERPAPI.Core.Services;

namespace SKERPAPI.Core.Tests.Services
{
    /// <summary>
    /// RbacServiceClient 單元測試。
    /// 使用假 HttpMessageHandler 攔截 HTTP 呼叫，不需要真實 RBAC 伺服器。
    /// </summary>
    [TestClass]
    public class RbacServiceClientTests
    {
        // ---------------------------------------------------------------
        // 輔助：建立假 HttpClient（固定回傳指定狀態碼與回應內容）
        // ---------------------------------------------------------------
        private static HttpClient BuildFakeClient(HttpStatusCode statusCode, string json)
        {
            var handler = new FakeHttpMessageHandler(statusCode, json);
            return new HttpClient(handler) { BaseAddress = new Uri("http://fake-rbac/") };
        }

        private const string ValidKeyJson = @"{
            ""isAuthenticated"": true,
            ""apiKey"": ""test-key-001"",
            ""keyName"": ""Test Key"",
            ""roles"": [""AOI_OPERATOR""],
            ""permissions"": [""aoi:inspection:execute"", ""aoi:status:read""],
            ""expiresAt"": null,
            ""isExpired"": false
        }";

        private const string NotFoundJson = @"{ ""isAuthenticated"": false }";

        // ---------------------------------------------------------------
        // Test 1: 有效 API Key → 回傳已認證的 RbacContext，包含角色與權限
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task ReturnsAuthenticatedContext_WhenApiKeyIsValid()
        {
            var client = BuildFakeClient(HttpStatusCode.OK, ValidKeyJson);
            var sut = new RbacServiceClient(client);

            var ctx = await sut.ResolveAsync("test-key-001", CancellationToken.None);

            Assert.IsTrue(ctx.IsAuthenticated);
            Assert.AreEqual("test-key-001", ctx.ApiKey);
            Assert.IsTrue(ctx.HasPermission("aoi:inspection:execute"));
            Assert.IsTrue(ctx.HasPermission("aoi:status:read"));
            Assert.IsFalse(ctx.HasPermission("car:vehicle:read"));
        }

        // ---------------------------------------------------------------
        // Test 2: RBAC API 回傳 404 → 回傳未認證上下文
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task ReturnsUnauthenticated_WhenApiKeyNotFound()
        {
            var client = BuildFakeClient(HttpStatusCode.NotFound, NotFoundJson);
            var sut = new RbacServiceClient(client);

            var ctx = await sut.ResolveAsync("nonexistent-key", CancellationToken.None);

            Assert.IsFalse(ctx.IsAuthenticated);
        }

        // ---------------------------------------------------------------
        // Test 3: HTTP 呼叫失敗 → 拋出例外（Fail-Closed）
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task ThrowsException_WhenHttpClientFails()
        {
            var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://fake-rbac/") };
            var sut = new RbacServiceClient(client);

            try
            {
                await sut.ResolveAsync("some-key", CancellationToken.None);
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException)
            {
                // Expected — Fail-Closed
            }
        }

        // ---------------------------------------------------------------
        // 內部 Fake Handlers
        // ---------------------------------------------------------------
        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _statusCode;
            private readonly string _content;

            public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
            {
                _statusCode = statusCode;
                _content = content;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }

        private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
        {
            private readonly Exception _exception;

            public ThrowingHttpMessageHandler(Exception exception)
            {
                _exception = exception;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw _exception;
            }
        }
    }
}
