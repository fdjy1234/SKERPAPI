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

        // --- v1 端點測試 ---

        [TestMethod]
        public void AOI_V1_GetStatus_Returns200()
        {
            var response = _fixture.Client.GetAsync("/webapi/aoi/v1/aoi01/status").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var json = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(json["success"].Value<bool>());
        }

        [TestMethod]
        public void AOI_V1_Inspect_ValidRequest_Returns200()
        {
            var requestBody = new { BatchId = "E2E-BATCH-001", StationCode = "E2E-ST-01" };
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = _fixture.Client.PostAsync("/webapi/aoi/v1/aoi01/inspect", content).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var json = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(json["success"].Value<bool>());
            Assert.IsNotNull(json["data"]["inspectionId"]);
        }

        [TestMethod]
        public void AOI_V1_Inspect_MissingBatchId_Returns400()
        {
            var requestBody = new { StationCode = "E2E-ST-01" };
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = _fixture.Client.PostAsync("/webapi/aoi/v1/aoi01/inspect", content).Result;
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public void AOI_V1_GetHistory_Returns200()
        {
            var response = _fixture.Client.GetAsync("/webapi/aoi/v1/aoi01/history?page=1&pageSize=10").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var json = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(json["success"].Value<bool>());
        }

        [TestMethod]
        public void AOI_V1_GetDevices_Returns200()
        {
            var response = _fixture.Client.GetAsync("/webapi/aoi/v1/aoi02/devices").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        // --- v2 端點測試（示範 breaking-change）---

        [TestMethod]
        public void AOI_V2_GetStatus_Returns200()
        {
            var response = _fixture.Client.GetAsync("/webapi/aoi/v2/aoi01/status").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var json = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(json["success"].Value<bool>());
        }

        [TestMethod]
        public void AOI_V2_Inspect_WithV2Model_Returns200()
        {
            // v2 使用 WorkstationCode 與 Items（breaking change from v1 StationCode/InspectionItems）
            var requestBody = new
            {
                BatchId = "E2E-V2-BATCH-001",
                WorkstationCode = "E2E-WS-01",
                Items = new[] { "SOLDER", "COMPONENT" },
                Priority = "High"
            };
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = _fixture.Client.PostAsync("/webapi/aoi/v2/aoi01/inspect", content).Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var json = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            Assert.IsTrue(json["success"].Value<bool>());
            Assert.IsNotNull(json["data"]["inspectionId"]);
        }

        [TestMethod]
        public void AOI_V2_Inspect_MissingWorkstationCode_Returns400()
        {
            // v2 必填欄位是 WorkstationCode，不是 StationCode
            var requestBody = new { BatchId = "E2E-V2-BATCH-002" };
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = _fixture.Client.PostAsync("/webapi/aoi/v2/aoi01/inspect", content).Result;
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public void AOI_V2_GetHistory_Returns200()
        {
            var response = _fixture.Client.GetAsync("/webapi/aoi/v2/aoi01/history").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        // --- 版本協商測試 ---

        [TestMethod]
        public void AOI_QueryString_Version_V1_Returns200()
        {
            var response = _fixture.Client.GetAsync("/webapi/aoi/v1/aoi01/status?api-version=1.0").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public void AOI_ResponseHeader_ContainsSupportedVersions()
        {
            var response = _fixture.Client.GetAsync("/webapi/aoi/v1/aoi01/status").Result;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            // ReportApiVersions = true 時，回應 header 中會包含 api-supported-versions
            Assert.IsTrue(response.Headers.Contains("api-supported-versions"),
                "Response should contain 'api-supported-versions' header when ReportApiVersions is enabled.");
        }

        // --- RBAC 安全性測試 ---

        [TestMethod]
        public void AOI_NoApiKey_Returns401()
        {
            using (var client = _fixture.CreateClientWithKey(apiKey: null))
            {
                var response = client.GetAsync("/webapi/aoi/v1/aoi01/status").Result;
                Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [TestMethod]
        public void AOI_InvalidApiKey_Returns401()
        {
            using (var client = _fixture.CreateClientWithKey("INVALID_KEY"))
            {
                var response = client.GetAsync("/webapi/aoi/v1/aoi01/status").Result;
                Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [TestMethod]
        public void AOI_ValidKeyWithNoPermission_Returns403()
        {
            using (var client = _fixture.CreateClientWithKey("NO_PERMISSION_KEY"))
            {
                var response = client.GetAsync("/webapi/aoi/v1/aoi01/status").Result;
                Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
            }
        }
    }
}
