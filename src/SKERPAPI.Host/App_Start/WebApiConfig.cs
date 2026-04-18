using System.Web.Http;
using SKERPAPI.Core.Extensions;
using SKERPAPI.Core.Security.Cors;

namespace SKERPAPI.Host
{
    /// <summary>
    /// Web API 配置入口。
    /// 負責路由、過濾器、DI、模組載入、CORS (Layer 2) 配置。
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

            // 2. 啟用 CORS (Layer 2 - Web API Attribute)
            CorsConfig.EnableWebApiCors(config);

            // 3. 啟用 Attribute Routing（掃描所有模組 Assembly 的 [RoutePrefix]）
            config.MapHttpAttributeRoutes();

            // 4. 配置 Autofac DI 容器
            AutofacConfig.Register(config);

            // 5. 從 App_Data/Plugins 載入外部 Plugin
            PluginLoader.LoadPlugins(config);

            // 6. 自動載入內建模組 (AOI, CAR, ...)
            ModuleInitializerRunner.RunAll(config);

            // 7. 預設路由（備援）
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            Serilog.Log.Information("WebApiConfig registered successfully.");
        }
    }
}

