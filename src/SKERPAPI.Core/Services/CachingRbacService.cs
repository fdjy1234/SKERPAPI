using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using SKERPAPI.Core.Interfaces;
using SKERPAPI.Core.Models;

namespace SKERPAPI.Core.Services
{
    /// <summary>
    /// RBAC 快取 Decorator。
    /// 使用 MemoryCache 暫存授權上下文，避免每個 Request 都呼叫 RBAC REST API。
    /// </summary>
    /// <remarks>
    /// 快取 TTL 由建構子的 cacheTtl 參數控制（預設 5 分鐘）。
    /// MemoryCache.Default 為 thread-safe，可在 Singleton 生命週期安全使用。
    /// </remarks>
    public sealed class CachingRbacService : IRbacService
    {
        private readonly IRbacService _inner;
        private readonly TimeSpan _cacheTtl;
        private readonly MemoryCache _cache;

        private const string CacheKeyPrefix = "rbac:";

        /// <param name="inner">被裝飾的 RbacServiceClient</param>
        /// <param name="cacheTtl">快取存活時間</param>
        /// <param name="cache">MemoryCache 實例（為 null 時使用 Default；僅單元測試時注入自訂 instance）</param>
        public CachingRbacService(IRbacService inner, TimeSpan cacheTtl, MemoryCache cache = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _cacheTtl = cacheTtl;
            _cache = cache ?? MemoryCache.Default;
        }

        /// <inheritdoc />
        public async Task<RbacContext> ResolveAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(apiKey))
                return RbacContext.Unauthenticated();

            var cacheKey = CacheKeyPrefix + apiKey;

            if (_cache.Get(cacheKey) is RbacContext cached)
                return cached;

            var context = await _inner.ResolveAsync(apiKey, cancellationToken).ConfigureAwait(false);

            _cache.Set(cacheKey, context, new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(_cacheTtl)
            });

            return context;
        }
    }
}
