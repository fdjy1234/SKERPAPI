using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;
using SKERPAPI.Core.Interfaces;
using SKERPAPI.Core.Models;

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

            // 註冊 RBAC Service（測試用 Stub，預設回傳所有 Permission 皆通過）
            _registrations[typeof(IRbacService)] =
                () => new AllPermissionsRbacServiceStub();
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

    /// <summary>
    /// E2E 測試用 RBAC Service Stub。
    /// 接受所有 E2E_TEST_KEY，並允許所有權限。
    /// </summary>
    internal sealed class AllPermissionsRbacServiceStub : IRbacService
    {
        public const string E2ETestApiKey = "E2E_TEST_KEY";

        public Task<RbacContext> ResolveAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(apiKey))
                return Task.FromResult(RbacContext.Unauthenticated());

            // 模擬無效金鑰
            if (apiKey == "INVALID_KEY")
                return Task.FromResult(RbacContext.Unauthenticated());

            // 模擬有效但權限不足的金鑰（用於 403 測試）
            if (apiKey == "NO_PERMISSION_KEY")
            {
                return Task.FromResult(new RbacContext
                {
                    IsAuthenticated = true,
                    ApiKey = apiKey,
                    Permissions = new List<string>() // 無任何 Permission
                });
            }

            // 預設：所有 E2E 測試用金鑰皆通過，給予全部權限
            return Task.FromResult(new RbacContext
            {
                IsAuthenticated = true,
                ApiKey = apiKey,
                KeyName = "E2E Test Key",
                Roles = new List<string> { "E2E_TESTER" },
                Permissions = new List<string>
                {
                    "aoi:status:read",
                    "aoi:inspection:execute",
                    "aoi:inspection:history",
                    "aoi:device:read",
                    "car:system:read",
                    "car:vehicle:register",
                    "car:vehicle:read",
                    "car:maintenance:read",
                    "car:maintenance:create"
                }
            });
        }
    }
}

