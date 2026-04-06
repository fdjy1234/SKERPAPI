using System.Web.Http;

namespace SKERPAPI.Core.Extensions
{
    /// <summary>
    /// HttpConfiguration 擴展方法，提供共用配置功能
    /// </summary>
    public static class HttpConfigurationExtensions
    {
        /// <summary>
        /// 註冊 SKERPAPI 核心過濾器至全域管線
        /// </summary>
        public static void RegisterCoreFilters(this HttpConfiguration config)
        {
            config.Filters.Add(new Filters.ApiExceptionFilter());
            config.Filters.Add(new Filters.ModelValidationFilter());
        }
    }
}
