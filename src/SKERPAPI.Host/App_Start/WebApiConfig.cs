using System.Web.Http;
using System.Web.Http.Routing;
using Asp.Versioning;
using Asp.Versioning.Routing;
using SKERPAPI.Core.Extensions;

namespace SKERPAPI.Host
{
    /// <summary>
    /// Web API 配置入口。
    /// 負責路由、過濾器、DI、模組載入配置。
    /// </summary>
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // 0. JSON 序列化設定 (使用 camelCase)
            var jsonFormatter = config.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;

            // 1. 註冊 Core 層全域過濾器
            config.RegisterCoreFilters();

            // 2. 註冊 API Versioning（Asp.Versioning.WebApi 7.x）
            //    「必須在 MapHttpAttributeRoutes 之前」，才能讓 {version:apiVersion} constraint 正確識別
            config.AddApiVersioning(options =>
            {
                // 在回應 header 中回報支援的版本（api-supported-versions / api-deprecated-versions）
                options.ReportApiVersions = true;
                // 未指定版本時，使用預設版本 1.0
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                // 組合多種版本讀取器：URL segment（/v1/）、query（?api-version=1.0）、header（X-Api-Version: 1.0）
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new QueryStringApiVersionReader("api-version"),
                    new HeaderApiVersionReader("X-Api-Version")
                );
            });

            // 3. 啟用 Attribute Routing（掃描所有模組 Assembly 的 [RoutePrefix]）
            //    需在 AddApiVersioning 之後執行，不然 apiVersion constraint 會訪失敗
            //    必須明確傳入含 ApiVersionRouteConstraint 的 resolver，否則 {version:apiVersion} 無法識別
            var constraintResolver = new DefaultInlineConstraintResolver();
            constraintResolver.ConstraintMap.Add("apiVersion", typeof(ApiVersionRouteConstraint));
            config.MapHttpAttributeRoutes(constraintResolver);

            // 4. 配置 Autofac DI 容器
            AutofacConfig.Register(config);

            // 5. 從 App_Data/Plugins 載入外部 Plugin
            PluginLoader.LoadPlugins(config);

            // 6. 自動載入內建模組 (AOI, CAR, ...)
            ModuleInitializerRunner.RunAll(config);

            // 6. 預設路由（備援）
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            Serilog.Log.Information("WebApiConfig registered successfully.");
        }
    }
}
