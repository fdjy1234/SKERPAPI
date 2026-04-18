using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace SKERPAPI.Core.Security.Authorization
{
    /// <summary>
    /// 自建資料庫 RBAC 授權 Provider。
    /// 查詢 DB 中的 User-Role-Permission 對應關係進行授權。
    /// </summary>
    /// <remarks>
    /// 資料庫結構（建議）：
    ///   Users  (UserId, UserName, ...)
    ///   Roles  (RoleId, RoleName, Description)
    ///   Permissions (PermissionId, PermissionCode, Description)
    ///   UserRoles (UserId, RoleId)
    ///   RolePermissions (RoleId, PermissionId)
    ///
    /// 權限代碼命名慣例：
    ///   "{module}:{resource}:{action}"
    ///   例如：aoi:workorder:create, car:vehicle:read
    ///
    /// 目前階段為記憶體內的模擬實作，便於開發與測試。
    /// 正式環境需連接實際 DB（可使用 Dapper / Entity Framework）。
    /// </remarks>
    public class DbRbacAuthProvider : IAuthorizationProvider
    {
        // ── 記憶體快取 (正式環境替換為 DB 查詢) ──
        private readonly ConcurrentDictionary<string, HashSet<string>> _userRoles;
        private readonly ConcurrentDictionary<string, HashSet<string>> _rolePermissions;

        public DbRbacAuthProvider()
        {
            _userRoles = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            _rolePermissions = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            // 預設角色與權限（開發期用）
            SeedDefaultData();
        }

        /// <summary>
        /// 初始化預設角色權限資料（開發期用）
        /// </summary>
        private void SeedDefaultData()
        {
            // 定義角色權限
            _rolePermissions["Admin"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "aoi:*:*", "car:*:*", "admin:*:*"
            };

            _rolePermissions["AOI_Operator"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "aoi:workorder:read", "aoi:workorder:create",
                "aoi:inspection:read", "aoi:inspection:execute",
                "aoi:status:read"
            };

            _rolePermissions["CAR_Operator"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "car:vehicle:read", "car:vehicle:create",
                "car:info:read"
            };

            _rolePermissions["ReadOnly"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "aoi:*:read", "car:*:read"
            };
        }

        /// <summary>
        /// 新增使用者角色對應（供外部初始化或 DB 載入使用）
        /// </summary>
        public void AssignRole(string userId, string role)
        {
            _userRoles.AddOrUpdate(userId,
                _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { role },
                (_, existing) => { existing.Add(role); return existing; }
            );
        }

        /// <summary>
        /// 新增角色權限對應（供外部初始化或 DB 載入使用）
        /// </summary>
        public void AddRolePermission(string role, string permission)
        {
            _rolePermissions.AddOrUpdate(role,
                _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { permission },
                (_, existing) => { existing.Add(permission); return existing; }
            );
        }

        public bool HasPermission(string userId, string permission)
        {
            var roles = GetRoles(userId);
            foreach (var role in roles)
            {
                if (_rolePermissions.TryGetValue(role, out var permissions))
                {
                    if (MatchPermission(permissions, permission))
                    {
                        Log.Debug("DbRbacAuth: User {UserId} has permission {Permission} via role {Role}",
                            userId, permission, role);
                        return true;
                    }
                }
            }

            Log.Debug("DbRbacAuth: User {UserId} denied permission {Permission}", userId, permission);
            return false;
        }

        public bool IsInRole(string userId, string role)
        {
            return _userRoles.TryGetValue(userId, out var roles) &&
                   roles.Contains(role);
        }

        public IEnumerable<string> GetPermissions(string userId)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roles = GetRoles(userId);
            foreach (var role in roles)
            {
                if (_rolePermissions.TryGetValue(role, out var permissions))
                {
                    foreach (var perm in permissions)
                        result.Add(perm);
                }
            }
            return result;
        }

        public IEnumerable<string> GetRoles(string userId)
        {
            if (_userRoles.TryGetValue(userId, out var roles))
                return roles;
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// 權限比對（支援萬用字元 *）
        /// </summary>
        /// <remarks>
        /// 例如：
        ///   "aoi:*:*" 匹配 "aoi:workorder:create"
        ///   "aoi:workorder:*" 匹配 "aoi:workorder:read"
        ///   "aoi:*:read" 匹配 "aoi:inspection:read"
        /// </remarks>
        private static bool MatchPermission(HashSet<string> grantedPermissions, string requiredPermission)
        {
            if (grantedPermissions.Contains(requiredPermission))
                return true;

            var requiredParts = requiredPermission.Split(':');
            foreach (var granted in grantedPermissions)
            {
                var grantedParts = granted.Split(':');
                if (grantedParts.Length != requiredParts.Length)
                    continue;

                var match = true;
                for (int i = 0; i < grantedParts.Length; i++)
                {
                    if (grantedParts[i] != "*" && !string.Equals(grantedParts[i], requiredParts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
            return false;
        }
    }
}
