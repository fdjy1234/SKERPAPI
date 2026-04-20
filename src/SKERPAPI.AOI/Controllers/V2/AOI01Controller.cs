using System.Web.Http;
using Asp.Versioning;
using SKERPAPI.Core.Controllers;
using SKERPAPI.Core.Filters;
using SKERPAPI.Core.Permissions;
using SKERPAPI.AOI.Services;
using SKERPAPI.AOI.Models;

namespace SKERPAPI.AOI.Controllers.V2
{
    /// <summary>
    /// AOI01 v2 控制器 - AOI 檢測主要端點（v2）
    /// </summary>
    /// <remarks>
    /// 路由前綴: webapi/aoi/v{version}/aoi01
    ///
    /// Breaking changes from v1:
    ///   - POST inspect: 請求模型由 AOIInspectionRequest 改為 AOIInspectionV2Request
    ///     - StationCode  → WorkstationCode
    ///     - InspectionItems → Items
    ///     - 新增 Priority 欄位
    /// </remarks>
    [ApiVersion("2.0")]
    [RoutePrefix("webapi/aoi/v{version:apiVersion}/aoi01")]
    public class AOI01Controller : ApiBaseController
    {
        private readonly IAOIService _aoiService;

        public AOI01Controller(IAOIService aoiService)
        {
            _aoiService = aoiService;
        }

        /// <summary>取得 AOI 系統狀態</summary>
        [HttpGet, Route("status")]
        [RbacAuthorize(Permission = RbacPermissions.Aoi.StatusRead)]
        public IHttpActionResult GetStatus()
        {
            var status = _aoiService.GetStatus();
            return ApiOk(status);
        }

        /// <summary>
        /// 執行 AOI 檢測（v2 - 使用新的 AOIInspectionV2Request 模型）
        /// </summary>
        [HttpPost, Route("inspect")]
        [RbacAuthorize(Permission = RbacPermissions.Aoi.InspectionExecute)]
        public IHttpActionResult Inspect([FromBody] AOIInspectionV2Request request)
        {
            if (!ValidateRequest())
            {
                return ApiFail("Validation failed.");
            }

            // 適配層：將 v2 請求轉換為服務層使用的 v1 模型
            var v1Request = new AOIInspectionRequest
            {
                BatchId = request.BatchId,
                StationCode = request.WorkstationCode,
                InspectionItems = request.Items,
                OperatorId = request.OperatorId
            };

            var result = _aoiService.Inspect(v1Request);
            return ApiOk(result);
        }

        /// <summary>查詢 AOI 檢測歷史記錄（分頁）</summary>
        [HttpGet, Route("history")]
        [RbacAuthorize(Permission = RbacPermissions.Aoi.InspectionHistory)]
        public IHttpActionResult GetHistory(int page = 1, int pageSize = 20)
        {
            var result = _aoiService.GetInspectionHistory(page, pageSize);
            return ApiPagedOk(result.Items, result.TotalCount, page, pageSize);
        }
    }
}
