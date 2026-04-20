using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Owin;
using Serilog;

namespace SKERPAPI.Core.Security.Authentication
{
    /// <summary>
    /// 用戶端憑證 (mTLS) 認證 OWIN 中介層。
    /// 驗證 TLS 用戶端憑證是否由受信任的 Windows CA 簽發，
    /// 並將憑證資訊寫入 ClaimsPrincipal。
    /// </summary>
    /// <remarks>
    /// 前置條件：
    ///   - IIS SSL Settings 需設為 "Accept" 或 "Require" Client Certificates
    ///   - 企業 Windows CA 必須在伺服器的 Trusted Root / Intermediate CAs 中
    ///
    /// Web.config 設定：
    ///   &lt;add key="Security:MtlsRequired" value="false" /&gt;
    ///   &lt;add key="Security:MtlsTrustedIssuers" value="CN=Company-CA,CN=Corp-SubCA" /&gt;
    /// </remarks>
    public class ClientCertificateAuthMiddleware : OwinMiddleware
    {
        public ClientCertificateAuthMiddleware(OwinMiddleware next) : base(next) { }

        public override async Task Invoke(IOwinContext context)
        {
            // 如果已有認證身份，直接 pass
            if (context.Authentication?.User?.Identity?.IsAuthenticated == true)
            {
                await Next.Invoke(context);
                return;
            }

            var clientCert = context.Get<X509Certificate2>("ssl.ClientCertificate");

            if (clientCert != null)
            {
                var validationResult = ValidateCertificate(clientCert);
                if (validationResult.IsValid)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, validationResult.SubjectName),
                        new Claim(SecurityConstants.ClientIdClaimType, validationResult.SubjectName),
                        new Claim(SecurityConstants.CertificateThumbprintClaimType, validationResult.Thumbprint),
                        new Claim(ClaimTypes.AuthenticationMethod, SecurityConstants.ClientCertificateScheme),
                        new Claim("cert_issuer", validationResult.Issuer)
                    };

                    var identity = new ClaimsIdentity(claims, SecurityConstants.ClientCertAuthenticationType);
                    context.Authentication.User = new ClaimsPrincipal(identity);

                    Log.Information("mTLS: Client authenticated via certificate. Subject={Subject}, Issuer={Issuer}, Thumbprint={Thumbprint}",
                        validationResult.SubjectName, validationResult.Issuer, validationResult.Thumbprint);
                }
                else
                {
                    Log.Warning("mTLS: Certificate validation failed. Subject={Subject}, Reason={Reason}",
                        clientCert.Subject, validationResult.FailureReason);

                    var isMtlsRequired = IsMtlsRequired();
                    if (isMtlsRequired)
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            "{\"success\":false,\"errorMessage\":\"Client certificate validation failed: " +
                            validationResult.FailureReason + "\"}");
                        return;
                    }
                }
            }
            else
            {
                var isMtlsRequired = IsMtlsRequired();
                if (isMtlsRequired)
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"success\":false,\"errorMessage\":\"Client certificate is required.\"}");
                    return;
                }
            }

            await Next.Invoke(context);
        }

        /// <summary>
        /// 驗證用戶端憑證。
        /// 使用 Windows Certificate Store 進行 Chain Validation，
        /// 並可選擇限制受信任的 Issuer。
        /// </summary>
        internal static CertificateValidationResult ValidateCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
                return new CertificateValidationResult { IsValid = false, FailureReason = "Certificate is null" };

            // 1. 檢查憑證是否過期
            if (DateTime.Now < certificate.NotBefore || DateTime.Now > certificate.NotAfter)
            {
                return new CertificateValidationResult
                {
                    IsValid = false,
                    FailureReason = $"Certificate expired or not yet valid. NotBefore={certificate.NotBefore}, NotAfter={certificate.NotAfter}"
                };
            }

            // 2. 使用 Windows CA Chain 驗證
            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                var chainIsValid = chain.Build(certificate);
                if (!chainIsValid)
                {
                    var statuses = new System.Text.StringBuilder();
                    foreach (var status in chain.ChainStatus)
                    {
                        statuses.Append($"[{status.Status}: {status.StatusInformation}] ");
                    }
                    return new CertificateValidationResult
                    {
                        IsValid = false,
                        FailureReason = $"Chain validation failed: {statuses}"
                    };
                }
            }

            // 3. 檢查 Issuer 白名單（可選）
            var trustedIssuers = GetTrustedIssuers();
            if (trustedIssuers != null && trustedIssuers.Length > 0)
            {
                var issuerDn = certificate.Issuer;
                var isTrusted = false;
                foreach (var trusted in trustedIssuers)
                {
                    if (issuerDn.IndexOf(trusted.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isTrusted = true;
                        break;
                    }
                }
                if (!isTrusted)
                {
                    return new CertificateValidationResult
                    {
                        IsValid = false,
                        FailureReason = $"Issuer '{issuerDn}' is not in the trusted issuers list"
                    };
                }
            }

            return new CertificateValidationResult
            {
                IsValid = true,
                SubjectName = certificate.Subject,
                Issuer = certificate.Issuer,
                Thumbprint = certificate.Thumbprint
            };
        }

        private static string[] GetTrustedIssuers()
        {
            var issuers = ConfigurationManager.AppSettings[SecurityConstants.AppSettingsMtlsTrustedIssuersKey];
            if (string.IsNullOrEmpty(issuers))
                return null;
            return issuers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool IsMtlsRequired()
        {
            var val = ConfigurationManager.AppSettings[SecurityConstants.AppSettingsMtlsRequiredKey];
            return !string.IsNullOrEmpty(val) && bool.TryParse(val, out bool result) && result;
        }
    }

    /// <summary>
    /// 憑證驗證結果
    /// </summary>
    public class CertificateValidationResult
    {
        public bool IsValid { get; set; }
        public string SubjectName { get; set; }
        public string Issuer { get; set; }
        public string Thumbprint { get; set; }
        public string FailureReason { get; set; }
    }
}
