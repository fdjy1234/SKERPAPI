using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKERPAPI.Core.Security.Authentication;

namespace SKERPAPI.Core.Tests.Security
{
    /// <summary>
    /// ClientCertificateAuthMiddleware 單元測試。
    /// 測試 mTLS 憑證驗證邏輯。
    /// </summary>
    [TestClass]
    public class ClientCertificateAuthMiddlewareTests
    {
        [TestMethod]
        public void ValidateCertificate_NullCertificate_ReturnsInvalid()
        {
            // Arrange & Act
            var result = ClientCertificateAuthMiddleware.ValidateCertificate(null);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Certificate is null", result.FailureReason);
        }

        [TestMethod]
        public void CertificateValidationResult_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var result = new CertificateValidationResult();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNull(result.SubjectName);
            Assert.IsNull(result.Issuer);
            Assert.IsNull(result.Thumbprint);
            Assert.IsNull(result.FailureReason);
        }

        [TestMethod]
        public void CertificateValidationResult_SetProperties_Persisted()
        {
            // Arrange & Act
            var result = new CertificateValidationResult
            {
                IsValid = true,
                SubjectName = "CN=TestClient",
                Issuer = "CN=TestCA",
                Thumbprint = "ABC123",
                FailureReason = null
            };

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("CN=TestClient", result.SubjectName);
            Assert.AreEqual("CN=TestCA", result.Issuer);
            Assert.AreEqual("ABC123", result.Thumbprint);
        }

        [TestMethod]
        public void ValidateCertificate_ExpiredCert_ReturnsInvalid()
        {
            // Arrange - 建立一個已過期的自簽憑證
            using (var rsa = System.Security.Cryptography.RSA.Create(2048))
            {
                var request = new CertificateRequest("CN=ExpiredTest", rsa, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
                var cert = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-30),
                    DateTimeOffset.UtcNow.AddDays(-1)  // 已過期
                );

                // Act
                var result = ClientCertificateAuthMiddleware.ValidateCertificate(cert);

                // Assert
                Assert.IsFalse(result.IsValid);
                Assert.IsTrue(result.FailureReason.Contains("expired") || result.FailureReason.Contains("not yet valid"),
                    $"Expected expiry message but got: {result.FailureReason}");
            }
        }
    }
}
