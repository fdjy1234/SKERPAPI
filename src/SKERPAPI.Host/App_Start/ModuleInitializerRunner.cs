using System;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using SKERPAPI.Core.Modules;
using Serilog;

namespace SKERPAPI.Host
{
    /// <summary>
    /// 內建模組初始化執行器。
    /// 掃描所有已載入的 SKERPAPI.* Assembly，
    /// 尋找並執行 IModuleInitializer 實作。
    /// </summary>
    public static class ModuleInitializerRunner
    {
        public static void RunAll(HttpConfiguration config)
        {
            var baseType = typeof(IModuleInitializer);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("SKERPAPI."))
                .ToList();

            var loadedModules = new System.Collections.Generic.List<string>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var moduleTypes = assembly.GetTypes()
                        .Where(t => baseType.IsAssignableFrom(t)
                                 && !t.IsInterface
                                 && !t.IsAbstract);

                    foreach (var moduleType in moduleTypes)
                    {
                        try
                        {
                            var instance = (IModuleInitializer)Activator.CreateInstance(moduleType);
                            instance.Initialize(config);
                            loadedModules.Add($"{instance.ModuleName} v{instance.ModuleVersion}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to initialize module: {TypeName}", moduleType.FullName);
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 某些 Assembly 可能無法載入所有型別，忽略
                }
            }

            Log.Information("ModuleInitializerRunner: {Count} modules loaded: [{Modules}]",
                loadedModules.Count, string.Join(", ", loadedModules));
        }
    }
}
