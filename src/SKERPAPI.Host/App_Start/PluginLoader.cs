using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using SKERPAPI.Core.Modules;
using Serilog;

namespace SKERPAPI.Host
{
    /// <summary>
    /// Plugin 動態載入器。
    /// 啟動時掃描 App_Data/Plugins/ 目錄下的 DLL，
    /// 載入並執行 IModuleInitializer 實作。
    /// </summary>
    /// <remarks>
    /// 設計原則：
    ///   - 不需重編 Host 即可加入新模組
    ///   - 將 DLL 放入 App_Data/Plugins/ 目錄即可
    ///   - 支援版本檢查和衝突偵測
    ///   - 完整的錯誤處理，單個 Plugin 失敗不影響其他
    /// </remarks>
    public static class PluginLoader
    {
        private static readonly List<Assembly> _loadedPlugins = new List<Assembly>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 取得已載入的 Plugin Assembly
        /// </summary>
        public static Assembly[] GetLoadedPluginAssemblies()
        {
            lock (_lock)
            {
                return _loadedPlugins.ToArray();
            }
        }

        /// <summary>
        /// 從 App_Data/Plugins/ 載入所有外部 Plugin DLL
        /// </summary>
        public static void LoadPlugins(HttpConfiguration config)
        {
            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Plugins");

            if (!Directory.Exists(pluginDir))
            {
                Directory.CreateDirectory(pluginDir);
                Log.Information("PluginLoader: Created plugin directory at {Path}", pluginDir);
            }

            var dllFiles = Directory.GetFiles(pluginDir, "SKERPAPI.*.dll");
            Log.Information("PluginLoader: Found {Count} plugin DLLs in {Path}", dllFiles.Length, pluginDir);

            foreach (var dllPath in dllFiles)
            {
                LoadSinglePlugin(dllPath, config);
            }

            Log.Information("PluginLoader: {Count} plugins loaded successfully.", _loadedPlugins.Count);
        }

        private static void LoadSinglePlugin(string dllPath, HttpConfiguration config)
        {
            var fileName = Path.GetFileName(dllPath);

            try
            {
                // 檢查是否已載入相同名稱的 Assembly
                var asmName = AssemblyName.GetAssemblyName(dllPath);
                var existing = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == asmName.Name);

                if (existing != null)
                {
                    Log.Warning("PluginLoader: Assembly {Name} already loaded (v{Version}), skipping {File}",
                        asmName.Name, existing.GetName().Version, fileName);
                    return;
                }

                // 載入 Assembly
                var assembly = Assembly.LoadFrom(dllPath);

                lock (_lock)
                {
                    _loadedPlugins.Add(assembly);
                }

                // 尋找並執行 IModuleInitializer
                var initializerTypes = assembly.GetTypes()
                    .Where(t => typeof(IModuleInitializer).IsAssignableFrom(t)
                             && !t.IsInterface
                             && !t.IsAbstract)
                    .ToList();

                foreach (var initType in initializerTypes)
                {
                    try
                    {
                        var initializer = (IModuleInitializer)Activator.CreateInstance(initType);
                        initializer.Initialize(config);
                        Log.Information("PluginLoader: Plugin module loaded: {ModuleName} v{Version} from {File}",
                            initializer.ModuleName, initializer.ModuleVersion, fileName);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "PluginLoader: Failed to initialize module {Type} from {File}",
                            initType.FullName, fileName);
                    }
                }

                if (initializerTypes.Count == 0)
                {
                    Log.Information("PluginLoader: Loaded assembly {File} (no IModuleInitializer found)", fileName);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log.Error(ex, "PluginLoader: Type load error for {File}. Loader exceptions: {Errors}",
                    fileName, string.Join("; ", ex.LoaderExceptions?.Select(e => e?.Message) ?? Array.Empty<string>()));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PluginLoader: Failed to load plugin {File}", fileName);
            }
        }
    }
}
