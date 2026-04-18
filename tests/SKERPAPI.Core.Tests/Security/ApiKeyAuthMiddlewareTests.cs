using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKERPAPI.Core.Security.Authentication;

namespace SKERPAPI.Core.Tests.Security
{
    /// <summary>
    /// ApiKeyAuthMiddleware 單元測試。
    /// 測試 API Key 驗證邏輯（不含 OWIN Pipeline，僅測試靜態驗證方法）。
    /// </summary>
    [TestClass]
    public class ApiKeyAuthMiddlewareTests
    {
        [TestMethod]
        public void ValidateApiKey_EmptyConfig_ReturnsValidWithDevClient()
        {
            // Arrange - 未設定任何 ApiKey（開發環境）
            // 注：ConfigurationManager 在測試環境中通常不含設定

            // Act
            var result = ApiKeyAuthMiddleware.ValidateApiKey("any-key");

            // Assert - 開發模式應放行
            Assert.IsTrue(result.IsValid, "未配置金鑰時應視為有效（開發環境）");
            Assert.AreEqual("dev-client", result.ClientId);
        }

        [TestMethod]
        public void ValidateApiKey_NullKey_ReturnsValidInDevMode()
        {
            // Arrange & Act
            var result = ApiKeyAuthMiddleware.ValidateApiKey(null);

            // Assert
            // 空 key 在無配置的情況下也應放行（開發環境）
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void ValidateApiKey_EmptyKey_ReturnsValidInDevMode()
        {
            // Arrange & Act
            var result = ApiKeyAuthMiddleware.ValidateApiKey("");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void ApiKeyValidationResult_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var result = new ApiKeyValidationResult();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNull(result.ClientId);
        }

        [TestMethod]
        public void ApiKeyValidationResult_SetProperties_Persisted()
        {
            // Arrange & Act
            var result = new ApiKeyValidationResult
            {
                IsValid = true,
                ClientId = "test-client"
            };

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("test-client", result.ClientId);
        }
    }
}
