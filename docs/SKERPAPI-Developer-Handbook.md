# SKERPAPI 開發手冊 (Developer Handbook)

> **版本**: 1.1.0 | **最後更新**: 2026-04-06 | **適用對象**: 程式設計師

---

## 1. 專案概覽

SKERPAPI 是一個基於 **ASP.NET Web API 2 (.NET Framework 4.8)** 建置的企業級 API 平台，
採用**多專案模組化架構**，每個業務系統（AOI、CAR 等）是獨立的 Class Library。

### 技術棧

| 技術 | 版本 | 用途 |
|---|---|---|
| .NET Framework | 4.8 (SDK-style) | 基礎框架 |
| ASP.NET Web API 2 | 5.3.0 | RESTful API |
| Autofac | 8.3.0 | DI 容器 |
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
│   ├── SKERPAPI.Core/      ← 共用基礎設施 (模型、例外處理)
│   ├── SKERPAPI.Host/      ← Web 主機 (唯一部署單位、啟動與載入Plugin)
│   ├── SKERPAPI.AOI/       ← AOI 系統模組
│   └── SKERPAPI.CAR/       ← CAR 系統模組
├── tests/
│   ├── SKERPAPI.Core.Tests/ ← Core 單元測試
│   ├── SKERPAPI.AOI.Tests/  ← AOI 單元測試
│   ├── SKERPAPI.CAR.Tests/  ← CAR 單元測試
│   └── SKERPAPI.E2E.Tests/  ← 端對端整合測試
└── App_Data/
    ├── Plugins/             ← 動態模組 DLL 放置路徑
    └── logs/                ← 系統日誌輸出
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

## 3. URL 路由慣例

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

### Controller 撰寫範本

```csharp
using System.Web.Http;
using SKERPAPI.Core.Controllers;

namespace SKERPAPI.AOI.Controllers.V1
{
    [RoutePrefix("webapi/aoi/v1/workorder")]
    public class WorkOrderController : ApiBaseController
    {
        private readonly IMesQueryService _mesService;

        // 透過 Autofac DI 注入
        public WorkOrderController(IMesQueryService mesService)
        {
            _mesService = mesService;
        }

        [HttpGet, Route("{id}")]
        public IHttpActionResult GetWorkOrder(string id)
        {
            var result = _mesService.GetWorkOrderDetails(id);
            return ApiOk(result);           // ← 統一成功回應
        }

        [HttpPost, Route("create")]
        public IHttpActionResult CreateOrder([FromBody] MyDto request)
        {
            if (!ModelState.IsValid) 
            {
                return ApiFail("Validation failed.");
            }
            var result = _mesService.Create(request);
            return ApiOk(result);
        }
    }
}
```

---

## 4. 統一回應格式

所有 API 統一依賴 `ApiBaseController` 回傳 `ApiResponse<T>`：

### 成功回應 (HTTP 200)
```json
{
  "success": true,
  "data": { "system": "AOI", "status": "Online" },
  "errorMessage": null,
  "traceId": "abc123def456",
  "timestamp": "2026-04-05T01:00:00Z"
}
```

### 失敗/例外回應 (HTTP 400/404/500)
```json
{
  "success": false,
  "data": null,
  "errorMessage": "Resource not found.",
  "traceId": "xyz789",
  "timestamp": "2026-04-05T01:00:00Z"
}
```

### 分頁回應
```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "totalCount": 150,
    "page": 2,
    "pageSize": 20,
    "totalPages": 8
  },
  "traceId": "...",
  "timestamp": "..."
}
```

### ApiBaseController 回應方法

| 方法 | 用途 | HTTP 狀態碼 |
|---|---|---|
| `ApiOk(data)` | 成功回應 | 200 |
| `ApiPagedOk(items, total, page, size)` | 分頁成功 | 200 |
| `ApiFail(message)` | 失敗回應 | 400 (預設) |
| `ApiNotFound(message)` | 資源不存在 | 404 |

---

## 5. DI 依賴注入 (Autofac)

### 自動註冊規則
系統透過 `AutofacConfig` 或 `PluginLoader` 自動掃描載入：
1. **Controller**: 自動發現並註冊。
2. **Service**: 類別名稱以 `Service` 結尾的，系統會自動透過 Reflection 注入此介面與實作。
3. **生命週期**: `InstancePerRequest`（每次 HTTP 請求一個實例）。

---

## 6. NSwag Swagger 分組

### 方案一: OpenApiTag (推薦)
在 Controller 上加入 `[OpenApiTag]` Attribute：
```csharp
[OpenApiTag("AOI", Description = "AOI 自動光學檢測系統")]
[RoutePrefix("webapi/aoi/v1/aoi01")]
public class AOI01Controller : ApiBaseController
```

### 方案二: IOperationProcessor (全自動)
自訂 `SystemGroupOperationProcessor` 根據 URL 路徑自動分組，不需在 Controller 上加任何 Attribute。

---

## 7. 安全機制

| 機制 | 過濾器 | 說明 |
|---|---|---|
| API 金鑰驗證 | `ApiKeyAttribute` | 從 `X-Api-Key` Header 驗證 |
| 限速 | `RateLimitAttribute` | 預設 100 次/分鐘 |
| 審計日誌 | `AuditLogAttribute` | 記錄 API 呼叫生命週期 |
| 安全標頭 | `SecurityHeadersAttribute` | XSS、Clickjacking 防護 |
| ModelState 驗證 | `ModelValidationFilter` | 自動驗證請求模型 |
| 全域例外處理 | `ApiExceptionFilter` | 捕獲未處理例外 |

上述 Filter 中如有全域設定或掛載於 `ApiBaseController` 之設定，子類別將自動適用。

---

## 8. 模組與 Plugin 開發指南

### 8.1 靜態載入 (編譯時依賴)
需建立新業務模組 (例如 `SKERPAPI.MES`) 且重啟主機：
1. 建立 C# 類別庫 (SDK-style .NET 4.8)。
2. 加入 `SKERPAPI.Core` 參考。
3. 在 Host 專案加入模組的專案參考。
4. 實作 `IModuleInitializer`：
```csharp
using System.Web.Http;
using SKERPAPI.Core.Modules;
using Serilog;

namespace SKERPAPI.MES
{
    public class MESModuleInitializer : IModuleInitializer
    {
        public string ModuleName => "MES";
        public string ModuleVersion => "1.0.0";

        public void Initialize(HttpConfiguration config)
        {
            Log.Information("MES Module Initialized.");
        }
    }
}
```

### 8.2 動態 Plugin 載入
要在不重啟整個 Application 或是重新編譯 Host 的狀況下擴充功能：
1. 編譯您的 C# Plugin 為 DLL (`SKERPAPI.MyPlugin.dll`)。
2. 將 DLL 與其相依組件放置於 `SKERPAPI.Host` 的 `App_Data/Plugins/` 目錄。
3. `Host` 下次重新啟動或回收應用程式集區時，`PluginLoader` 掃描到該 DLL 就會自動註冊對應的 Controller 與 Service。

---

## 9. 測試規範 (TDD)

### Red → Green → Refactor
1. **Red**: 先寫測試（測試必定失敗）。
2. **Green**: 寫最少量的程式碼讓測試通過。
3. **Refactor**: 重構程式碼，保持測試通過。

### 測試結構 (AAA 模式)
```csharp
[TestMethod]
public void MethodName_Condition_Expected()
{
    // Arrange - 準備測試資料
    var mockService = new Mock<IAOIService>();
    mockService.Setup(s => s.GetStatus()).Returns(new StatusDto { Status = "Online" });
    var controller = new AOI01Controller(mockService.Object);

    // Act - 執行被測方法
    var result = controller.GetStatus();

    // Assert - 驗證結果
    Assert.IsNotNull(result);
    mockService.Verify(s => s.GetStatus(), Times.Once);
}
```

### 端對端測試 (E2E)
在 `SKERPAPI.E2E.Tests` 中，透過 `TestServerFixture` (內建 `HttpServer`) 進行完整系統測試，不綁定 Port。

### 測試覆蓋率目標
| 層級 | 最低覆蓋率 | 重點 |
|---|---|---|
| Service | 80% | 業務邏輯完整測試 |
| Controller | 70% | 路由、驗證、回應格式 |
| Filter | 90% | 安全機制必須完整測試 |
| Model | 60% | 建構子、條件等 |

---

## 10. 日誌與監控

### Serilog 結構化日誌
```csharp
Serilog.Log.Information("AOI inspection completed: {BatchId} Result={Status}",
                         request.BatchId, result.Status);
```
日誌輸出至: `App_Data/logs/skerpapi-YYYY-MM-DD.log`

### 日誌格式規範
```
2026-04-06 09:00:00.123 +08:00 [INF] API CALL START: [POST] http://localhost/webapi/aoi/v1/aoi01/inspect from 192.168.1.100
2026-04-06 09:00:00.456 +08:00 [INF] AOI inspection completed: BATCH-001 Result=Pass
2026-04-06 09:00:00.789 +08:00 [INF] API CALL END: Status=OK
```
