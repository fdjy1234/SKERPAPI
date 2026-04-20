using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SKERPAPI.Core.Interfaces;
using SKERPAPI.Core.Models;

namespace SKERPAPI.Core.Services
{
    /// <summary>
    /// RBAC 服務 HTTP 客戶端。
    /// 呼叫內部 RBAC REST API 取得 API Key 的角色與權限資訊。
    /// </summary>
    /// <remarks>
    /// Fail-Closed 策略：當 RBAC API 無法連線時，拋出例外而非返回空上下文，
    /// 由上層 RbacAuthorizeAttribute 捕捉並回傳 503。
    /// </remarks>
    public sealed class RbacServiceClient : IRbacService
    {
        private readonly HttpClient _httpClient;

        /// <param name="httpClient">已設定 BaseAddress 與 Timeout 的 HttpClient（應為 Singleton）</param>
        public RbacServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc />
        public async Task<RbacContext> ResolveAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(apiKey))
                return RbacContext.Unauthenticated();

            var url = $"api/rbac/keys/{Uri.EscapeDataString(apiKey)}";

            HttpResponseMessage response;
            try
            {
                response = await _httpClient
                    .GetAsync(url, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "RBAC service is unavailable. Unable to authorize request.", ex);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
                return RbacContext.Unauthenticated();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"RBAC service returned unexpected status {(int)response.StatusCode}.");
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var dto = JsonConvert.DeserializeObject<RbacApiKeyResponse>(json);

            if (dto == null || !dto.IsAuthenticated)
                return RbacContext.Unauthenticated();

            if (dto.IsExpired)
                return RbacContext.Unauthenticated();

            return new RbacContext
            {
                IsAuthenticated = true,
                ApiKey = dto.ApiKey,
                KeyName = dto.KeyName,
                Roles = dto.Roles ?? new System.Collections.Generic.List<string>(),
                Permissions = dto.Permissions ?? new System.Collections.Generic.List<string>()
            };
        }
    }
}
