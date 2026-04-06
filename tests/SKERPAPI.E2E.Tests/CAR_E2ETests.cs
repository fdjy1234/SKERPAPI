using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SKERPAPI.E2E.Tests
{
    [TestClass]
    public class CAR_E2ETests
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
        public void CAR_GetInfo_Returns200()
        {
            // Act
            var response = _fixture.Client.GetAsync("/webapi/car/v1/car01/info").Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadAsStringAsync().Result;
            var json = JObject.Parse(content);
            Assert.IsTrue(json["success"].Value<bool>());
        }

        [TestMethod]
        public void CAR_RegisterCar_Returns200()
        {
            // Arrange
            var requestBody = new
            {
                PlateNumber = "E2E-PLT-001",
                Brand = "Tesla",
                Model = "Model 3",
                Color = "Red",
                Year = 2026,
                OwnerId = "E2E-OWNER"
            };
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = _fixture.Client.PostAsync("/webapi/car/v1/car01/register", jsonContent).Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var content = response.Content.ReadAsStringAsync().Result;
            var json = JObject.Parse(content);
            Assert.IsTrue(json["success"].Value<bool>());
            Assert.IsNotNull(json["data"]["carId"]);
        }

        [TestMethod]
        public void CAR_GetCarList_Returns200()
        {
            // Act
            var response = _fixture.Client.GetAsync("/webapi/car/v1/car01/list?page=1&pageSize=10").Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public void CAR_GetMaintenance_Returns200()
        {
            // Act
            var response = _fixture.Client.GetAsync("/webapi/car/v1/car02/maintenance/TEST001").Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
