# SKERPAPI 開發手冊 (Developer Handbook)

> **版本**: 2.0.0 | **最後更新**: 2026-04-18 | **適用對象**: 程式設計師

---

## 1. 專案概覽

SKERPAPI 是一個基於 **ASP.NET Web API 2 (.NET Framework 4.8)** 建置的企業級 API 平台，
採用**多專案模組化架構**，每個業務系統（AOI、CAR 等）是獨立的 Class Library。

### 技術棧

| 技術 | 版本 | 用途 |
|---|---|---|
| .NET Framework | 4.8 (SDK-style) | 基礎框架 |
| ASP.NET Web API 2 | 5.3.0 | RESTful API |
| OWIN / Katana | 4.2.2 | 中介層管線（認證/CORS） |
| Autofac | 8.4.0 | DI 容器 |
| NSwag | 14.6.3 | Swagger 文件 |
| Serilog | 4.3.1 | 結構化日誌 |
| MSTest | 3.8.3 | 單元測試 |
| Moq | 4.20.72 | Mock 框架 |
| C# | 7.3+ | 開發語言 |

---

## 2. 專案結構與依賴規則

### 專案結構

```text
SKERPAPI.sln
├── src/
│   ├── SKERPAPI.Core/              ← 共用基礎設施
│   │   ├── Controllers/            ← ApiBaseController
│   │   ├── Filters/                ← 過濾器管線
│   │   ├── Models/                 ← ApiResponse, PagedResult
│   │   ├── Modules/                ← IModuleInitializer 介面
│   │   ├── Extensions/             ← HttpConfiguration 擴展
│   │   └── Security/               ← 安全模組 ★ NEW
│   │       ├── SecurityConstants.cs
│   │       ├── Authentication/     ← OWIN 認證中介層
│   │       │   ├── ApiKeyAuthMiddleware.cs
│   │       │   ├── ClientCertificateAuthMiddleware.cs
│   │       │   ├── OAuthServerConfig.cs
│   │       │   └── AuthenticationRequiredMiddleware.cs
│   │       ├── Authorization/      ← 可插拔 RBAC 授權
│   │       │   ├── IAuthorizationProvider.cs
│   │       │   ├── PermissionAuthorizeAttribute.cs
│   │       │   ├── ConfigBasedAuthProvider.cs
│   │       │   └── DbRbacAuthProvider.cs
│   │       └── Cors/               ← CORS 雙層配置
│   │           └── CorsConfig.cs
│   ├── SKERPAPI.Host/              ← Web 主機（唯一部署單位）
│   │   └── App_Start/
│   │       ├── Startup.cs           ← OWIN 啟動 ★ NEW
│   │       ├── WebApiConfig.cs
│   │       ├── AutofacConfig.cs
│   │       ├── PluginLoader.cs
│   │       ├── ModuleInitializerRunner.cs
│   │       └── SerilogConfig.cs
│   ├── SKERPAPI.AOI/               ← AOI 系統模組
│   └── SKERPAPI.CAR/               ← CAR 系統模組
├── tests/
│   ├── SKERPAPI.Core.Tests/
│   │   └── Security/               ← 安全模組單元測試 ★ NEW
│   │       ├── ApiKeyAuthMiddlewareTests.cs
│   │       ├── ClientCertificateAuthMiddlewareTests.cs
│   │       ├── PermissionAuthorizeAttributeTests.cs
│   │       ├── DbRbacAuthProviderTests.cs
│   │       └── CorsConfigTests.cs
│   ├── SKERPAPI.AOI.Tests/
│   ├── SKERPAPI.CAR.Tests/
│   └── SKERPAPI.E2E.Tests/
├── docs/
│   ├── work/                        ← 工作報告
│   └── test/                        ← 測試報告
└── App_Data/
    ├── Plugins/                     ← 動態模組 DLL 放置路徑
    └── logs/                        ← 系統日誌輸出
```

### 依賴規則

> [!CAUTION]
> 違反依賴規則會導致循環引用編譯失敗！

| 規則 | 說明 |
|---|---|
| ✅ 模組 → Core | AOI/CAR 可引用 Core |
| ✅ Host → Core + 所有模組 | Host 引用所有人 |
| ❌ 模組 → Host | 模組不可引用 Host |
| ❌ 模組 → 模組 | AOI 不可引用 CAR |
| ❌ Core → 模組 | Core 不可引用任何模組 |

---

## 3. OWIN Pipeline 與應用程式啟動

### 3.1 啟動流程

應用程式由 **OWIN Startup** 驅動（取代傳統 `Global.asax`）：

```text
IIS 啟動
  └── OWIN Startup.Configuration(IAppBuilder app)
        ├── 1. SerilogConfig.Configure()
        ├── 2. CorsConfig.ConfigureOwinCors(app)         ← CORS Layer 1
        ├── 3. app.Use<ClientCertificateAuthMiddleware>() ← mTLS
        ├── 4. OAuthServerConfig.ConfigureOAuthServer(app) ← OAuth2
        ├── 5. app.Use<ApiKeyAuthMiddleware>()             ← API Key
        ├── 6. app.Use<AuthenticationRequiredMiddleware>() ← 認證閘門
        └── 7. app.UseWebApi(config)                       ← Web API Pipeline
              ├── WebApiConfig.Register(config)
              │     ├── RegisterCoreFilters()
              │     ├── CorsConfig.EnableWebApiCors()      ← CORS Layer 2
              │     ├── MapHttpAttributeRoutes()
              │     ├── AutofacConfig.Register()
              │     ├── PluginLoader.LoadPlugins()
              │     └── ModuleInitializerRunner.RunAll()
              └── 路由解析 → Controller → Action
```

### 3.2 Web.config 安全設定

```xml
<appSettings>
  <!-- OWIN 自動啟動 -->
  <add key="owin:AutomaticAppStartup" value="true" />

  <!-- API Key 認證 -->
  <add key="ApiKey" value="" />                     <!-- 單一金鑰（留空=跳過） -->
  <add key="Security:ApiKeys" value="" />           <!-- 多金鑰: clientId:key,clientId:key -->

  <!-- OAuth2 / JWT -->
  <add key="Jwt:Issuer" value="SKERPAPI" />
  <add key="Jwt:Audience" value="SKERPAPI-Clients" />
  <add key="Jwt:Secret" value="" />
  <add key="Jwt:ExpiryMinutes" value="60" />

  <!-- mTLS -->
  <add key="Security:MtlsRequired" value="false" /> <!-- true=強制要求憑證 -->
  <add key="Security:MtlsTrustedIssuers" value="" /> <!-- 留空=信任 Windows CA Store 所有 -->

  <!-- CORS -->
  <add key="Cors:AllowedOrigins" value="" />         <!-- 逗號分隔，留空=允許所有 -->
  <add key="Cors:AllowCredentials" value="true" />
  <add key="Cors:MaxAge" value="3600" />
</appSettings>
```

---

## 4. 認證 (Authentication)

### 4.1 支援的認證方式

| 認證方式 | Header / 機制 | 適用場景 | 中介層 |
|---|---|---|---|
| **API Key** | `X-Api-Key: <key>` | 內部工具、開發環境 | `ApiKeyAuthMiddleware` |
| **OAuth2 Bearer** | `Authorization: Bearer <token>` | SPA、Mobile、第三方 | `OAuthBearerAuthenticationMiddleware` |
| **mTLS** | TLS Client Certificate | M2M、工廠設備 | `ClientCertificateAuthMiddleware` |

### 4.2 取得 OAuth2 Token

```bash
# client_credentials grant
curl -X POST http://localhost/api/token \
  -d "grant_type=client_credentials&client_id=myapp&client_secret=my-api-key"

# password grant
curl -X POST http://localhost/api/token \
  -d "grant_type=password&username=admin&password=pass123"
```

回應：
```json
{
  "access_token": "eyJhbGciOi...",
  "token_type": "bearer",
  "expires_in": 3599
}
```

### 4.3 使用 Bearer Token 呼叫 API

```bash
curl -H "Authorization: Bearer eyJhbGciOi..." http://localhost/webapi/aoi/v1/aoi01/status
```

### 4.4 認證被動模式說明

所有認證中介層採用**被動模式**：攜帶了有效認證就設定 `ClaimsPrincipal`，未攜帶不立即拒絕。
最終由 `AuthenticationRequiredMiddleware` 檢查是否至少通過一種認證。

公開路徑（不需認證）：
- `/api/token` — OAuth2 Token 端點
- `/swagger` — Swagger UI
- `/api/health` — 健康檢查

---

## 5. 授權 (Authorization)

### 5.1 使用 PermissionAuthorize Attribute

```csharp
using SKERPAPI.Core.Security.Authorization;

[RoutePrefix("webapi/aoi/v1/workorder")]
public class WorkOrderController : ApiBaseController
{
    [HttpGet, Route("{id}")]
    [PermissionAuthorize("aoi:workorder:read")]        // 讀取權限
    public IHttpActionResult GetWorkOrder(string id) { ... }

    [HttpPost, Route("create")]
    [PermissionAuthorize("aoi:workorder:create")]      // 建立權限
    public IHttpActionResult CreateOrder([FromBody] MyDto request) { ... }

    [HttpDelete, Route("{id}")]
    [PermissionAuthorize("aoi:workorder:delete")]      // 刪除權限
    public IHttpActionResult DeleteOrder(string id) { ... }
}
```

### 5.2 權限代碼命名規則

```
{module}:{resource}:{action}
```

範例：`aoi:workorder:create`, `car:vehicle:read`, `admin:config:write`

### 5.3 萬用字元支援

| 授權設定 | 匹配的權限 |
|---|---|
| `aoi:*:*` | 所有 AOI 權限 |
| `aoi:*:read` | AOI 所有資源的讀取權限 |
| `*:*:*` | 超級管理員 |

### 5.4 授權行為

| 狀態 | HTTP 回應 | 說明 |
|---|---|---|
| 未認證 | 401 Unauthorized | 未通過任何認證 |
| 已認證但無權限 | 403 Forbidden | 認證通過但缺少所需權限 |
| 無 Provider 註冊 | 200 (Fallback) | 僅檢查是否認證，放行所有授權 |

### 5.5 切換授權 Provider

在 `AutofacConfig.cs` 中修改一行即可：

```csharp
// 開發環境：全部放行
builder.RegisterType<ConfigBasedAuthProvider>()
    .As<IAuthorizationProvider>().SingleInstance();

// 正式環境：切換為 DB RBAC
// builder.RegisterType<DbRbacAuthProvider>()
//     .As<IAuthorizationProvider>().SingleInstance();
```

---

## 6. CORS（跨域資源共享）

### 6.1 全域 CORS（OWIN Layer 1）
由 `CorsConfig.ConfigureOwinCors()` 處理，設定在 `Web.config`：
- `Cors:AllowedOrigins` — 逗號分隔的白名單（留空允許所有）
- `Cors:AllowCredentials` — 是否允許 credentials
- `Cors:MaxAge` — preflight 快取秒數

### 6.2 Controller 級 CORS（Web API Layer 2）

```csharp
using System.Web.Http.Cors;

// 公開 API
[EnableCors(origins: "*", headers: "*", methods: "GET")]
public class StatusController : ApiBaseController { }

// 受限 API
[EnableCors(origins: "https://erp.company.com", headers: "*", methods: "GET,POST")]
public class WorkOrderController : ApiBaseController { }

// 禁止跨域
[DisableCors]
public class AdminController : ApiBaseController { }
```

---

## 7. URL 路由慣例

### 路由格式

```
webapi/{系統代號}/{版本}/{功能代號}/{Action 或 ID}
```

### 範例

| 端點 | HTTP 方法 | 所屬模組 | 功能說明 |
|---|---|---|---|
| `webapi/aoi/v1/aoi01/status` | GET | SKERPAPI.AOI | AOI 系統狀態 |
| `webapi/aoi/v1/aoi01/inspect` | POST | SKERPAPI.AOI | 執行檢測 |
| `webapi/car/v1/car01/info` | GET | SKERPAPI.CAR | 系統資訊 |
| `api/token` | POST | Host (OAuth) | 取得 Token |

### Controller 撰寫範本

```csharp
using System.Web.Http;
using SKERPAPI.Core.Controllers;
using SKERPAPI.Core.Security.Authorization;

namespace SKERPAPI.AOI.Controllers.V1
{
    [RoutePrefix("webapi/aoi/v1/workorder")]
    public class WorkOrderController : ApiBaseController
    {
        private readonly IMesQueryService _mesService;

        public WorkOrderController(IMesQueryService mesService)
        {
            _mesService = mesService;
        }

        [HttpGet, Route("{id}")]
        [PermissionAuthorize("aoi:workorder:read")]
        public IHttpActionResult GetWorkOrder(string id)
        {
            var result = _mesService.GetWorkOrderDetails(id);
            return ApiOk(result);
        }

        [HttpPost, Route("create")]
        [PermissionAuthorize("aoi:workorder:create")]
        public IHttpActionResult CreateOrder([FromBody] MyDto request)
        {
            if (!ModelState.IsValid) return ApiFail("Validation failed.");
            var result = _mesService.Create(request);
            return ApiOk(result);
        }
    }
}
```

---

## 8. 統一回應格式

所有 API 統一依賴 `ApiBaseController` 回傳 `ApiResponse<T>`：

### 成功回應 (HTTP 200)
```json
{
  "success": true,
  "data": { "system": "AOI", "status": "Online" },
  "errorMessage": null,
  "traceId": "abc123def456",
  "timestamp": "2026-04-18T01:00:00Z"
}
```

### 失敗回應 (HTTP 400/401/403/404/500)
```json
{
  "success": false,
  "data": null,
  "errorMessage": "Forbidden. Permission 'admin:config:write' is required.",
  "traceId": "xyz789",
  "timestamp": "2026-04-18T01:00:00Z"
}
```

| 方法 | 用途 | HTTP 狀態碼 |
|---|---|---|
| `ApiOk(data)` | 成功回應 | 200 |
| `ApiPagedOk(items, total, page, size)` | 分頁成功 | 200 |
| `ApiFail(message)` | 失敗回應 | 400 (預設) |
| `ApiNotFound(message)` | 資源不存在 | 404 |

---

## 9. DI 依賴注入 (Autofac)

### 自動註冊規則
1. **Controller**: 自動發現並註冊
2. **Service**: 類別名稱以 `Service` 結尾的，自動註冊為介面實作
3. **IAuthorizationProvider**: 手動註冊，可切換 Provider 實作
4. **生命週期**: Service=`InstancePerRequest`, IAuthorizationProvider=`SingleInstance`

---

## 10. 安全機制彙整

| 機制 | 元件 | 層級 | 說明 |
|---|---|---|---|
| 認證: API Key | `ApiKeyAuthMiddleware` | OWIN MW | 多金鑰支援 |
| 認證: OAuth2 | `OAuthServerConfig` | OWIN MW | 自建 Token 端點 |
| 認證: mTLS | `ClientCertificateAuthMiddleware` | OWIN MW | Windows CA 驗證 |
| 認證閘門 | `AuthenticationRequiredMiddleware` | OWIN MW | 至少一種認證通過 |
| CORS Layer 1 | `CorsConfig` | OWIN MW | 全域 preflight |
| CORS Layer 2 | `[EnableCors]` | Web API | Controller/Action 精細控制 |
| 授權 | `PermissionAuthorizeAttribute` | Web API Filter | RBAC 權限檢查 |
| 限速 | `RateLimitAttribute` | Web API Filter | 預設 100 次/分鐘 |
| 審計日誌 | `AuditLogAttribute` | Web API Filter | API 呼叫生命週期 |
| 安全標頭 | `SecurityHeadersAttribute` | Web API Filter | XSS/Clickjacking 防護 |
| ModelState 驗證 | `ModelValidationFilter` | Web API Filter | 自動驗證模型 |
| 全域例外處理 | `ApiExceptionFilter` | Web API Filter | 捕獲未處理例外 |

---

## 11. 模組與 Plugin 開發指南

### 11.1 靜態載入 (編譯時依賴)
1. 建立 C# 類別庫 (SDK-style .NET 4.8)
2. 加入 `SKERPAPI.Core` 參考
3. 在 Host 專案加入模組的專案參考
4. 實作 `IModuleInitializer`

### 11.2 動態 Plugin 載入
1. 編譯 Plugin 為 DLL (`SKERPAPI.MyPlugin.dll`)
2. 放置於 `App_Data/Plugins/` 目錄
3. Host 重啟時自動掃描載入

---

## 12. 測試規範 (TDD)

### Red → Green → Refactor
1. **Red**: 先寫測試（測試必定失敗）
2. **Green**: 寫最少量的程式碼讓測試通過
3. **Refactor**: 重構程式碼，保持測試通過

### 測試結構 (AAA 模式)
```csharp
[TestMethod]
public void HasPermission_AdminRole_HasAllPermissions()
{
    // Arrange
    var provider = new DbRbacAuthProvider();
    provider.AssignRole("admin", "Admin");

    // Act
    var result = provider.HasPermission("admin", "aoi:workorder:create");

    // Assert
    Assert.IsTrue(result);
}
```

### 測試覆蓋率目標
| 層級 | 最低覆蓋率 | 重點 |
|---|---|---|
| Security | 90% | 認證/授權必須完整測試 |
| Service | 80% | 業務邏輯完整測試 |
| Controller | 70% | 路由、驗證、回應格式 |
| Filter | 90% | 安全機制必須完整測試 |

---

## 13. 日誌與監控

### Serilog 結構化日誌
```csharp
Serilog.Log.Information("AOI inspection completed: {BatchId} Result={Status}",
                         request.BatchId, result.Status);
```
日誌輸出至: `App_Data/logs/skerpapi-YYYY-MM-DD.log`
