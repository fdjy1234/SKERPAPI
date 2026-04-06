using Serilog;

namespace SKERPAPI.Host
{
    /// <summary>
    /// Serilog 結構化日誌配置
    /// </summary>
    public static class SerilogConfig
    {
        public static void Configure()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(@"App_Data\logs\skerpapi-.log",
                              rollingInterval: RollingInterval.Day,
                              outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Serilog configured for SKERPAPI.");
        }
    }
}
