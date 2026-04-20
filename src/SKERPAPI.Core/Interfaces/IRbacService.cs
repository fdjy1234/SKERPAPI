using System.Threading;
using System.Threading.Tasks;
using SKERPAPI.Core.Models;

namespace SKERPAPI.Core.Interfaces
{
    /// <summary>
    /// RBAC 授權服務介面。
    /// 依據 API Key 解析使用者的角色與權限清單。
    /// </summary>
    public interface IRbacService
    {
        /// <summary>
        /// 依 API Key 查詢 RBAC 授權上下文。
        /// </summary>
        /// <param name="apiKey">來自 X-Api-Key Header 的金鑰</param>
        /// <param name="cancellationToken">取消符記</param>
        /// <returns>授權上下文；若金鑰無效則 IsAuthenticated = false</returns>
        /// <exception cref="System.Exception">當 RBAC 服務無法連線時拋出（Fail-Closed）</exception>
        Task<RbacContext> ResolveAsync(string apiKey, CancellationToken cancellationToken = default);
    }
}
