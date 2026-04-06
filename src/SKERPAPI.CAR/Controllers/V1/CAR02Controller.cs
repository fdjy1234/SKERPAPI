using System;
using System.Collections.Generic;
using System.Web.Http;
using SKERPAPI.Core.Controllers;

namespace SKERPAPI.CAR.Controllers.V1
{
    /// <summary>
    /// CAR02 控制器 - 車輛維修保養管理
    /// </summary>
    [RoutePrefix("webapi/car/v1/car02")]
    public class CAR02Controller : ApiBaseController
    {
        /// <summary>
        /// 取得維修紀錄
        /// </summary>
        [HttpGet, Route("maintenance/{carId}")]
        public IHttpActionResult GetMaintenanceRecords(string carId)
        {
            var records = new List<object>
            {
                new { RecordId = "MR-001", CarId = carId, Type = "Oil Change", Date = DateTime.UtcNow.AddDays(-30), Cost = 1500 },
                new { RecordId = "MR-002", CarId = carId, Type = "Tire Rotation", Date = DateTime.UtcNow.AddDays(-60), Cost = 800 }
            };
            return ApiOk(records);
        }

        /// <summary>
        /// 新增維修紀錄
        /// </summary>
        [HttpPost, Route("maintenance")]
        public IHttpActionResult AddMaintenanceRecord([FromBody] object data)
        {
            return ApiOk(new
            {
                RecordId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                Message = "Maintenance record created."
            });
        }
    }
}
