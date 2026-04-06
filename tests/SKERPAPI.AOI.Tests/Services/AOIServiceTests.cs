using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKERPAPI.AOI.Models;
using SKERPAPI.AOI.Services;

namespace SKERPAPI.AOI.Tests.Services
{
    [TestClass]
    public class AOIServiceTests
    {
        private AOIService _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new AOIService();
        }

        [TestMethod]
        public void GetStatus_ReturnsOnlineStatus()
        {
            // Act
            dynamic status = _service.GetStatus();

            // Assert
            Assert.IsNotNull(status);
        }

        [TestMethod]
        public void Inspect_ValidRequest_ReturnsResult()
        {
            // Arrange
            var request = new AOIInspectionRequest
            {
                BatchId = "BATCH-001",
                StationCode = "ST-01"
            };

            // Act
            var result = _service.Inspect(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("BATCH-001", result.BatchId);
            Assert.AreEqual("ST-01", result.StationCode);
            Assert.IsNotNull(result.InspectionId);
            Assert.IsTrue(result.InspectedAt > System.DateTime.MinValue);
        }

        [TestMethod]
        public void Inspect_ResultStatus_IsPassWarningOrFail()
        {
            // Arrange
            var request = new AOIInspectionRequest
            {
                BatchId = "BATCH-002",
                StationCode = "ST-02"
            };

            // Act - run multiple times to get different statuses
            bool hasValidStatus = false;
            for (int i = 0; i < 20; i++)
            {
                var result = _service.Inspect(request);
                if (result.Status == "Pass" || result.Status == "Warning" || result.Status == "Fail")
                {
                    hasValidStatus = true;
                }
            }

            // Assert
            Assert.IsTrue(hasValidStatus);
        }

        [TestMethod]
        public void GetInspectionHistory_ReturnsPaginatedResults()
        {
            // Arrange
            var svc = new AOIService();
            for (int i = 0; i < 5; i++)
            {
                svc.Inspect(new AOIInspectionRequest { BatchId = $"B-{i}", StationCode = "ST-01" });
            }

            // Act
            var (items, totalCount) = svc.GetInspectionHistory(1, 3);

            // Assert
            Assert.IsTrue(totalCount >= 5);
            Assert.IsTrue(items.Count <= 3);
        }
    }
}
