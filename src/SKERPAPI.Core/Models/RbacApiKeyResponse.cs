using System;
using System.Collections.Generic;

namespace SKERPAPI.Core.Models
{
    /// <summary>
    /// 內部 RBAC REST API 回應 DTO。
    /// 對應 GET {RbacApiBaseUrl}/api/rbac/keys/{apiKey} 的 JSON 回應。
    /// </summary>
    internal sealed class RbacApiKeyResponse
    {
        public bool IsAuthenticated { get; set; }
        public string ApiKey { get; set; }
        public string KeyName { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
        public DateTime? ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
    }
}
