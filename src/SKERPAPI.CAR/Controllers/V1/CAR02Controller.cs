using System;
using System.Collections.Generic;
using System.Web.Http;
using Asp.Versioning;
using SKERPAPI.Core.Controllers;
using SKERPAPI.Core.Filters;
using SKERPAPI.Core.Permissions;

namespace SKERPAPI.CAR.Controllers.V1
{
    /// <summary>
    /// CAR02 控制器 - 車輛維修保養管理
    /// </summary>
    [ApiVersion("1.0")]
    [RoutePrefix("webapi/car/v{version:apiVersion}/car02")]
    public class CAR02Controller : ApiBaseController
    {
        /// <summary>
        /// 取得維修紀錄
        /// </summary>
        [HttpGet, Route("maintenance/{carId}")]
        [RbacAuthorize(Permission = RbacPermissions.Car.MaintenanceRead)]
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
        [RbacAuthorize(Permission = RbacPermissions.Car.MaintenanceCreate)]
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
