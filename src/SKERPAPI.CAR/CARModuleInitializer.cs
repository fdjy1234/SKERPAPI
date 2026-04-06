using System.Web.Http;
using SKERPAPI.Core.Modules;
using Serilog;

namespace SKERPAPI.CAR
{
    /// <summary>
    /// CAR 模組初始化器
    /// </summary>
    public class CARModuleInitializer : IModuleInitializer
    {
        public string ModuleName => "CAR";
        public string ModuleVersion => "2.0.0";

        public void Initialize(HttpConfiguration config)
        {
            Log.Information("CAR Module initialized: {ModuleName} v{Version}", ModuleName, ModuleVersion);
        }
    }
}
