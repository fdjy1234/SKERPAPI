using System.ComponentModel.DataAnnotations;

namespace SKERPAPI.CAR.Models
{
    /// <summary>
    /// 車輛註冊請求模型
    /// </summary>
    public class CarRegistrationRequest
    {
        [Required(ErrorMessage = "PlateNumber is required.")]
        public string PlateNumber { get; set; }

        [Required(ErrorMessage = "Brand is required.")]
        public string Brand { get; set; }

        public string Model { get; set; }
        public string Color { get; set; }
        public int Year { get; set; }
        public string OwnerId { get; set; }
    }
}
