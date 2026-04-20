using System;
using System.Collections.Generic;
using System.Web.Http;
using Asp.Versioning;
using SKERPAPI.Core.Controllers;
using SKERPAPI.Core.Filters;
using SKERPAPI.Core.Permissions;

namespace SKERPAPI.AOI.Controllers.V1
{
    /// <summary>
    /// AOI02 控制器 - AOI 設備管理端點
    /// </summary>
    [ApiVersion("1.0")]
    [RoutePrefix("webapi/aoi/v{version:apiVersion}/aoi02")]
    public class AOI02Controller : ApiBaseController
    {
        /// <summary>
        /// 取得 AOI 設備清單
        /// </summary>
        [HttpGet, Route("devices")]
        [RbacAuthorize(Permission = RbacPermissions.Aoi.DeviceRead)]
        public IHttpActionResult GetDevices()
        {
            var devices = new List<object>
            {
                new { DeviceId = "AOI-DEV-001", Name = "AOI Scanner A", Status = "Active", Location = "Line-1" },
                new { DeviceId = "AOI-DEV-002", Name = "AOI Scanner B", Status = "Maintenance", Location = "Line-2" },
                new { DeviceId = "AOI-DEV-003", Name = "AOI Scanner C", Status = "Active", Location = "Line-3" }
            };
            return ApiOk(devices);
        }

        /// <summary>
        /// 取得單一 AOI 設備資訊
        /// </summary>
        [HttpGet, Route("devices/{id}")]
        [RbacAuthorize(Permission = RbacPermissions.Aoi.DeviceRead)]
        public IHttpActionResult GetDevice(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return ApiFail("Device ID is required.");
            }

            return ApiOk(new
            {
                DeviceId = id,
                Name = "AOI Scanner " + id,
                Status = "Active",
                FirmwareVersion = "2.1.0",
                LastCalibration = DateTime.UtcNow.AddDays(-7)
            });
        }
    }
}
