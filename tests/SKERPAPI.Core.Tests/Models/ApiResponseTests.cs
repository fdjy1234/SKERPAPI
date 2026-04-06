using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKERPAPI.Core.Models;

namespace SKERPAPI.Core.Tests.Models
{
    [TestClass]
    public class ApiResponseTests
    {
        [TestMethod]
        public void Constructor_WithData_SetsSuccessTrue()
        {
            // Arrange & Act
            var response = new ApiResponse<string>("test data");

            // Assert
            Assert.IsTrue(response.Success);
            Assert.AreEqual("test data", response.Data);
            Assert.IsNull(response.ErrorMessage);
        }

        [TestMethod]
        public void Ok_SetsSuccessAndData()
        {
            // Arrange & Act
            var response = ApiResponse<int>.Ok(42);

            // Assert
            Assert.IsTrue(response.Success);
            Assert.AreEqual(42, response.Data);
            Assert.IsNotNull(response.TraceId);
            Assert.AreEqual(16, response.TraceId.Length);
        }

        [TestMethod]
        public void Fail_SetsErrorMessage()
        {
            // Arrange & Act
            var response = ApiResponse<object>.Fail("Something went wrong");

            // Assert
            Assert.IsFalse(response.Success);
            Assert.AreEqual("Something went wrong", response.ErrorMessage);
            Assert.IsNull(response.Data);
            Assert.IsNotNull(response.TraceId);
        }

        [TestMethod]
        public void Response_HasTimestamp()
        {
            // Arrange & Act
            var before = DateTime.UtcNow.AddSeconds(-1);
            var response = new ApiResponse<string>("data");
            var after = DateTime.UtcNow.AddSeconds(1);

            // Assert
            Assert.IsTrue(response.Timestamp >= before);
            Assert.IsTrue(response.Timestamp <= after);
        }

        [TestMethod]
        public void Ok_GeneratesUniqueTraceIds()
        {
            // Arrange & Act
            var response1 = ApiResponse<string>.Ok("a");
            var response2 = ApiResponse<string>.Ok("b");

            // Assert
            Assert.AreNotEqual(response1.TraceId, response2.TraceId);
        }
    }
}
