using System.Collections.Generic;

namespace SKERPAPI.Core.Security.Authorization
{
    /// <summary>
    /// 授權提供者介面。
    /// 企業可實作此介面，對接內部 RBAC / AD / LDAP / 自建權限系統。
    /// </summary>
    /// <remarks>
    /// 設計原則：
    ///   - SKERPAPI 不硬建角色管理，僅提供抽象介面
    ///   - 透過 Autofac DI 注入，切換實作只需修改一行註冊
    ///   - 支援 Permission-based 與 Role-based 兩種查詢模式
    ///
    /// 實作範例：
    ///   - ConfigBasedAuthProvider：開發/測試用，全部放行
    ///   - DbRbacAuthProvider：查詢資料庫 User-Role-Permission
    ///   - ActiveDirectoryAuthProvider：查詢 AD Group → Permission Mapping
    /// </remarks>
    public interface IAuthorizationProvider
    {
        /// <summary>
        /// 檢查指定使用者是否具有某權限
        /// </summary>
        /// <param name="userId">使用者識別（可來自 ClaimsPrincipal 的 NameIdentifier 或 Name）</param>
        /// <param name="permission">權限代碼，格式建議："{module}:{resource}:{action}"，例如 "aoi:workorder:create"</param>
        /// <returns>是否授權</returns>
        bool HasPermission(string userId, string permission);

        /// <summary>
        /// 檢查指定使用者是否屬於某角色
        /// </summary>
        /// <param name="userId">使用者識別</param>
        /// <param name="role">角色名稱</param>
        /// <returns>是否屬於該角色</returns>
        bool IsInRole(string userId, string role);

        /// <summary>
        /// 取得使用者所有權限（可用於前端動態 UI 權限渲染）
        /// </summary>
        /// <param name="userId">使用者識別</param>
        /// <returns>權限代碼清單</returns>
        IEnumerable<string> GetPermissions(string userId);

        /// <summary>
        /// 取得使用者所有角色
        /// </summary>
        /// <param name="userId">使用者識別</param>
        /// <returns>角色名稱清單</returns>
        IEnumerable<string> GetRoles(string userId);
    }
}
