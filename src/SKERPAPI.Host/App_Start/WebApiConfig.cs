using System.Web.Http;
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

            // 2. 啟用 Attribute Routing（掃描所有模組 Assembly 的 [RoutePrefix]）
            config.MapHttpAttributeRoutes();

            // 3. 配置 Autofac DI 容器
            AutofacConfig.Register(config);

            // 4. 從 App_Data/Plugins 載入外部 Plugin
            PluginLoader.LoadPlugins(config);

            // 5. 自動載入內建模組 (AOI, CAR, ...)
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
