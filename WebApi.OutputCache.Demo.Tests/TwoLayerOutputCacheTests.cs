using System.Runtime.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebApi.OutputCache.V2.Demo.CacheProviders;

namespace WebApi.OutputCache.Demo.Tests
{
    [TestClass]
    public class TwoLayerOutputCacheTests : OutputCacheInterfaceTests
    {
        private IOutputCacheProvider<byte[]> _firstLayer;
        private IOutputCacheProvider<byte[]> _secondLayer;

        [TestInitialize]
        public override void Initialize()
        {
            _firstLayer = new InMemoryOutputCacheProvider<byte[]>(new MemoryCache("firstLayer"));
            _secondLayer = new InMemoryOutputCacheProvider<byte[]>(new MemoryCache("secondLayer"));

            CacheUnderTest = new TwoLayerOutputCacheProvider(_firstLayer, _secondLayer);

            base.Initialize();
        }
    }
}