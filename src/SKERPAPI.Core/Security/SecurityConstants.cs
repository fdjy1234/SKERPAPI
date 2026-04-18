namespace SKERPAPI.Core.Security
{
    /// <summary>
    /// 安全模組常數定義。
    /// 集中管理 Header 名稱、認證 Scheme、Claims Type 等常數。
    /// </summary>
    public static class SecurityConstants
    {
        // ── Header Names ──────────────────────────────────────────
        public const string ApiKeyHeaderName = "X-Api-Key";
        public const string AuthorizationHeaderName = "Authorization";

        // ── Authentication Schemes ────────────────────────────────
        public const string BearerScheme = "Bearer";
        public const string ApiKeyScheme = "ApiKey";
        public const string ClientCertificateScheme = "ClientCertificate";

        // ── Authentication Types (for ClaimsIdentity) ─────────────
        public const string ApiKeyAuthenticationType = "ApiKeyAuthentication";
        public const string JwtBearerAuthenticationType = "JwtBearerAuthentication";
        public const string ClientCertAuthenticationType = "ClientCertificateAuthentication";
        public const string OAuthLocalAuthenticationType = "OAuthLocalToken";

        // ── Custom Claim Types ────────────────────────────────────
        public const string PermissionClaimType = "permission";
        public const string ClientIdClaimType = "client_id";
        public const string CertificateThumbprintClaimType = "cert_thumbprint";

        // ── AppSettings Keys ──────────────────────────────────────
        public const string AppSettingsApiKeysKey = "Security:ApiKeys";
        public const string AppSettingsJwtIssuerKey = "Jwt:Issuer";
        public const string AppSettingsJwtAudienceKey = "Jwt:Audience";
        public const string AppSettingsJwtSecretKey = "Jwt:Secret";
        public const string AppSettingsJwtExpiryMinutesKey = "Jwt:ExpiryMinutes";
        public const string AppSettingsCorsOriginsKey = "Cors:AllowedOrigins";
        public const string AppSettingsCorsCredentialsKey = "Cors:AllowCredentials";
        public const string AppSettingsCorsMaxAgeKey = "Cors:MaxAge";
        public const string AppSettingsMtlsRequiredKey = "Security:MtlsRequired";
        public const string AppSettingsMtlsTrustedIssuersKey = "Security:MtlsTrustedIssuers";
    }
}
