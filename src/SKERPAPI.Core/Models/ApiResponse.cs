using System;

namespace SKERPAPI.Core.Models
{
    /// <summary>
    /// 統一 API 回應格式。
    /// 所有 API 端點的回應都會透過此模型封裝，確保格式一致。
    /// </summary>
    /// <typeparam name="T">回應資料型別</typeparam>
    /// <remarks>
    /// 回應範例 (成功)：
    /// {
    ///   "success": true,
    ///   "data": { ... },
    ///   "errorMessage": null,
    ///   "traceId": "abc123...",
    ///   "timestamp": "2026-04-05T01:00:00Z"
    /// }
    /// </remarks>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public string TraceId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ApiResponse() { }

        /// <summary>
        /// 建立成功回應
        /// </summary>
        public ApiResponse(T data)
        {
            Success = true;
            Data = data;
        }

        /// <summary>
        /// 靜態工廠：建立成功回應
        /// </summary>
        public static ApiResponse<T> Ok(T data)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                TraceId = Guid.NewGuid().ToString("N").Substring(0, 16)
            };
        }

        /// <summary>
        /// 靜態工廠：建立失敗回應
        /// </summary>
        public static ApiResponse<T> Fail(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                ErrorMessage = message,
                TraceId = Guid.NewGuid().ToString("N").Substring(0, 16)
            };
        }
    }
}
