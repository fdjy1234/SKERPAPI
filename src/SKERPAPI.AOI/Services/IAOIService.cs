using System.Collections.Generic;
using SKERPAPI.AOI.Models;

namespace SKERPAPI.AOI.Services
{
    /// <summary>
    /// AOI 檢測服務介面
    /// </summary>
    public interface IAOIService
    {
        /// <summary>取得 AOI 系統狀態</summary>
        object GetStatus();

        /// <summary>執行 AOI 檢測</summary>
        AOIInspectionResult Inspect(AOIInspectionRequest request);

        /// <summary>查詢檢測歷史記錄 (分頁)</summary>
        (List<AOIInspectionResult> Items, int TotalCount) GetInspectionHistory(int page, int pageSize);
    }
}
