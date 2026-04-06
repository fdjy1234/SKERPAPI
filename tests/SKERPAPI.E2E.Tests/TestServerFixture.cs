using System;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.SelfHost;
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

        public TestServerFixture()
        {
            var config = new HttpConfiguration();

            var jsonFormatter = config.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;

            // 配置 Core filters
            config.RegisterCoreFilters();

            // 啟用 Attribute Routing
            config.MapHttpAttributeRoutes();

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
        }

        public void Dispose()
        {
            Client?.Dispose();
            _server?.Dispose();
        }
    }
}
