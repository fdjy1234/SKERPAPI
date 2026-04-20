using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using SKERPAPI.Core.Interfaces;
using SKERPAPI.Core.Services;

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

            // 4. 明確註冊 RBAC 服務（Singleton，覆蓋自動掃描；使用 Decorator 模式加快取層）
            var rbacBaseUrl = ConfigurationManager.AppSettings["RbacApiBaseUrl"] ?? string.Empty;
            var rbacTimeout = int.TryParse(ConfigurationManager.AppSettings["RbacApiTimeoutSeconds"], out int t) ? t : 10;
            var rbacTtl = int.TryParse(ConfigurationManager.AppSettings["RbacCacheTtlMinutes"], out int m) ? m : 5;

            var rbacHttpClient = new HttpClient
            {
                BaseAddress = new Uri(rbacBaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(rbacTimeout)
            };

            builder.Register(_ => new RbacServiceClient(rbacHttpClient))
                .Named<IRbacService>("rbacClient")
                .SingleInstance();

            builder.Register(c => new CachingRbacService(
                    c.ResolveNamed<IRbacService>("rbacClient"),
                    TimeSpan.FromMinutes(rbacTtl)))
                .As<IRbacService>()
                .SingleInstance();

            // 5. 建立容器並設為 Web API 的 DependencyResolver
            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            Serilog.Log.Information(
                "Autofac DI container configured. Scanned {Count} assemblies + {PluginCount} plugins. RBAC URL: {RbacUrl}, TTL: {Ttl}min.",
                assemblies.Length, pluginAssemblies.Length, rbacBaseUrl, rbacTtl);
        }
    }
}
