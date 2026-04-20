using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SKERPAPI.AOI.Controllers.V2;
using SKERPAPI.AOI.Models;
using SKERPAPI.AOI.Services;
using System.Collections.Generic;
using System.Web.Http.Results;
using SKERPAPI.Core.Models;

namespace SKERPAPI.AOI.Tests.Controllers
{
    /// <summary>
    /// AOI01 v2 Controller 單元測試
    /// 驗證 breaking-change 欄位（WorkstationCode / Items / Priority）的行為
    /// </summary>
    [TestClass]
    public class AOI01V2ControllerTests
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
        public void GetStatus_V2_ReturnsOkResult()
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
        public void Inspect_V2_ValidRequest_MapsWorkstationCodeToStationCode()
        {
            // Arrange
            var v2Request = new AOIInspectionV2Request
            {
                BatchId = "B-V2-001",
                WorkstationCode = "WS-01",
                Items = new List<string> { "SOLDER", "COMPONENT" },
                Priority = "High"
            };
            var expectedResult = new AOIInspectionResult
            {
                InspectionId = "TESTV200",
                BatchId = "B-V2-001",
                Status = "Pass"
            };
            _mockService.Setup(s => s.Inspect(It.IsAny<AOIInspectionRequest>())).Returns(expectedResult);

            // Act
            var result = _controller.Inspect(v2Request);

            // Assert
            Assert.IsNotNull(result);
            // 驗證 WorkstationCode 正確適配為 StationCode 傳入服務
            _mockService.Verify(s => s.Inspect(It.Is<AOIInspectionRequest>(r =>
                r.BatchId == "B-V2-001" &&
                r.StationCode == "WS-01"
            )), Times.Once);
        }

        [TestMethod]
        public void Inspect_V2_ValidRequest_MapsItemsToInspectionItems()
        {
            // Arrange
            var items = new List<string> { "ITEM-A", "ITEM-B" };
            var v2Request = new AOIInspectionV2Request
            {
                BatchId = "B-V2-002",
                WorkstationCode = "WS-02",
                Items = items
            };
            _mockService.Setup(s => s.Inspect(It.IsAny<AOIInspectionRequest>()))
                .Returns(new AOIInspectionResult { InspectionId = "X", Status = "Pass" });

            // Act
            _controller.Inspect(v2Request);

            // Assert — Items 對應到 InspectionItems
            _mockService.Verify(s => s.Inspect(It.Is<AOIInspectionRequest>(r =>
                r.InspectionItems == items
            )), Times.Once);
        }

        [TestMethod]
        public void Inspect_V2_CallsServiceOnce()
        {
            // Arrange
            var v2Request = new AOIInspectionV2Request
            {
                BatchId = "B-V2-003",
                WorkstationCode = "WS-03"
            };
            _mockService.Setup(s => s.Inspect(It.IsAny<AOIInspectionRequest>()))
                .Returns(new AOIInspectionResult { InspectionId = "Y", Status = "Pass" });

            // Act
            _controller.Inspect(v2Request);

            // Assert
            _mockService.Verify(s => s.Inspect(It.IsAny<AOIInspectionRequest>()), Times.Once);
        }

        [TestMethod]
        public void GetHistory_V2_ReturnsPaginatedResult()
        {
            // Arrange
            var items = new List<AOIInspectionResult>
            {
                new AOIInspectionResult { InspectionId = "V2-A", Status = "Pass" }
            };
            _mockService.Setup(s => s.GetInspectionHistory(1, 20)).Returns((items, 1));

            // Act
            var result = _controller.GetHistory(1, 20);

            // Assert
            Assert.IsNotNull(result);
            _mockService.Verify(s => s.GetInspectionHistory(1, 20), Times.Once);
        }

        [TestMethod]
        public void Inspect_V2_DefaultPriority_IsNormal()
        {
            // Arrange — 不傳 Priority，預設應為 "Normal"
            var v2Request = new AOIInspectionV2Request
            {
                BatchId = "B-V2-004",
                WorkstationCode = "WS-04"
            };

            // Assert
            Assert.AreEqual("Normal", v2Request.Priority);
        }
    }
}
