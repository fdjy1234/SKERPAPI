using System;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;

namespace SKERPAPI.Host
{
    /// <summary>
    /// Autofac DI 容器配置。
    /// 自動掃描所有模組 Assembly 並註冊服務。
    /// </summary>
    public static class AutofacConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var builder = new ContainerBuilder();

            // 1. 掃描所有 SKERPAPI.* Assembly，自動註冊 Controller
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("SKERPAPI."))
                .ToArray();

            builder.RegisterApiControllers(assemblies);

            // 2. 自動掃描並註冊所有 Service（Interface → Implementation）
            foreach (var asm in assemblies)
            {
                builder.RegisterAssemblyTypes(asm)
                    .Where(t => t.Name.EndsWith("Service"))
                    .AsImplementedInterfaces()
                    .InstancePerRequest();
            }

            // 3. 註冊 Plugin 中的額外 Assembly (如果有)
            var pluginAssemblies = PluginLoader.GetLoadedPluginAssemblies();
            if (pluginAssemblies.Length > 0)
            {
                builder.RegisterApiControllers(pluginAssemblies);
                foreach (var asm in pluginAssemblies)
                {
                    builder.RegisterAssemblyTypes(asm)
                        .Where(t => t.Name.EndsWith("Service"))
                        .AsImplementedInterfaces()
                        .InstancePerRequest();
                }
            }

            // 4. 建立容器並設為 Web API 的 DependencyResolver
            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            Serilog.Log.Information("Autofac DI container configured. Scanned {Count} assemblies + {PluginCount} plugins.",
                assemblies.Length, pluginAssemblies.Length);
        }
    }
}
