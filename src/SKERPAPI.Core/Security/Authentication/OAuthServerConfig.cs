using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using Owin;
using Serilog;

namespace SKERPAPI.Core.Security.Authentication
{
    /// <summary>
    /// OAuth2 本地 Token 發行伺服器配置。
    /// 在 SKERPAPI 內建簡易的 OAuth2 Token 端點 (/api/token)，
    /// 支援 client_credentials 與 password grant type。
    /// </summary>
    /// <remarks>
    /// 此為過渡階段方案。未來可切換至外部 IdP (Azure AD, Keycloak, etc.)。
    /// 切換時僅需：
    ///   1. 移除此 OAuthAuthorizationServer 配置
    ///   2. 保留 JwtBearerAuth Middleware (驗證外部 IdP 發行的 Token)
    ///   3. 更新 Web.config 的 Jwt:Issuer / Jwt:Audience
    /// </remarks>
    public static class OAuthServerConfig
    {
        /// <summary>
        /// 預設 Token 端點路徑
        /// </summary>
        public const string TokenEndpointPath = "/api/token";

        /// <summary>
        /// 配置並啟用 OAuth2 Authorization Server (自建 Token 端點)
        /// </summary>
        public static void ConfigureOAuthServer(IAppBuilder app)
        {
            var expiryMinutesStr = ConfigurationManager.AppSettings[SecurityConstants.AppSettingsJwtExpiryMinutesKey];
            var expiryMinutes = 60; // 預設 60 分鐘
            if (!string.IsNullOrEmpty(expiryMinutesStr) && int.TryParse(expiryMinutesStr, out int parsed))
                expiryMinutes = parsed;

            var allowInsecureHttp = false;
#if DEBUG
            allowInsecureHttp = true;
#endif

            var oAuthServerOptions = new OAuthAuthorizationServerOptions
            {
                AllowInsecureHttp = allowInsecureHttp,
                TokenEndpointPath = new PathString(TokenEndpointPath),
                AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(expiryMinutes),
                Provider = new SkerpApiOAuthProvider(),
                // 使用預設的 Machine Key 保護 format
                AccessTokenFormat = null
            };

            app.UseOAuthAuthorizationServer(oAuthServerOptions);

            // 同時啟用 Bearer Token 驗證 (讓此端點發行的 Token 也能被驗證)
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

            Log.Information("OAuth2 Authorization Server configured. TokenEndpoint={Path}, ExpiryMinutes={Expiry}",
                TokenEndpointPath, expiryMinutes);
        }
    }

    /// <summary>
    /// OAuth2 Provider 實作。
    /// 處理 client_credentials 與 password grant type 的驗證邏輯。
    /// </summary>
    /// <remarks>
    /// 此 Provider 目前使用 Web.config 中的 API Key 清單作為 Client 驗證來源。
    /// 未來可替換為資料庫查詢或外部 IdP 驗證。
    /// </remarks>
    public class SkerpApiOAuthProvider : OAuthAuthorizationServerProvider
    {
        /// <summary>
        /// 驗證 Client 身份 (client_id / client_secret)
        /// </summary>
        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            string clientId = null;
            string clientSecret = null;

            // 嘗試從 Basic Auth Header 或 Form Body 提取 client credentials
            if (!context.TryGetBasicCredentials(out clientId, out clientSecret))
            {
                context.TryGetFormCredentials(out clientId, out clientSecret);
            }

            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                // 使用 ApiKey 驗證邏輯驗證 client_secret
                var result = ApiKeyAuthMiddleware.ValidateApiKey(clientSecret);
                if (result.IsValid)
                {
                    context.Validated(clientId);
                    Log.Debug("OAuth: Client {ClientId} validated", clientId);
                    return Task.CompletedTask;
                }
            }

            // 允許 password grant 不帶 client credentials
            if (string.IsNullOrEmpty(clientId))
            {
                context.Validated();
                return Task.CompletedTask;
            }

            Log.Warning("OAuth: Client validation failed for {ClientId}", clientId);
            context.Rejected();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 處理 client_credentials grant type
        /// </summary>
        public override Task GrantClientCredentials(OAuthGrantClientCredentialsContext context)
        {
            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.Name, context.ClientId ?? "system"));
            identity.AddClaim(new Claim(SecurityConstants.ClientIdClaimType, context.ClientId ?? "system"));
            identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, "client_credentials"));

            var properties = new AuthenticationProperties(new Dictionary<string, string>
            {
                { "client_id", context.ClientId ?? "system" }
            });

            var ticket = new AuthenticationTicket(identity, properties);
            context.Validated(ticket);

            Log.Information("OAuth: Token issued via client_credentials for {ClientId}", context.ClientId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 處理 password (Resource Owner Password Credentials) grant type
        /// </summary>
        public override Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            // TODO: 連接企業 RBAC 資料庫驗證 username/password
            // 目前階段使用簡易驗證，正式環境必須替換

            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.Name, context.UserName));
            identity.AddClaim(new Claim(SecurityConstants.ClientIdClaimType, context.ClientId ?? "unknown"));
            identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, "password"));

            var ticket = new AuthenticationTicket(identity, new AuthenticationProperties());
            context.Validated(ticket);

            Log.Information("OAuth: Token issued via password grant for user {UserName}", context.UserName);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Token 端點回應時附加額外資訊
        /// </summary>
        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (var property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }
            return Task.CompletedTask;
        }
    }
}
