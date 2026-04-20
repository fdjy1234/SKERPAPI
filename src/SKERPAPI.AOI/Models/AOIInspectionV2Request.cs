using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SKERPAPI.AOI.Models
{
    /// <summary>
    /// AOI 檢測請求模型 v2（Breaking change from v1）
    /// </summary>
    /// <remarks>
    /// Breaking changes from v1:
    ///   - StationCode  → 重新命名為 WorkstationCode
    ///   - InspectionItems → 重新命名為 Items
    ///   - 新增 Priority 欄位（可選）
    /// </remarks>
    public class AOIInspectionV2Request
    {
        /// <summary>產品批次編號（必填）</summary>
        [Required(ErrorMessage = "BatchId is required.")]
        public string BatchId { get; set; }

        /// <summary>工站代碼（v2 改名自 StationCode）</summary>
        [Required(ErrorMessage = "WorkstationCode is required.")]
        public string WorkstationCode { get; set; }

        /// <summary>檢測項目清單（v2 改名自 InspectionItems）</summary>
        public List<string> Items { get; set; }

        /// <summary>操作人員工號</summary>
        public string OperatorId { get; set; }

        /// <summary>優先等級（v2 新增，Low / Normal / High）</summary>
        public string Priority { get; set; } = "Normal";
    }
}
