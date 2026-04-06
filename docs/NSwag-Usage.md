NSwag 使用說明（針對 ASP.NET Web API / Visual Basic）

目標
- 在 Web API 專案啟用 NSwag 以產生 OpenAPI (Swagger) JSON 並提供 Swagger UI。

先決條件
- 專案使用 packages.config 或 NuGet 管理套件。
- 建議安裝：
  - `NSwag.AspNet.WebApi`（會自動帶入 `NSwag.Generation.WebApi` 與 `NSwag.Core` 等）

安裝（在 Visual Studio 的 NuGet Package Manager Console 執行）：

Install-Package NSwag.AspNet.WebApi -Version 14.6.3

或在 GUI 裡安裝 `NSwag.AspNet.WebApi`。

編譯時整合（範例）
- 在 `App_Start\WebApiConfig.vb` 頂端加入：

```vb
Imports NSwag.AspNet.WebApi
```

- 在 `Register` 方法中加入：

```vb
' 產生 Swagger JSON 路由
config.MapSwaggerRoute()

' 註冊 Swagger UI（可透過 settings 自訂標題、路徑等）
config.MapSwaggerUiRoute(Function(settings)
    settings.DocumentTitle = "WebApplication1 API Documentation"
    ' settings.Path = "swagger"  '（如果需要改 UI 路徑）
End Function)
```

注意：如果你在編譯時看到錯誤（像是 `MapSwaggerRoute` 不是 `HttpConfiguration` 的成員），表示：
- 專案尚未還原 NuGet 套件，或
- 專案引用中沒有對應的 NSwag 程式庫。

請先還原/安裝套件，然後 Rebuild。

執行時（驗證）
- 啟動 Web API，檢查：
  - Swagger JSON：`/swagger/docs/v1`（或套件版本預設路徑）
  - Swagger UI：通常 `/swagger` 或 `/swagger/ui/index`（依版本而異）

進階：產生用戶端程式碼
- 可用 `NSwagStudio`、`nswag.exe` 或 `NSwag.MSBuild` 來自動產生 TypeScript/C# 等客戶端程式碼。

示例：在 CI 用 `nswag run /runtime:Net60 nswag.json` 來產生代理。

備註：本專案已包含一個「執行時 shim」(App_Start/NSwagExtensions.vb)，
- 目的：當開發機或某些環境沒有安裝 NSwag 時仍能編譯與執行。
- 若你已安裝並還原 NSwag 套件，建議直接用上面的「編譯時整合」方式（可得到更完整的設定 IntelliSense）。

若要我：
- 幫你把 `WebApiConfig` 改回直接編譯時呼叫（我會新增 `Imports NSwag.AspNet.WebApi` 並移除 shim），或
- 幫你新增一組範例 `nswag.json` 並示範如何在 CI 中用 `NSwag.MSBuild` 產生客戶端，請直接選擇。