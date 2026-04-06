using System;
using System.Collections.Generic;
using System.Linq;
using SKERPAPI.AOI.Models;

namespace SKERPAPI.AOI.Services
{
    /// <summary>
    /// AOI 檢測服務實作（Demo 用，使用記憶體資料）
    /// </summary>
    public class AOIService : IAOIService
    {
        private static readonly List<AOIInspectionResult> _history = new List<AOIInspectionResult>();
        private static readonly object _lock = new object();

        public object GetStatus()
        {
            return new
            {
                System = "AOI",
                Status = "Online",
                Version = "2.0.0",
                Timestamp = DateTime.UtcNow,
                TotalInspections = _history.Count
            };
        }

        public AOIInspectionResult Inspect(AOIInspectionRequest request)
        {
            var rng = new Random();
            var defectCount = rng.Next(0, 5);
            var defects = new List<string>();

            for (int i = 1; i <= defectCount; i++)
            {
                defects.Add($"Defect-{i}: Surface scratch at zone {rng.Next(1, 10)}");
            }

            var result = new AOIInspectionResult
            {
                InspectionId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                BatchId = request.BatchId,
                StationCode = request.StationCode,
                Status = defectCount == 0 ? "Pass" : defectCount <= 2 ? "Warning" : "Fail",
                DefectCount = defectCount,
                Defects = defects,
                InspectedAt = DateTime.UtcNow
            };

            lock (_lock)
            {
                _history.Add(result);
            }
            return result;
        }

        public (List<AOIInspectionResult> Items, int TotalCount) GetInspectionHistory(int page, int pageSize)
        {
            lock (_lock)
            {
                var total = _history.Count;
                var items = _history
                    .OrderByDescending(r => r.InspectedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (items, total);
            }
        }
    }
}
