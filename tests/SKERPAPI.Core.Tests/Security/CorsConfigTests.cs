using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKERPAPI.Core.Security;
using SKERPAPI.Core.Security.Cors;

namespace SKERPAPI.Core.Tests.Security
{
    /// <summary>
    /// CORS 配置與 SecurityConstants 單元測試。
    /// </summary>
    [TestClass]
    public class CorsConfigTests
    {
        [TestMethod]
        public void SecurityConstants_ApiKeyHeaderName_IsCorrect()
        {
            Assert.AreEqual("X-Api-Key", SecurityConstants.ApiKeyHeaderName);
        }

        [TestMethod]
        public void SecurityConstants_BearerScheme_IsCorrect()
        {
            Assert.AreEqual("Bearer", SecurityConstants.BearerScheme);
        }

        [TestMethod]
        public void SecurityConstants_PermissionClaimType_IsCorrect()
        {
            Assert.AreEqual("permission", SecurityConstants.PermissionClaimType);
        }

        [TestMethod]
        public void SecurityConstants_AllAppSettingsKeys_AreNotNullOrEmpty()
        {
            // 確保所有設定 Key 都有值
            Assert.IsFalse(string.IsNullOrEmpty(SecurityConstants.AppSettingsApiKeysKey));
            Assert.IsFalse(string.IsNullOrEmpty(SecurityConstants.AppSettingsJwtIssuerKey));
            Assert.IsFalse(string.IsNullOrEmpty(SecurityConstants.AppSettingsJwtAudienceKey));
            Assert.IsFalse(string.IsNullOrEmpty(SecurityConstants.AppSettingsJwtSecretKey));
            Assert.IsFalse(string.IsNullOrEmpty(SecurityConstants.AppSettingsCorsOriginsKey));
            Assert.IsFalse(string.IsNullOrEmpty(SecurityConstants.AppSettingsMtlsRequiredKey));
        }

        [TestMethod]
        public void CorsConfig_GetMaxAge_DefaultIs3600()
        {
            // Act - 無配置時應返回預設值
            var maxAge = CorsConfig.GetMaxAge();

            // Assert
            Assert.AreEqual(3600, maxAge);
        }

        [TestMethod]
        public void CorsConfig_GetAllowedOrigins_NullWhenNotConfigured()
        {
            // Act
            var origins = CorsConfig.GetAllowedOrigins();

            // Assert - 未配置時返回 null
            Assert.IsNull(origins);
        }

        [TestMethod]
        public void CorsConfig_GetAllowCredentials_FalseWhenNotConfigured()
        {
            // Act
            var creds = CorsConfig.GetAllowCredentials();

            // Assert - 未配置時返回 false
            Assert.IsFalse(creds);
        }
    }
}
