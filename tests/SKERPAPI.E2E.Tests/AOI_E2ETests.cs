using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SKERPAPI.E2E.Tests
{
    [TestClass]
    public class AOI_E2ETests
    {
        private static TestServerFixture _fixture;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _fixture = new TestServerFixture();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _fixture?.Dispose();
        }

        [TestMethod]
        public void AOI_GetStatus_Returns200()
        {
            // Act
            var response = _fixture.Client.GetAsync("/webapi/aoi/v1/aoi01/status").Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadAsStringAsync().Result;
            var json = JObject.Parse(content);
            Assert.IsTrue(json["success"].Value<bool>());
        }

        [TestMethod]
        public void AOI_Inspect_ValidRequest_Returns200()
        {
            // Arrange
            var requestBody = new
            {
                BatchId = "E2E-BATCH-001",
                StationCode = "E2E-ST-01"
            };
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = _fixture.Client.PostAsync("/webapi/aoi/v1/aoi01/inspect", jsonContent).Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadAsStringAsync().Result;
            var json = JObject.Parse(content);
            Assert.IsTrue(json["success"].Value<bool>());
            Assert.IsNotNull(json["data"]["inspectionId"]);
        }

        [TestMethod]
        public void AOI_GetHistory_Returns200()
        {
            // Act
            var response = _fixture.Client.GetAsync("/webapi/aoi/v1/aoi01/history?page=1&pageSize=10").Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadAsStringAsync().Result;
            var json = JObject.Parse(content);
            Assert.IsTrue(json["success"].Value<bool>());
        }

        [TestMethod]
        public void AOI_GetDevices_Returns200()
        {
            // Act
            var response = _fixture.Client.GetAsync("/webapi/aoi/v1/aoi02/devices").Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
