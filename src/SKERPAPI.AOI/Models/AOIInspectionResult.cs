using System;
using System.Collections.Generic;

namespace SKERPAPI.AOI.Models
{
    /// <summary>
    /// AOI 檢測結果模型
    /// </summary>
    public class AOIInspectionResult
    {
        /// <summary>檢測結果 ID</summary>
        public string InspectionId { get; set; }

        /// <summary>批次編號</summary>
        public string BatchId { get; set; }

        /// <summary>檢測狀態 (Pass / Fail / Warning)</summary>
        public string Status { get; set; }

        /// <summary>缺陷數量</summary>
        public int DefectCount { get; set; }

        /// <summary>缺陷詳細資訊</summary>
        public List<string> Defects { get; set; }

        /// <summary>檢測時間</summary>
        public DateTime InspectedAt { get; set; }

        /// <summary>工站代碼</summary>
        public string StationCode { get; set; }
    }
}
