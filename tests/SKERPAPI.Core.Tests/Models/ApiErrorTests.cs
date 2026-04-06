using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKERPAPI.Core.Models;

namespace SKERPAPI.Core.Tests.Models
{
    [TestClass]
    public class ApiErrorTests
    {
        [TestMethod]
        public void Constructor_WithParams_SetsProperties()
        {
            // Arrange & Act
            var error = new ApiError("VAL_ERROR", "Validation failed", new { Field = "Name" });

            // Assert
            Assert.AreEqual("VAL_ERROR", error.Error);
            Assert.AreEqual("Validation failed", error.Message);
            Assert.IsNotNull(error.Details);
        }

        [TestMethod]
        public void Constructor_DefaultDetails_IsNull()
        {
            // Arrange & Act
            var error = new ApiError("AUTH_FAILED", "Unauthorized");

            // Assert
            Assert.AreEqual("AUTH_FAILED", error.Error);
            Assert.IsNull(error.Details);
        }

        [TestMethod]
        public void Parameterless_Constructor_AllNull()
        {
            // Arrange & Act
            var error = new ApiError();

            // Assert
            Assert.IsNull(error.Error);
            Assert.IsNull(error.Message);
            Assert.IsNull(error.Details);
        }
    }
}
