namespace SKERPAPI.Core.Permissions
{
    /// <summary>
    /// RBAC 權限常量定義。
    /// 格式：module:resource:action
    /// 由 RbacAuthorizeAttribute 的 Permission 參數引用，避免魔術字串。
    /// </summary>
    public static class RbacPermissions
    {
        /// <summary>AOI 模組權限</summary>
        public static class Aoi
        {
            /// <summary>讀取 AOI 系統狀態 (GET aoi01/status)</summary>
            public const string StatusRead = "aoi:status:read";

            /// <summary>執行 AOI 檢測 (POST aoi01/inspect)</summary>
            public const string InspectionExecute = "aoi:inspection:execute";

            /// <summary>查詢 AOI 檢測歷史 (GET aoi01/history)</summary>
            public const string InspectionHistory = "aoi:inspection:history";

            /// <summary>讀取 AOI 設備清單 (GET aoi02/devices)</summary>
            public const string DeviceRead = "aoi:device:read";
        }

        /// <summary>CAR 模組權限</summary>
        public static class Car
        {
            /// <summary>讀取 CAR 系統資訊 (GET car01/info)</summary>
            public const string SystemRead = "car:system:read";

            /// <summary>註冊新車輛 (POST car01/register)</summary>
            public const string VehicleRegister = "car:vehicle:register";

            /// <summary>讀取車輛資料 (GET car01/list, GET car01/{carId})</summary>
            public const string VehicleRead = "car:vehicle:read";

            /// <summary>讀取維修紀錄 (GET car02/maintenance/{carId})</summary>
            public const string MaintenanceRead = "car:maintenance:read";

            /// <summary>新增維修紀錄 (POST car02/maintenance)</summary>
            public const string MaintenanceCreate = "car:maintenance:create";
        }
    }
}
