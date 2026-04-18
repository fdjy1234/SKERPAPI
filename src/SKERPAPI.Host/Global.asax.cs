using System;
using System.Web;

namespace SKERPAPI.Host
{
    /// <summary>
    /// Global Application 事件處理。
    /// Web API 配置已遷移至 OWIN Startup.cs。
    /// 此類別僅保留 Application 生命週期事件。
    /// </summary>
    public class Global : HttpApplication
    {
        protected void Application_Start()
        {
            // Web API 配置已遷移至 Startup.cs (OWIN Pipeline)
            // Serilog 配置也由 Startup.cs 負責
            // 此處保留作為 IIS Application 生命週期 Hook
        }

        protected void Application_End()
        {
            Serilog.Log.Information("SKERPAPI Host shutting down.");
            Serilog.Log.CloseAndFlush();
        }
    }
}
