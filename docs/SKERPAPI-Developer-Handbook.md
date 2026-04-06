# SKERPAPI 開發手冊

## 1. 專案概述
SKERPAPI 是一個基於 ASP.NET Web API 2 (.NET Framework 4.8) 建置的模組化企業層 API 平台。
系統將功能切分為核心層 (Core)、主機層 (Host) 以及獨立的業務模組 (AOI, CAR 等)，以達高內聚低耦合的架構要求。

技術特點：
* **語言**: C#
* **專案格式**: SDK-style (.NET Sdk)
* **API 框架**: ASP.NET Web API 2
* **依賴注入**: Autofac
* **模組化設計**: 透過 `IModuleInitializer` 自動加載與 Plugin 動態匯入
* **日誌系統**: Serilog（結構化日誌）
* **測試框架**: MSTest + Moq (TDD)，HttpClient / HttpServer (E2E)

## 2. 開發環境與建議
* **IDE**: Visual Studio 2022 (支援 SDK-style .NET 4.8 專案)
* **SDK**: .NET Framework 4.8 開發者套件
* **Git**: `feature/*` 分支開發，請確認提交流程前皆通過測試。

## 3. 專案結構
```text
SKERPAPI.sln
│
├── src/
│   ├── SKERPAPI.Core/      (共用基底、模型、屬性、例外處理)
│   ├── SKERPAPI.Host/      (Web 宿主、應用程式啟動、Plugin 載入器)
│   ├── SKERPAPI.AOI/       (檢測業務模組)
│   └── SKERPAPI.CAR/       (車輛業務模組)
│
├── tests/
│   ├── SKERPAPI.Core.Tests/ (核心單元測試)
│   ├── SKERPAPI.AOI.Tests/  (AOI 單元測試)
│   ├── SKERPAPI.CAR.Tests/  (CAR 單元測試)
│   └── SKERPAPI.E2E.Tests/  (端對端整合測試)
│
└── App_Data/
    ├── Plugins/             (動態模組 DLL 放置路徑)
    └── logs/                (系統日誌輸出)
```

## 4. 模組開發指南
建立新業務模組 (例如 `SKERPAPI.MES`) 時，請依照以下步驟：

### 4.1 專案建置
1. 建立 C# 類別庫 (Class Library)，設定 Target Framework 為 `net48`。
2. 將 `.csproj` 改為 SDK-style。
3. 加入 `SKERPAPI.Core` 的專案參考 (或 DLL 參考)。

### 4.2 實作 IModuleInitializer
在專案根目錄建立類別實作此介面，主機會在啟動時自動呼叫此介面：

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

### 4.3 控制器與服務設計
* **Controller**: 繼承自 `ApiBaseController`，並標註 `[RoutePrefix]`
* **Service**: 需以 `Service` 作為結尾命名，例如 `MesQueryService`，系統（AutofacConfig / PluginLoader）會自動透過 Reflection 注入此介面與實作。

```csharp
using System.Web.Http;
using SKERPAPI.Core.Controllers;

namespace SKERPAPI.MES.Controllers.V1
{
    [RoutePrefix("webapi/mes/v1/workorder")]
    public class WorkOrderController : ApiBaseController
    {
        private readonly IMesQueryService _mesService;

        public WorkOrderController(IMesQueryService mesService)
        {
            _mesService = mesService;
        }

        [HttpGet, Route("{id}")]
        public IHttpActionResult GetWorkOrder(string id)
        {
            var result = _mesService.GetWorkOrderDetails(id);
            return ApiOk(result);
        }
    }
}
```

## 5. Plugin 動態載入
要在不重啟整個 Application 或是重新編譯的狀況下擴充功能：
1. 編譯您的 C# Plugin 為 DLL (`SKERPAPI.MyPlugin.dll`)。
2. 將 DLL 放置於 `SKERPAPI.Host` 的 `App_Data/Plugins/` 目錄。
3. `Host` 下次啟動 / 回收 (IIS AppPool Recycle) 時，`PluginLoader` 掃描到該 DLL 就會自動註冊對應的 Controller 與 Service。

## 6. 測試規範 (TDD 與 E2E)
* 本系統實施測試驅動開發 (TDD) 原則。請在完成 Service 與 Controller 邏輯前，先撰寫 `SKERPAPI.[Module].Tests` 專案中的測試。
* **依賴隔離 (`Moq`)**: 在 Controller 測試中，務必 Mock 對應的 Service 介面，單獨檢驗回應格式與狀態。
* **端對端測試 (E2E)**: 在 `SKERPAPI.E2E.Tests` 專案中，透過 `TestServerFixture` (內建在記憶體執行的 `HttpServer`) 進行完全的系統整合測試，不綁定實際 Port 且支援快速的管線流轉。

## 7. 錯誤處理與回應
請統一使用 `ApiBaseController` 提供的封裝方法，這些方法內部套用了 `ApiResponse<T>`，包含 `Success`、`Data`、`ErrorMessage` 等標準格式：

```csharp
// 成功回應 (會回傳 HTTP 200 與 ApiResponse 包裝)
return ApiOk(data);

// 分頁回傳
return ApiPagedOk(items, totalCount, page, pageSize);

// 自訂報錯
return ApiFail("操作無法完成");

// Not Found
return ApiNotFound("找不到資源");
```

當程式丟出 Exception 時，`ApiExceptionFilter` 會全域捕捉錯誤，在 Release 環境隱藏錯誤並輸出標準 API 錯誤訊息，在 Debug 模式則保留完整錯誤堆疊。
