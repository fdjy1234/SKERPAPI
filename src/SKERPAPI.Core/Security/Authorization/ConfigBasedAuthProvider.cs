using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace SKERPAPI.Core.Security.Authorization
{
    /// <summary>
    /// 基於設定檔的簡易授權 Provider（開發/測試用）。
    /// 預設全部放行。正式環境應替換為 DbRbacAuthProvider。
    /// </summary>
    public class ConfigBasedAuthProvider : IAuthorizationProvider
    {
        public bool HasPermission(string userId, string permission)
        {
            Log.Debug("ConfigBasedAuthProvider: HasPermission({UserId}, {Permission}) → true (dev mode)", userId, permission);
            return true;
        }

        public bool IsInRole(string userId, string role)
        {
            Log.Debug("ConfigBasedAuthProvider: IsInRole({UserId}, {Role}) → true (dev mode)", userId, role);
            return true;
        }

        public IEnumerable<string> GetPermissions(string userId)
        {
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetRoles(string userId)
        {
            return Enumerable.Empty<string>();
        }
    }
}
