using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Dependencies;

namespace SKERPAPI.E2E.Tests
{
    /// <summary>
    /// 簡易 DI Resolver，用於 E2E 測試環境
    /// </summary>
    public class TestDependencyResolver : IDependencyResolver
    {
        private readonly Dictionary<Type, Func<object>> _registrations = new Dictionary<Type, Func<object>>();

        public TestDependencyResolver()
        {
            // 註冊 AOI Service
            _registrations[typeof(SKERPAPI.AOI.Services.IAOIService)] =
                () => new SKERPAPI.AOI.Services.AOIService();

            // 註冊 CAR Service
            _registrations[typeof(SKERPAPI.CAR.Services.ICARService)] =
                () => new SKERPAPI.CAR.Services.CARService();
        }

        public object GetService(Type serviceType)
        {
            if (_registrations.ContainsKey(serviceType))
                return _registrations[serviceType]();

            // 嘗試建立 Controller
            if (!serviceType.IsAbstract && !serviceType.IsInterface)
            {
                try
                {
                    var ctors = serviceType.GetConstructors();
                    if (ctors.Length > 0)
                    {
                        var ctor = ctors.OrderByDescending(c => c.GetParameters().Length).First();
                        var parameters = ctor.GetParameters()
                            .Select(p => GetService(p.ParameterType))
                            .ToArray();
                        return ctor.Invoke(parameters);
                    }
                    return Activator.CreateInstance(serviceType);
                }
                catch { }
            }
            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType) => Enumerable.Empty<object>();
        public IDependencyScope BeginScope() => this;
        public void Dispose() { }
    }
}
