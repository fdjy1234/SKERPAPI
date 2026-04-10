# SKERPAPI — Asp.Versioning.WebApi 技術報告

| 項目         | 內容                                               |
|--------------|----------------------------------------------------|
| 文件版本     | v1.0                                               |
| 建立日期     | 2026-04-05                                         |
| 適用版本     | SKERPAPI v2.x                                      |
| 套件版本     | Asp.Versioning.WebApi 7.1.0                        |
| 目標框架     | .NET Framework 4.8                                 |
| 作者         | 資深系統架構師 / GitHub Copilot                   |

---

## 1. 背景與動機

SKERPAPI 採用模組化架構（AOI、CAR、Plugin），各模組透過 `RoutePrefix` 屬性路由對外提供 REST API。

**問題**：原始設計以硬編碼 `v1` 字串（`"webapi/aoi/v1/aoi01"`）作為版本標識，當需要同時維護多版本時無法靠框架協助進行版本協商、回報或衝突偵測。

**目標**：引入 `Asp.Versioning.WebApi` 7.x，統一版本管理，支援 URL segment / Query string / Header 三種版本協商策略，並以新版本控制器示範 breaking-change 處理模式。

---

## 2. 套件資訊

### 2.1 安裝方式

```xml
<!-- 每個需要版本標記的專案 (.csproj) 都需要加入 -->
<PackageReference Include="Asp.Versioning.WebApi" Version="7.1.0" />
```

### 2.2 適用專案清單

| 專案                     | 套件        | 用途                              |
|--------------------------|-------------|-----------------------------------|
| `SKERPAPI.Host`          | 必要        | 主機設定、AddApiVersioning 呼叫    |
| `SKERPAPI.Core`          | 必要        | 基礎架構，ApiBaseController        |
| `SKERPAPI.AOI`           | 必要        | AOI 控制器上的 `[ApiVersion]`      |
| `SKERPAPI.CAR`           | 必要        | CAR 控制器上的 `[ApiVersion]`      |
| `SKERPAPI.E2E.Tests`     | 必要        | TestServerFixture 的 versioning 設定 |

### 2.3 命名空間對照

| 功能                     | 命名空間 / 型別                                   |
|--------------------------|--------------------------------------------------|
| AddApiVersioning 設定     | `Asp.Versioning`                                  |
| ApiVersion 型別           | `Asp.Versioning.ApiVersion`                       |
| ApiVersionAttribute       | `Asp.Versioning.ApiVersionAttribute`              |
| 版本讀取器組合             | `Asp.Versioning.ApiVersionReader.Combine(...)`   |
| URL Segment 讀取器        | `Asp.Versioning.UrlSegmentApiVersionReader`       |
| Query String 讀取器       | `Asp.Versioning.QueryStringApiVersionReader`     |
| Header 讀取器             | `Asp.Versioning.HeaderApiVersionReader`           |
| Route Constraint 型別     | `Asp.Versioning.Routing.ApiVersionRouteConstraint` |
| Inline Constraint Resolver | `System.Web.Http.Routing.DefaultInlineConstraintResolver` |

---

## 3. 核心設定

### 3.1 WebApiConfig.cs — 正確初始化順序（關鍵）

```csharp
using System.Web.Http;
using System.Web.Http.Routing;
using Asp.Versioning;
using Asp.Versioning.Routing;
using SKERPAPI.Core.Extensions;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        // Step 1: 全域 Filters
        config.RegisterCoreFilters();

        // Step 2: 版本設定（必須在 MapHttpAttributeRoutes 之前）
        config.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("api-version"),
                new HeaderApiVersionReader("X-Api-Version")
            );
        });

        // Step 3: Attribute Routing（必須在 AddApiVersioning 之後）
        //   ⚠️ 需明確傳入含 ApiVersionRouteConstraint 的 resolver
        //   否則 {version:apiVersion} inline constraint 會解析失敗
        var constraintResolver = new DefaultInlineConstraintResolver();
        constraintResolver.ConstraintMap.Add("apiVersion", typeof(ApiVersionRouteConstraint));
        config.MapHttpAttributeRoutes(constraintResolver);

        // Step 4: DI、Plugins、Routes...
    }
}
```

> **⚠️ 常見陷阱**：`Asp.Versioning.WebApi` 7.x 不會自動修改 `DefaultInlineConstraintResolver`，
> 必須手動將 `ApiVersionRouteConstraint` 加入 ConstraintMap 並以覆載方式傳入 `MapHttpAttributeRoutes`。

---

### 3.2 版本讀取策略說明

| 策略               | 範例                                      | 優先序 | 適用場景                     |
|--------------------|-------------------------------------------|--------|------------------------------|
| URL Segment        | `/webapi/aoi/v1/aoi01/status`             | 1      | 推薦，直觀、便於 Swagger 文件 |
| Query String       | `/webapi/aoi/aoi01/status?api-version=1.0`| 2      | 測試、向後相容                |
| Custom Header      | `X-Api-Version: 1.0`                      | 3      | 服務間呼叫                    |

---

### 3.3 回應 Header 說明

當 `ReportApiVersions = true` 時，每個回應均附帶版本資訊：

```
api-supported-versions: 1.0, 2.0
api-deprecated-versions: (若有廢棄版本)
```

---

## 4. Controller 版本標記

### 4.1 v1 Controller 標準模式

```csharp
using Asp.Versioning;

[ApiVersion("1.0")]
[RoutePrefix("webapi/aoi/v{version:apiVersion}/aoi01")]
public class AOI01Controller : ApiBaseController
{
    [HttpGet, Route("status")]
    public IHttpActionResult GetStatus() { ... }
}
```

### 4.2 v2 Controller — Breaking-Change 模式

v2 引入新的請求模型（欄位重新命名），並在 Controller 層提供適配器邏輯：

```csharp
[ApiVersion("2.0")]
[RoutePrefix("webapi/aoi/v{version:apiVersion}/aoi01")]
public class AOI01Controller : ApiBaseController  // 注意：不同命名空間 V2
{
    [HttpPost, Route("inspect")]
    public IHttpActionResult Inspect([FromBody] AOIInspectionV2Request request)
    {
        // 適配層：v2 模型轉換為服務層 v1 模型（避免改動服務介面）
        var v1Request = new AOIInspectionRequest
        {
            BatchId        = request.BatchId,
            StationCode    = request.WorkstationCode,  // 欄位更名
            InspectionItems = request.Items,            // 欄位更名
            OperatorId     = request.OperatorId
        };
        return ApiOk(_aoiService.Inspect(v1Request));
    }
}
```

### 4.3 Breaking-Change 對照表（v1 vs v2 AOI01 Inspect）

| 欄位 (v1)          | 欄位 (v2)          | 說明                    |
|--------------------|--------------------|-------------------------|
| `StationCode`      | `WorkstationCode`  | 重新命名                |
| `InspectionItems`  | `Items`            | 重新命名                |
| (不存在)           | `Priority`         | 新增，預設值 "Normal"   |

---

## 5. E2E 測試設定

`TestServerFixture` 需與 `WebApiConfig` 保持一致設定（**先 AddApiVersioning，再 MapHttpAttributeRoutes**，並明確傳入 constraintResolver）：

```csharp
config.AddApiVersioning(options => { /* 同生產環境設定 */ });

var constraintResolver = new DefaultInlineConstraintResolver();
constraintResolver.ConstraintMap.Add("apiVersion", typeof(ApiVersionRouteConstraint));
config.MapHttpAttributeRoutes(constraintResolver);
```

---

## 6. 版本演進策略

```
v1.0 (目前) ─── 維護期 ──────────────────────→ 未來廢棄
v2.0 (目前) ─── 主力版本 ─────────────────────→ 持續擴充
vN.0 (未來) ─── 新增控制器於 Controllers/VN/ ──→ 
```

### 版本管理原則

1. **Non-breaking change**（新增欄位、可選參數）→ 同一版本
2. **Breaking change**（欄位重命名、必填欄位、刪除欄位）→ 新增版本號
3. 舊版本維護至少一個大版本週期才廢棄（標記 `[ApiVersion("1.0", Deprecated = true)]`）
4. 每個版本在獨立資料夾 `Controllers/V1/`、`Controllers/V2/`

---

## 7. 架構決策記錄 (ADR)

| ADR  | 決策                                              | 理由                              |
|------|---------------------------------------------------|-----------------------------------|
| #001 | URL Segment 為主要版本讀取策略                    | 直觀、SEO 友善、易於 Swagger      |
| #002 | 服務層保持 v1 介面，Controller 層做版本適配       | 避免服務層膨脹，關注點分離        |
| #003 | 明確傳入 DefaultInlineConstraintResolver          | Asp.Versioning.WebApi 7.x 不自動修改 DefaultInlineConstraintResolver |
| #004 | 測試環境與生產環境使用相同 versioning 設定        | 確保 E2E 測試覆蓋真實路由行為     |
| #005 | v2 模型獨立檔案（AOIInspectionV2Request.cs）      | 符合單一職責原則，易於維護        |

---

## 8. 已知限制

1. `Asp.Versioning.WebApi` 7.1.0 不支援 .NET Framework 4.8 的自動 constraint 注入，需手動加入 ConstraintMap。
2. 若 Plugin DLL 中的 Controller 也需版本控制，Plugin 的 DLL 也需安裝 `Asp.Versioning.WebApi` 套件。
3. Swagger (NSwag) 的版本化文件需另外設定 `VersionedApiExplorer`。

---

## 附錄 A — 相關檔案清單

| 檔案                                               | 說明                           |
|----------------------------------------------------|--------------------------------|
| `src/SKERPAPI.Host/App_Start/WebApiConfig.cs`      | 主設定，AddApiVersioning 入口   |
| `src/SKERPAPI.AOI/Controllers/V1/AOI01Controller.cs` | v1 AOI 主控制器              |
| `src/SKERPAPI.AOI/Controllers/V2/AOI01Controller.cs` | v2 AOI 主控制器（示範）      |
| `src/SKERPAPI.AOI/Models/AOIInspectionV2Request.cs`  | v2 請求模型                  |
| `tests/SKERPAPI.E2E.Tests/TestServerFixture.cs`    | E2E 測試伺服器，與生產一致     |
| `tests/SKERPAPI.E2E.Tests/AOI_E2ETests.cs`         | v1 + v2 E2E 測試             |
| `tests/SKERPAPI.AOI.Tests/Controllers/AOI01V2ControllerTests.cs` | v2 單元測試 |
