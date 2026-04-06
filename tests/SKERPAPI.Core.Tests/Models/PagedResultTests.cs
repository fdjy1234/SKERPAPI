using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SKERPAPI.Core.Models;

namespace SKERPAPI.Core.Tests.Models
{
    [TestClass]
    public class PagedResultTests
    {
        [TestMethod]
        public void PagedResult_Properties_SetCorrectly()
        {
            // Arrange & Act
            var result = new PagedResult<string>
            {
                Items = new List<string> { "a", "b", "c" },
                TotalCount = 100,
                Page = 2,
                PageSize = 20,
                TotalPages = 5
            };

            // Assert
            Assert.AreEqual(3, result.Items.Count);
            Assert.AreEqual(100, result.TotalCount);
            Assert.AreEqual(2, result.Page);
            Assert.AreEqual(20, result.PageSize);
            Assert.AreEqual(5, result.TotalPages);
        }

        [TestMethod]
        public void PagedResult_EmptyItems_ReturnsEmptyList()
        {
            // Arrange & Act
            var result = new PagedResult<int>
            {
                Items = new List<int>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 20,
                TotalPages = 0
            };

            // Assert
            Assert.AreEqual(0, result.Items.Count);
            Assert.AreEqual(0, result.TotalCount);
        }
    }
}
