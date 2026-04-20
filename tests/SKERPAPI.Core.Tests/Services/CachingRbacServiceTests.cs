using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SKERPAPI.Core.Interfaces;
using SKERPAPI.Core.Models;
using SKERPAPI.Core.Services;

namespace SKERPAPI.Core.Tests.Services
{
    [TestClass]
    public class CachingRbacServiceTests
    {
        private static RbacContext MakeContext(string apiKey = "key-1") =>
            new RbacContext
            {
                IsAuthenticated = true,
                ApiKey = apiKey,
                Permissions = new System.Collections.Generic.List<string> { "aoi:status:read" }
            };

        // 每個測試用獨立的 MemoryCache 避免跨測試污染
        private static MemoryCache NewCache() =>
            new MemoryCache(Guid.NewGuid().ToString());

        // ---------------------------------------------------------------
        // Test 1: Cache 為空時，呼叫 inner service
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task CallsInnerService_WhenCacheEmpty()
        {
            var innerMock = new Mock<IRbacService>();
            innerMock.Setup(s => s.ResolveAsync("key-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(MakeContext("key-1"));

            var sut = new CachingRbacService(innerMock.Object, TimeSpan.FromMinutes(5), NewCache());

            var result = await sut.ResolveAsync("key-1", CancellationToken.None);

            Assert.IsTrue(result.IsAuthenticated);
            innerMock.Verify(s => s.ResolveAsync("key-1", It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------------------------------------------------------------
        // Test 2: 第二次呼叫同一 Key → 回傳快取結果，不呼叫 inner
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task ReturnsCachedResult_OnSecondCallWithSameKey()
        {
            var innerMock = new Mock<IRbacService>();
            innerMock.Setup(s => s.ResolveAsync("key-2", It.IsAny<CancellationToken>()))
                .ReturnsAsync(MakeContext("key-2"));

            var sut = new CachingRbacService(innerMock.Object, TimeSpan.FromMinutes(5), NewCache());

            var first = await sut.ResolveAsync("key-2", CancellationToken.None);
            var second = await sut.ResolveAsync("key-2", CancellationToken.None);

            Assert.AreEqual(first, second, "Should return same cached instance.");
            innerMock.Verify(s => s.ResolveAsync("key-2", It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------------------------------------------------------------
        // Test 3: TTL 到期後，下次呼叫重新查詢 inner service
        // ---------------------------------------------------------------
        [TestMethod]
        public async Task CallsInnerAgain_AfterCacheExpiry()
        {
            var innerMock = new Mock<IRbacService>();
            innerMock.Setup(s => s.ResolveAsync("key-3", It.IsAny<CancellationToken>()))
                .ReturnsAsync(MakeContext("key-3"));

            // TTL = 50ms，確保快取快速過期
            var sut = new CachingRbacService(
                innerMock.Object,
                TimeSpan.FromMilliseconds(50),
                NewCache());

            await sut.ResolveAsync("key-3", CancellationToken.None);

            // 等待快取過期
            await Task.Delay(200);

            await sut.ResolveAsync("key-3", CancellationToken.None);

            innerMock.Verify(s => s.ResolveAsync("key-3", It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}
