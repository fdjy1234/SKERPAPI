using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Routing;
using System.Web.Http.SelfHost;
using Asp.Versioning;
using Asp.Versioning.Routing;
using SKERPAPI.Core.Extensions;

namespace SKERPAPI.E2E.Tests
{
    /// <summary>
    /// E2E 測試用的 Self-Host 伺服器。
    /// 啟動完整的 Web API pipeline 做端對端 HTTP 測試。
    /// </summary>
    public class TestServerFixture : IDisposable
    {
        private readonly HttpServer _server;
        public HttpClient Client { get; }
        public string BaseUrl => "http://localhost/";

        /// <summary>
        /// 建立一個使用指定 API Key 的獨立 HttpClient（用於需要驗證特定金鑰行為的測試）。
        /// 呼叫端負責 Dispose。
        /// </summary>
        public HttpClient CreateClientWithKey(string apiKey)
        {
            // disposeHandler: false — 防止 Dispose client 時把共用 _server 一起關閉
            var client = new HttpClient(_server, disposeHandler: false) { BaseAddress = new Uri(BaseUrl) };
            if (apiKey != null)
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            return client;
        }

        public TestServerFixture()
        {
            var config = new HttpConfiguration();

            var jsonFormatter = config.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;

            // 配置 Core filters
            config.RegisterCoreFilters();

            // API Versioning — 「必須在 MapHttpAttributeRoutes 之前」
            //    不然 {version:apiVersion} constraint 註冊失敗導致所有端點 500
            config.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new QueryStringApiVersionReader("api-version"),
                    new HeaderApiVersionReader("X-Api-Version")
                );
            });

            // 啟用 Attribute Routing
            var constraintResolver = new DefaultInlineConstraintResolver();
            constraintResolver.ConstraintMap.Add("apiVersion", typeof(ApiVersionRouteConstraint));
            config.MapHttpAttributeRoutes(constraintResolver);

            // 預設路由
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // 手動註冊 AOI 和 CAR 服務（不用 Autofac 在測試環境）
            config.DependencyResolver = new TestDependencyResolver();

            _server = new HttpServer(config);
            Client = new HttpClient(_server) { BaseAddress = new Uri(BaseUrl) };

            // 為所有測試預設加入有效的 API Key，使 RBAC 過濾器通過
            Client.DefaultRequestHeaders.Add("X-Api-Key", AllPermissionsRbacServiceStub.E2ETestApiKey);
        }

        public void Dispose()
        {
            Client?.Dispose();
            _server?.Dispose();
        }
    }
}
