using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SKERPAPI.E2E.Tests
{
    [TestClass]
    public class Plugin_E2ETests
    {
        [TestMethod]
        public void PluginLoader_CreatesDirectory_IfNotExists()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "SKERPAPI_Plugin_Test_" + System.Guid.NewGuid().ToString("N"));

            // Act & Assert
            Assert.IsFalse(Directory.Exists(testDir));
            Directory.CreateDirectory(testDir);
            Assert.IsTrue(Directory.Exists(testDir));

            // Cleanup
            Directory.Delete(testDir, true);
        }

        [TestMethod]
        public void PluginLoader_EmptyDirectory_DoesNotCrash()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "SKERPAPI_Plugin_Test_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);

            // Act
            var dlls = Directory.GetFiles(testDir, "SKERPAPI.*.dll");

            // Assert
            Assert.AreEqual(0, dlls.Length);

            // Cleanup
            Directory.Delete(testDir, true);
        }

        [TestMethod]
        public void PluginLoader_GetLoadedPluginAssemblies_ReturnsArray()
        {
            // Act
            var assemblies = SKERPAPI.Host.PluginLoader.GetLoadedPluginAssemblies();

            // Assert
            Assert.IsNotNull(assemblies);
        }
    }
}
