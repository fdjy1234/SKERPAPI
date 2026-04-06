using System.Web.Http;
using SKERPAPI.Core.Controllers;
using SKERPAPI.AOI.Services;
using SKERPAPI.AOI.Models;

namespace SKERPAPI.AOI.Controllers.V1
{
    /// <summary>
    /// AOI01 控制器 - AOI 檢測主要端點
    /// </summary>
    /// <remarks>
    /// 路由前綴: webapi/aoi/v1/aoi01
    /// 
    /// 端點清單:
    ///   GET  webapi/aoi/v1/aoi01/status     - 取得系統狀態
    ///   POST webapi/aoi/v1/aoi01/inspect    - 執行檢測
    ///   GET  webapi/aoi/v1/aoi01/history    - 查詢檢測記錄 (分頁)
    /// </remarks>
    [RoutePrefix("webapi/aoi/v1/aoi01")]
    public class AOI01Controller : ApiBaseController
    {
        private readonly IAOIService _aoiService;

        /// <summary>
        /// 建構子 - 透過 DI 注入 IAOIService
        /// </summary>
        public AOI01Controller(IAOIService aoiService)
        {
            _aoiService = aoiService;
        }

        /// <summary>
        /// 取得 AOI 系統狀態
        /// </summary>
        [HttpGet, Route("status")]
        public IHttpActionResult GetStatus()
        {
            var status = _aoiService.GetStatus();
            return ApiOk(status);
        }

        /// <summary>
        /// 執行 AOI 檢測
        /// </summary>
        [HttpPost, Route("inspect")]
        public IHttpActionResult Inspect([FromBody] AOIInspectionRequest request)
        {
            if (!ValidateRequest())
            {
                return ApiFail("Validation failed.");
            }

            var result = _aoiService.Inspect(request);
            return ApiOk(result);
        }

        /// <summary>
        /// 查詢 AOI 檢測歷史記錄 (分頁)
        /// </summary>
        [HttpGet, Route("history")]
        public IHttpActionResult GetHistory(int page = 1, int pageSize = 20)
        {
            var result = _aoiService.GetInspectionHistory(page, pageSize);
            return ApiPagedOk(result.Items, result.TotalCount, page, pageSize);
        }
    }
}
