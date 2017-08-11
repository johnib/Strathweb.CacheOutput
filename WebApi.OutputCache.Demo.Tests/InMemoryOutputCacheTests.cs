using System.Runtime.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebApi.OutputCache.V2.Demo.Core;

namespace WebApi.OutputCache.Demo.Tests
{
    [TestClass]
    public class InMemoryOutputCacheTests : OutputCacheInterfaceTests
    {
        [TestInitialize]
        public override void Initialize()
        {
            CacheUnderTest = new InMemoryOutputCache<byte[]>(new MemoryCache("test"));
        }
    }
}