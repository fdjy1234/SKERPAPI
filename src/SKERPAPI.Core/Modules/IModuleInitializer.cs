using System.Web.Http;

namespace SKERPAPI.Core.Modules
{
    /// <summary>
    /// 定義模組初始化介面。
    /// 所有子系統專案 (AOI, CAR, MES...) 應實作此介面，
    /// 由 Host 的 PluginLoader 自動掃描並執行。
    /// </summary>
    public interface IModuleInitializer
    {
        /// <summary>
        /// 模組名稱，用於日誌識別（例如 "AOI", "CAR"）
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// 模組版本
        /// </summary>
        string ModuleVersion { get; }

        /// <summary>
        /// 初始化邏輯：DI 註冊、模組專屬路由、Filter 配置等
        /// </summary>
        /// <param name="config">全域 HttpConfiguration</param>
        void Initialize(HttpConfiguration config);
    }
}
