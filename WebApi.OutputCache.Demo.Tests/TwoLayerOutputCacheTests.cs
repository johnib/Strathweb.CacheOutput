using System;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StackExchange.Redis;
using WebApi.OutputCache.V2.Demo.CacheProviders;

namespace WebApi.OutputCache.Demo.Tests
{
    [TestClass]
    public class TwoLayerOutputCacheTests : OutputCacheInterfaceTests
    {
        private static ConnectionMultiplexer _connection;
        private static IDatabase _database;

        private IOutputCacheProvider<byte[]> _firstLayer;
        private IOutputCacheProvider<byte[]> _secondLayer;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            const string connectionString =
                "localhost:6379,password=,ssl=False,abortConnect=False";

            ConfigurationOptions redisConfig = ConfigurationOptions.Parse(connectionString);
            redisConfig.AllowAdmin = true;
            _connection = ConnectionMultiplexer.Connect(redisConfig);
            _database = _connection.GetDatabase();
        }

        [TestInitialize]
        public override void Initialize()
        {
            _connection.GetServer(_connection.GetEndPoints().First()).FlushDatabase();

            _firstLayer = new InMemoryOutputCacheProvider<byte[]>(new MemoryCache("test"));
            _secondLayer = new RedisOutputCacheProvider(_connection, _database);

            CacheUnderTest = new TwoLayerOutputCacheProvider(_firstLayer, _secondLayer);

            base.Initialize();
        }
    }

    [TestClass]
    public class TwoLayerOutputCacheUnitTests
    {
        private const string DefaultKey = "key";
        private const string DefaultValue = "value";
        private readonly byte[] _defaultValueBytes = Encoding.UTF8.GetBytes(DefaultValue);

        private Mock<IOutputCacheProvider<byte[]>> _firstLayerMock;
        private Mock<IOutputCacheProvider<byte[]>> _secondLayerMock;

        private IOutputCacheProvider<byte[]> _cacheUnderTest;

        [TestInitialize]
        public void Initialize()
        {
            _firstLayerMock = new Mock<IOutputCacheProvider<byte[]>>();
            _firstLayerMock.Setup(c => c.Contains(It.IsAny<string>()));
            _firstLayerMock.Setup(c => c.Get(It.IsAny<string>()));
            _firstLayerMock.Setup(c => c.Remove(It.IsAny<string>()));
            _firstLayerMock.Setup(c => c.RemoveDependentsOf(It.IsAny<string>()));
            _firstLayerMock.Setup(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()));

            _secondLayerMock = new Mock<IOutputCacheProvider<byte[]>>();
            _secondLayerMock.Setup(c => c.Contains(It.IsAny<string>()));
            _secondLayerMock.Setup(c => c.Get(It.IsAny<string>()));
            _secondLayerMock.Setup(c => c.Remove(It.IsAny<string>()));
            _secondLayerMock.Setup(c => c.RemoveDependentsOf(It.IsAny<string>()));
            _secondLayerMock.Setup(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()));

            _cacheUnderTest = new TwoLayerOutputCacheProvider(_firstLayerMock.Object, _secondLayerMock.Object);
        }

        [TestMethod]
        public void TestContainsReturnsTrueWhenOnlySecondLayerContainsCacheEntry()
        {
            _firstLayerMock.Setup(c => c.Contains(DefaultKey)).Returns(false);
            _secondLayerMock.Setup(c => c.Contains(DefaultKey)).Returns(true);

            Assert.IsTrue(_cacheUnderTest.Contains(DefaultKey));
        }

        [TestMethod]
        public void TestGetUpdatesFirstLayerIfCacheEntryExistsInSecondLayer()
        {
            _firstLayerMock.Setup(c => c.Get(It.IsAny<string>())).Returns((byte[]) null);
            _secondLayerMock.Setup(c => c.Get(DefaultKey)).Returns(_defaultValueBytes);

            DateTimeOffset expirationTime = DateTimeOffset.UtcNow;
            byte[] expirationTimeBytes = Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("o"));
            _secondLayerMock.Setup(c => c.Get($"{TwoLayerOutputCacheProvider.ExpirationCacheKey}{DefaultKey}"))
                .Returns(expirationTimeBytes);

            var resultBytes = _cacheUnderTest.Get(DefaultKey);
            var result = Encoding.UTF8.GetString(resultBytes);

            Assert.AreEqual(DefaultValue, result);
            _firstLayerMock.Verify(c => c.Set(DefaultKey, _defaultValueBytes, expirationTime, null), Times.Once());
        }

        [TestMethod]
        public void TestSetUpdatesBothCacheLayers()
        {
            DateTimeOffset expiration = DateTimeOffset.UtcNow;
            byte[] expirationTimeBytes = Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("o"));

            _cacheUnderTest.Set(DefaultKey, _defaultValueBytes, expiration);

            _firstLayerMock.Verify(c => c.Set(DefaultKey, _defaultValueBytes, expiration, null), Times.Once());
            _secondLayerMock.Verify(c => c.Set(DefaultKey, _defaultValueBytes, expiration, null), Times.Once());
            _secondLayerMock.Verify(c => c.Set($"{TwoLayerOutputCacheProvider.ExpirationCacheKey}{DefaultKey}", expirationTimeBytes, expiration, DefaultKey), Times.Once());
        }

        [TestMethod]
        public void TestRemoveUpdatesBothCacheLayers()
        {
            _cacheUnderTest.Remove(DefaultKey);

            _firstLayerMock.Verify(c => c.Remove(DefaultKey), Times.Once());
            _secondLayerMock.Verify(c => c.Remove(DefaultKey), Times.Once());
        }

        [TestMethod]
        public void TestRemoveDependentsOfUpdatesBothCacheLayers()
        {
            _cacheUnderTest.RemoveDependentsOf(DefaultKey);

            _firstLayerMock.Verify(c => c.RemoveDependentsOf(DefaultKey), Times.Once());
            _secondLayerMock.Verify(c => c.RemoveDependentsOf(DefaultKey), Times.Once());
        }

        [TestMethod]
        public void TestGetWhenGetExpirationReturnsNullThenMainPayloadIsReturnedAndFirstLayerIsNotUpdated()
        {
            _firstLayerMock.Setup(c => c.Get(It.IsAny<string>())).Returns((byte[]) null);
            _secondLayerMock.Setup(c => c.Get(DefaultKey)).Returns(_defaultValueBytes);
            _secondLayerMock.Setup(c => c.Get($"{TwoLayerOutputCacheProvider.ExpirationCacheKey}{DefaultKey}")).Returns((byte[]) null);

            var resultBytes = _cacheUnderTest.Get(DefaultKey);
            var result = Encoding.UTF8.GetString(resultBytes);

            Assert.AreEqual(DefaultValue, result);
            _firstLayerMock.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()), Times.Never());
        }
    }
}