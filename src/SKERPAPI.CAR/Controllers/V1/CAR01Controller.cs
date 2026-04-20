using System.Web.Http;
using Asp.Versioning;
using SKERPAPI.Core.Controllers;
using SKERPAPI.Core.Filters;
using SKERPAPI.Core.Permissions;
using SKERPAPI.CAR.Services;
using SKERPAPI.CAR.Models;

namespace SKERPAPI.CAR.Controllers.V1
{
    /// <summary>
    /// CAR01 控制器 - 車輛管理主要端點
    /// </summary>
    [ApiVersion("1.0")]
    [RoutePrefix("webapi/car/v{version:apiVersion}/car01")]
    public class CAR01Controller : ApiBaseController
    {
        private readonly ICARService _carService;

        public CAR01Controller(ICARService carService)
        {
            _carService = carService;
        }

        /// <summary>
        /// 取得 CAR 系統資訊
        /// </summary>
        [HttpGet, Route("info")]
        [RbacAuthorize(Permission = RbacPermissions.Car.SystemRead)]
        public IHttpActionResult GetInfo()
        {
            return ApiOk(new
            {
                System = "CAR",
                Module = "CAR01",
                Version = "2.0.0",
                Description = "Car Management System"
            });
        }

        /// <summary>
        /// 註冊新車輛
        /// </summary>
        [HttpPost, Route("register")]        [RbacAuthorize(Permission = RbacPermissions.Car.VehicleRegister)]        public IHttpActionResult RegisterCar([FromBody] CarRegistrationRequest request)
        {
            if (!ValidateRequest())
            {
                return ApiFail("Validation failed.");
            }

            var car = _carService.RegisterCar(request);
            return ApiOk(car);
        }

        /// <summary>
        /// 取得車輛清單 (分頁)
        /// </summary>
        [HttpGet, Route("list")]        [RbacAuthorize(Permission = RbacPermissions.Car.VehicleRead)]        public IHttpActionResult GetCars(int page = 1, int pageSize = 20)
        {
            var result = _carService.GetAllCars(page, pageSize);
            return ApiPagedOk(result.Items, result.TotalCount, page, pageSize);
        }

        /// <summary>
        /// 取得指定車輛資訊
        /// </summary>
        [HttpGet, Route("{carId}")]        [RbacAuthorize(Permission = RbacPermissions.Car.VehicleRead)]        public IHttpActionResult GetCar(string carId)
        {
            var car = _carService.GetCarInfo(carId);
            if (car == null)
            {
                return ApiNotFound($"Car '{carId}' not found.");
            }
            return ApiOk(car);
        }
    }
}
