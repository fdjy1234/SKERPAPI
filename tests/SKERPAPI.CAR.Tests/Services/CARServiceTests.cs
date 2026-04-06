using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKERPAPI.CAR.Models;
using SKERPAPI.CAR.Services;

namespace SKERPAPI.CAR.Tests.Services
{
    [TestClass]
    public class CARServiceTests
    {
        private CARService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new CARService();
        }

        [TestMethod]
        public void RegisterCar_ValidRequest_ReturnsCar()
        {
            // Arrange
            var request = new CarRegistrationRequest
            {
                PlateNumber = "ABC-1234",
                Brand = "Toyota",
                Model = "Camry",
                Color = "White",
                Year = 2026,
                OwnerId = "EMP-001"
            };

            // Act
            var car = _service.RegisterCar(request);

            // Assert
            Assert.IsNotNull(car);
            Assert.AreEqual("ABC-1234", car.PlateNumber);
            Assert.AreEqual("Toyota", car.Brand);
            Assert.AreEqual("Active", car.Status);
            Assert.IsNotNull(car.CarId);
            Assert.AreEqual(8, car.CarId.Length);
        }

        [TestMethod]
        public void GetCarInfo_NonExisting_ReturnsNull()
        {
            // Act
            var car = _service.GetCarInfo("NONEXISTENT");

            // Assert
            Assert.IsNull(car);
        }

        [TestMethod]
        public void GetCarInfo_ExistingCar_ReturnsCar()
        {
            // Arrange
            var registered = _service.RegisterCar(new CarRegistrationRequest
            {
                PlateNumber = "XYZ-5678",
                Brand = "Honda"
            });

            // Act
            var found = _service.GetCarInfo(registered.CarId);

            // Assert
            Assert.IsNotNull(found);
            Assert.AreEqual("XYZ-5678", found.PlateNumber);
        }

        [TestMethod]
        public void GetAllCars_ReturnsPaginatedResults()
        {
            // Arrange
            var svc = new CARService();
            for (int i = 0; i < 5; i++)
            {
                svc.RegisterCar(new CarRegistrationRequest
                {
                    PlateNumber = $"PLT-{i}",
                    Brand = "Test"
                });
            }

            // Act
            var (items, totalCount) = svc.GetAllCars(1, 3);

            // Assert
            Assert.IsTrue(totalCount >= 5);
            Assert.IsTrue(items.Count <= 3);
        }
    }
}
