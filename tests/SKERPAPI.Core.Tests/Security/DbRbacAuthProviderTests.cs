using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKERPAPI.Core.Security.Authorization;

namespace SKERPAPI.Core.Tests.Security
{
    /// <summary>
    /// DbRbacAuthProvider 單元測試。
    /// 測試 RBAC 權限比對邏輯，包含萬用字元 (*) 匹配。
    /// </summary>
    [TestClass]
    public class DbRbacAuthProviderTests
    {
        private DbRbacAuthProvider _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new DbRbacAuthProvider();
        }

        [TestMethod]
        public void HasPermission_UserWithAdminRole_HasAllPermissions()
        {
            // Arrange
            _provider.AssignRole("admin-user", "Admin");

            // Act & Assert
            Assert.IsTrue(_provider.HasPermission("admin-user", "aoi:workorder:create"));
            Assert.IsTrue(_provider.HasPermission("admin-user", "car:vehicle:read"));
            Assert.IsTrue(_provider.HasPermission("admin-user", "admin:config:write"));
        }

        [TestMethod]
        public void HasPermission_AOIOperator_HasOnlyAOIPermissions()
        {
            // Arrange
            _provider.AssignRole("aoi-user", "AOI_Operator");

            // Act & Assert
            Assert.IsTrue(_provider.HasPermission("aoi-user", "aoi:workorder:read"));
            Assert.IsTrue(_provider.HasPermission("aoi-user", "aoi:workorder:create"));
            Assert.IsTrue(_provider.HasPermission("aoi-user", "aoi:inspection:execute"));
            Assert.IsFalse(_provider.HasPermission("aoi-user", "car:vehicle:read"), "AOI Operator 不應有 CAR 權限");
            Assert.IsFalse(_provider.HasPermission("aoi-user", "admin:config:write"), "AOI Operator 不應有 Admin 權限");
        }

        [TestMethod]
        public void HasPermission_ReadOnlyRole_MatchesWildcardRead()
        {
            // Arrange
            _provider.AssignRole("readonly-user", "ReadOnly");

            // Act & Assert - "aoi:*:read" should match any aoi read
            Assert.IsTrue(_provider.HasPermission("readonly-user", "aoi:workorder:read"));
            Assert.IsTrue(_provider.HasPermission("readonly-user", "aoi:inspection:read"));
            Assert.IsTrue(_provider.HasPermission("readonly-user", "car:vehicle:read"));
            Assert.IsFalse(_provider.HasPermission("readonly-user", "aoi:workorder:create"), "ReadOnly 不應有 create 權限");
            Assert.IsFalse(_provider.HasPermission("readonly-user", "car:vehicle:delete"), "ReadOnly 不應有 delete 權限");
        }

        [TestMethod]
        public void HasPermission_UnknownUser_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_provider.HasPermission("unknown-user", "aoi:workorder:read"));
        }

        [TestMethod]
        public void HasPermission_MultipleRoles_CombinesPermissions()
        {
            // Arrange
            _provider.AssignRole("multi-user", "AOI_Operator");
            _provider.AssignRole("multi-user", "CAR_Operator");

            // Act & Assert
            Assert.IsTrue(_provider.HasPermission("multi-user", "aoi:workorder:read"));
            Assert.IsTrue(_provider.HasPermission("multi-user", "car:vehicle:read"));
        }

        [TestMethod]
        public void IsInRole_AssignedRole_ReturnsTrue()
        {
            // Arrange
            _provider.AssignRole("test-user", "Admin");

            // Act & Assert
            Assert.IsTrue(_provider.IsInRole("test-user", "Admin"));
            Assert.IsFalse(_provider.IsInRole("test-user", "NonExistentRole"));
        }

        [TestMethod]
        public void GetRoles_UserWithRoles_ReturnsCorrectRoles()
        {
            // Arrange
            _provider.AssignRole("test-user", "AOI_Operator");
            _provider.AssignRole("test-user", "ReadOnly");

            // Act
            var roles = _provider.GetRoles("test-user").ToList();

            // Assert
            Assert.AreEqual(2, roles.Count);
            CollectionAssert.Contains(roles, "AOI_Operator");
            CollectionAssert.Contains(roles, "ReadOnly");
        }

        [TestMethod]
        public void GetRoles_UnknownUser_ReturnsEmpty()
        {
            // Act
            var roles = _provider.GetRoles("unknown").ToList();

            // Assert
            Assert.AreEqual(0, roles.Count);
        }

        [TestMethod]
        public void GetPermissions_UserWithRoles_ReturnsAllPermissions()
        {
            // Arrange
            _provider.AssignRole("test-user", "AOI_Operator");

            // Act
            var permissions = _provider.GetPermissions("test-user").ToList();

            // Assert
            Assert.IsTrue(permissions.Count > 0);
            Assert.IsTrue(permissions.Contains("aoi:workorder:read"));
        }

        [TestMethod]
        public void AddRolePermission_CustomPermission_IsRecognized()
        {
            // Arrange
            _provider.AddRolePermission("CustomRole", "custom:feature:execute");
            _provider.AssignRole("custom-user", "CustomRole");

            // Act & Assert
            Assert.IsTrue(_provider.HasPermission("custom-user", "custom:feature:execute"));
        }

        [TestMethod]
        public void HasPermission_CaseInsensitive_Matches()
        {
            // Arrange
            _provider.AssignRole("test-user", "Admin");

            // Act & Assert - 權限比對應不分大小寫
            Assert.IsTrue(_provider.HasPermission("test-user", "AOI:WorkOrder:Create"));
        }
    }
}
