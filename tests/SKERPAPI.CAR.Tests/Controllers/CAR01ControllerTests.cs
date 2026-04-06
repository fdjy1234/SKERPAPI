using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SKERPAPI.CAR.Controllers.V1;
using SKERPAPI.CAR.Models;
using SKERPAPI.CAR.Services;
using System.Collections.Generic;
using System.Web.Http.Results;

namespace SKERPAPI.CAR.Tests.Controllers
{
    [TestClass]
    public class CAR01ControllerTests
    {
        private Mock<ICARService> _mockService;
        private CAR01Controller _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockService = new Mock<ICARService>();
            _controller = new CAR01Controller(_mockService.Object);
        }

        [TestMethod]
        public void GetInfo_ReturnsOk()
        {
            // Act
            var result = _controller.GetInfo();

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void RegisterCar_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CarRegistrationRequest { PlateNumber = "ABC-1234", Brand = "Toyota" };
            var expectedCar = new CarInfo { CarId = "TEST0001", PlateNumber = "ABC-1234", Brand = "Toyota" };
            _mockService.Setup(s => s.RegisterCar(It.IsAny<CarRegistrationRequest>())).Returns(expectedCar);

            // Act
            var result = _controller.RegisterCar(request);

            // Assert
            Assert.IsNotNull(result);
            _mockService.Verify(s => s.RegisterCar(It.Is<CarRegistrationRequest>(r => r.PlateNumber == "ABC-1234")), Times.Once);
        }

        [TestMethod]
        public void GetCar_NotFound_Returns404()
        {
            // Arrange
            _mockService.Setup(s => s.GetCarInfo("FAKE")).Returns((CarInfo)null);

            // Act
            var result = _controller.GetCar("FAKE");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(NegotiatedContentResult<Core.Models.ApiResponse<object>>));
        }

        [TestMethod]
        public void GetCar_Existing_ReturnsOk()
        {
            // Arrange
            var car = new CarInfo { CarId = "CAR001", PlateNumber = "XYZ-999" };
            _mockService.Setup(s => s.GetCarInfo("CAR001")).Returns(car);

            // Act
            var result = _controller.GetCar("CAR001");

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetCars_ReturnsPaginatedResult()
        {
            // Arrange
            var cars = new List<CarInfo> { new CarInfo { CarId = "C1" }, new CarInfo { CarId = "C2" } };
            _mockService.Setup(s => s.GetAllCars(1, 20)).Returns((cars, 2));

            // Act
            var result = _controller.GetCars(1, 20);

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
