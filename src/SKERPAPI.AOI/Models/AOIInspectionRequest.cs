using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SKERPAPI.AOI.Models
{
    /// <summary>
    /// AOI 檢測請求模型
    /// </summary>
    public class AOIInspectionRequest
    {
        /// <summary>產品批次編號</summary>
        [Required(ErrorMessage = "BatchId is required.")]
        public string BatchId { get; set; }

        /// <summary>工站代碼</summary>
        [Required(ErrorMessage = "StationCode is required.")]
        public string StationCode { get; set; }

        /// <summary>檢測項目清單</summary>
        public List<string> InspectionItems { get; set; }

        /// <summary>操作人員工號</summary>
        public string OperatorId { get; set; }
    }
}
