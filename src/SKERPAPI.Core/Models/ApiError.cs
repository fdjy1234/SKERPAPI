namespace SKERPAPI.Core.Models
{
    /// <summary>
    /// 結構化錯誤模型，用於詳細的錯誤回報
    /// </summary>
    public class ApiError
    {
        /// <summary>錯誤代碼 (例如 "VALIDATION_ERROR", "AUTH_FAILED")</summary>
        public string Error { get; set; }

        /// <summary>人類可讀的錯誤訊息</summary>
        public string Message { get; set; }

        /// <summary>額外的錯誤細節 (例如欄位驗證錯誤清單)</summary>
        public object Details { get; set; }

        public ApiError() { }

        public ApiError(string errorKey, string message, object details = null)
        {
            Error = errorKey;
            Message = message;
            Details = details;
        }
    }
}
