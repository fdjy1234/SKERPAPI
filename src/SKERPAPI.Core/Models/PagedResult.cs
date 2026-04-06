using System.Collections.Generic;

namespace SKERPAPI.Core.Models
{
    /// <summary>
    /// 分頁結果模型，用於列表型 API 回應。
    /// </summary>
    /// <typeparam name="T">項目型別</typeparam>
    /// <remarks>
    /// 使用方式：
    ///   return ApiPagedOk(items, totalCount, page, pageSize);
    /// </remarks>
    public class PagedResult<T>
    {
        /// <summary>當前頁面的項目清單</summary>
        public List<T> Items { get; set; }

        /// <summary>符合條件的總筆數</summary>
        public int TotalCount { get; set; }

        /// <summary>目前頁碼 (從 1 開始)</summary>
        public int Page { get; set; }

        /// <summary>每頁筆數</summary>
        public int PageSize { get; set; }

        /// <summary>總頁數</summary>
        public int TotalPages { get; set; }
    }
}
