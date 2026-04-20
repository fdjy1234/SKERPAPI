using System.Collections.Generic;
using System.Linq;

namespace SKERPAPI.Core.Models
{
    /// <summary>
    /// RBAC 授權上下文。
    /// 攜帶呼叫端的認證狀態、角色清單與權限清單，
    /// 在 RbacAuthorizeAttribute 中用於授權判斷。
    /// </summary>
    public sealed class RbacContext
    {
        /// <summary>API Key 是否通過認證</summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>呼叫端 API Key（遮罩後用於日誌）</summary>
        public string ApiKey { get; set; }

        /// <summary>金鑰名稱（由 RBAC API 回傳）</summary>
        public string KeyName { get; set; }

        /// <summary>擁有的角色清單</summary>
        public IReadOnlyList<string> Roles { get; set; } = new List<string>();

        /// <summary>擁有的權限清單（格式：module:resource:action）</summary>
        public IReadOnlyList<string> Permissions { get; set; } = new List<string>();

        /// <summary>
        /// 建立未認證的上下文（用於 API Key 不存在的情況）
        /// </summary>
        public static RbacContext Unauthenticated() =>
            new RbacContext { IsAuthenticated = false };

        /// <summary>
        /// 檢查是否擁有指定的權限
        /// </summary>
        /// <param name="permission">權限字串，格式：module:resource:action</param>
        public bool HasPermission(string permission)
        {
            if (string.IsNullOrEmpty(permission)) return false;
            return Permissions != null && Permissions.Any(p =>
                string.Equals(p, permission, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 檢查是否擁有任一指定權限（OR 語意）
        /// </summary>
        public bool HasAnyPermission(params string[] permissions)
        {
            if (permissions == null || permissions.Length == 0) return false;
            return permissions.Any(HasPermission);
        }
    }
}
