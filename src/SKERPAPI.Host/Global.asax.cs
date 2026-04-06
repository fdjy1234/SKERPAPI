using System;
using System.Web;
using System.Web.Http;

namespace SKERPAPI.Host
{
    public class Global : HttpApplication
    {
        protected void Application_Start()
        {
            // 1. 配置 Serilog
            SerilogConfig.Configure();

            // 2. 配置 Web API
            GlobalConfiguration.Configure(WebApiConfig.Register);

            Serilog.Log.Information("SKERPAPI Host started at {Time}", DateTime.UtcNow);
        }

        protected void Application_End()
        {
            Serilog.Log.Information("SKERPAPI Host shutting down.");
            Serilog.Log.CloseAndFlush();
        }
    }
}
