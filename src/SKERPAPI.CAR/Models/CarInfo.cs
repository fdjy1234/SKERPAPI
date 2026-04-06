using System;

namespace SKERPAPI.CAR.Models
{
    /// <summary>
    /// 車輛資訊模型
    /// </summary>
    public class CarInfo
    {
        public string CarId { get; set; }
        public string PlateNumber { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }
        public int Year { get; set; }
        public string OwnerId { get; set; }
        public DateTime RegisteredAt { get; set; }
        public string Status { get; set; }
    }
}
