using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SKERPAPI.AOI.Controllers.V1;
using SKERPAPI.AOI.Models;
using SKERPAPI.AOI.Services;
using System.Collections.Generic;
using System.Web.Http.Results;
using SKERPAPI.Core.Models;

namespace SKERPAPI.AOI.Tests.Controllers
{
    [TestClass]
    public class AOI01ControllerTests
    {
        private Mock<IAOIService> _mockService;
        private AOI01Controller _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockService = new Mock<IAOIService>();
            _controller = new AOI01Controller(_mockService.Object);
        }

        [TestMethod]
        public void GetStatus_ReturnsOkResult()
        {
            // Arrange
            _mockService.Setup(s => s.GetStatus()).Returns(new { System = "AOI", Status = "Online" });

            // Act
            var result = _controller.GetStatus();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(OkNegotiatedContentResult<ApiResponse<object>>));
        }

        [TestMethod]
        public void GetStatus_CallsServiceOnce()
        {
            // Arrange
            _mockService.Setup(s => s.GetStatus()).Returns(new { Status = "Online" });

            // Act
            _controller.GetStatus();

            // Assert
            _mockService.Verify(s => s.GetStatus(), Times.Once);
        }

        [TestMethod]
        public void Inspect_ValidRequest_ReturnsResult()
        {
            // Arrange
            var request = new AOIInspectionRequest { BatchId = "B-001", StationCode = "ST-01" };
            var expectedResult = new AOIInspectionResult
            {
                InspectionId = "TEST1234",
                BatchId = "B-001",
                Status = "Pass"
            };
            _mockService.Setup(s => s.Inspect(It.IsAny<AOIInspectionRequest>())).Returns(expectedResult);

            // Act
            var result = _controller.Inspect(request);

            // Assert
            Assert.IsNotNull(result);
            _mockService.Verify(s => s.Inspect(It.Is<AOIInspectionRequest>(r => r.BatchId == "B-001")), Times.Once);
        }

        [TestMethod]
        public void GetHistory_ReturnsPaginatedResult()
        {
            // Arrange
            var items = new List<AOIInspectionResult>
            {
                new AOIInspectionResult { InspectionId = "A", Status = "Pass" },
                new AOIInspectionResult { InspectionId = "B", Status = "Fail" }
            };
            _mockService.Setup(s => s.GetInspectionHistory(1, 20)).Returns((items, 2));

            // Act
            var result = _controller.GetHistory(1, 20);

            // Assert
            Assert.IsNotNull(result);
        }
    }
}
