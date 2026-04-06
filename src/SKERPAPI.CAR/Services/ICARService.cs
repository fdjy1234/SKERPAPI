using System.Collections.Generic;
using SKERPAPI.CAR.Models;

namespace SKERPAPI.CAR.Services
{
    /// <summary>
    /// CAR 車輛管理服務介面
    /// </summary>
    public interface ICARService
    {
        CarInfo GetCarInfo(string carId);
        CarInfo RegisterCar(CarRegistrationRequest request);
        (List<CarInfo> Items, int TotalCount) GetAllCars(int page, int pageSize);
    }
}
