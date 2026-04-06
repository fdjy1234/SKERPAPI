using System.Web.Http;
using SKERPAPI.Core.Modules;
using Serilog;

namespace SKERPAPI.AOI
{
    /// <summary>
    /// AOI 模組初始化器
    /// </summary>
    public class AOIModuleInitializer : IModuleInitializer
    {
        public string ModuleName => "AOI";
        public string ModuleVersion => "2.0.0";

        public void Initialize(HttpConfiguration config)
        {
            Log.Information("AOI Module initialized: {ModuleName} v{Version}", ModuleName, ModuleVersion);
        }
    }
}
