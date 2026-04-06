# SKERPAPI 架構設計文件 (Architecture Document)

## 1. 系統架構理念
SKERPAPI 目標提供強固且容易橫向擴展（模組化）的 API 後台系統，同時保留 .NET Framework 的相容性與資產。
本架構實踐**微內核設計模式 (Microkernel Architecture)**，包含一個核心基礎（Host + Core），並把實際商業邏輯分離至各別的外掛模組中。

## 2. 核心元件
1. **Host 層 (`SKERPAPI.Host`)**:
   負責 API 的 Lifecycle，包括 OWIN / Web API 啟動、Autofac 相依注入掛載、Serilog 初始化，以及掃描 `App_Data/Plugins/` 的動態加載。
2. **Core 層 (`SKERPAPI.Core`)**:
   所有模組共通引用的核心元件，包含全域模型 (`ApiResponse`)、全域 Filter、例外處理過濾器，以及模組介面定義 (`IModuleInitializer`)，絕不涉及商業邏輯。
3. **Module 層 (`SKERPAPI.AOI`, `SKERPAPI.CAR`, 等)**:
   實作真正的領域邏輯，包含 Domain Models、Services 與 Web API Controllers。

### 2.1 依賴反轉與注入 (DI)
使用 `Autofac` 作為核心容器。
為了解決模組繁多導致手動註冊太過複雜的問題，採用約定優於配置的方式：
- 各層 Assembly 只要名稱是 `SKERPAPI.*`，啟動時就透過 Reflection 掃描。
- `ApiControllers` 自動註冊。
- 所有字尾為 `Service` 的類別，皆會以 `AsImplementedInterfaces()` 及 `InstancePerRequest()` (Per HTTP Request) 註冊。

### 2.2 相依性規則 (Dependency Rule)
- `Module` (例如 AOI) **只能** 相依於 `Core`。
- `Module` **不能** 互相依賴 (例如 AOI 不能依賴 CAR)。若有需要通訊可規劃事件總線 (Event Bus) 或向上提取至 `Core`。
- `Host` 依賴於所有內建 Modules 與 `Core`。

## 3. 架構決策紀錄 (ADR)

* **ADR 001: 程式語言自 VB.NET 全數遷移至 C#**
  * **背景**: 舊專案歷史包袱龐大、編譯慢且語法維護不易。
  * **決策**: 以 C# 全面取代 VB.NET 開發，並搭配 SDK-style `.csproj`。
* **ADR 002: 建立動態 Plugin 機制**
  * **背景**: 工廠環境在增加新儀器或新站點的資料對接模組時，無法中斷主要 API 服務。
  * **決策**: 建立 `PluginLoader.cs`，使得放置於特定目錄的 DLL 可在下一次啟動 / 自動重啟時自動讀取，達到微型服務治理的假象，增進佈署彈性。
* **ADR 003: 採用 In-Memory E2E Test Server**
  * **背景**: IIS Express 的啟動延遲過久，導致自動化整合測試容易發生 Port 佔用問題。
  * **決策**: E2E 測試導入 Web API 的 `HttpServer`，不綁定 Socket Port 在本機中全模擬，讓流水線穩定且極速。

## 4. 品質保證：TDD 與持續整合
- **Unit Testing**: 以小單位驗證各別 Service 邏輯與 Controller 行為。不需啟動 HTTP/DI。
- **End-to-End Testing**: 模擬一整個使用者的 Request 從建立 (包含 Json 序列化: camelCase) → Filter 攔截 → Controller 行為 → 返回結果。此驗證了 `Core` 與 `Module` 之間的協作行為與 JSON 格式正確性。

## 5. API 回應設計
採用一致包裝：
```json
{
  "success": true,
  "data": { ... },
  "errorMessage": null,
  "traceId": "9c12df8b3a0f12...",
  "timestamp": "2026-04-06T00:00:00Z"
}
```
並全面導向 **camelCase** 的 JSON Property Name。
