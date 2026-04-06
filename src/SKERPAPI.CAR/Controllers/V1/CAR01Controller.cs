using System.Web.Http;
using SKERPAPI.Core.Controllers;
using SKERPAPI.CAR.Services;
using SKERPAPI.CAR.Models;

namespace SKERPAPI.CAR.Controllers.V1
{
    /// <summary>
    /// CAR01 控制器 - 車輛管理主要端點
    /// </summary>
    [RoutePrefix("webapi/car/v1/car01")]
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
        [HttpPost, Route("register")]
        public IHttpActionResult RegisterCar([FromBody] CarRegistrationRequest request)
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
        [HttpGet, Route("list")]
        public IHttpActionResult GetCars(int page = 1, int pageSize = 20)
        {
            var result = _carService.GetAllCars(page, pageSize);
            return ApiPagedOk(result.Items, result.TotalCount, page, pageSize);
        }

        /// <summary>
        /// 取得指定車輛資訊
        /// </summary>
        [HttpGet, Route("{carId}")]
        public IHttpActionResult GetCar(string carId)
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
