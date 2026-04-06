using System;
using System.Collections.Generic;
using System.Linq;
using SKERPAPI.CAR.Models;

namespace SKERPAPI.CAR.Services
{
    /// <summary>
    /// CAR 車輛管理服務實作（Demo 用，使用記憶體資料）
    /// </summary>
    public class CARService : ICARService
    {
        private static readonly List<CarInfo> _cars = new List<CarInfo>();
        private static readonly object _lock = new object();

        public CarInfo RegisterCar(CarRegistrationRequest request)
        {
            var car = new CarInfo
            {
                CarId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                PlateNumber = request.PlateNumber,
                Brand = request.Brand,
                Model = request.Model,
                Color = request.Color,
                Year = request.Year,
                OwnerId = request.OwnerId,
                RegisteredAt = DateTime.UtcNow,
                Status = "Active"
            };

            lock (_lock)
            {
                _cars.Add(car);
            }
            return car;
        }

        public CarInfo GetCarInfo(string carId)
        {
            lock (_lock)
            {
                return _cars.FirstOrDefault(c => c.CarId == carId);
            }
        }

        public (List<CarInfo> Items, int TotalCount) GetAllCars(int page, int pageSize)
        {
            lock (_lock)
            {
                var total = _cars.Count;
                var items = _cars
                    .OrderByDescending(c => c.RegisteredAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                return (items, total);
            }
        }
    }
}
