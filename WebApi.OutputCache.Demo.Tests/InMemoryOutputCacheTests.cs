using System;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using WebApi.OutputCache.V2.Demo.Core;

namespace WebApi.OutputCache.Demo.Tests
{
    [TestClass]
    public class InMemoryOutputCacheTests
    {
        private const string DefaultKey = "key";
        private const string DefaultValue = "value";
        private readonly byte[] _defaultValueBytes = Encoding.UTF8.GetBytes(DefaultValue);

        private IOutputCache<byte[]> _cacheUnderTest;
        private static ConnectionMultiplexer _connection;
        private static IDatabase _database;

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
        public void Initialize()
        {
//            _cacheUnderTest = new InMemoryOutputCache<byte[]>(new MemoryCache("test"));

            _connection.GetServer(_connection.GetEndPoints().First()).FlushDatabase();
            _cacheUnderTest = new RedisOutputCache(_connection, _database);
        }

        [TestMethod]
        public void TestAdd()
        {
            _cacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);

            Assert.AreEqual(DefaultValue, Encoding.UTF8.GetString(_cacheUnderTest.Get(DefaultKey)));
        }

        [TestMethod]
        public void TestSetOverridesExistingKey()
        {
            const string existingValue = "existingValue";
            byte[] existingValueBytes = Encoding.UTF8.GetBytes(existingValue);
            _cacheUnderTest.Set(DefaultKey, existingValueBytes, DateTimeOffset.MaxValue);
            _cacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);
            var currentValue = _cacheUnderTest.Get(DefaultKey);

            Assert.AreEqual(DefaultValue, Encoding.UTF8.GetString(currentValue));
        }

        [TestMethod]
        public void TestContainsWhenKeyDoesNotExist()
        {
            var itemExists = _cacheUnderTest.Contains(DefaultKey);

            Assert.IsFalse(itemExists);
        }

        [TestMethod]
        public void TestContainsWhenKeyDoesExist()
        {
            _cacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);
            var itemExists = _cacheUnderTest.Contains(DefaultKey);

            Assert.IsTrue(itemExists);
        }

        [TestMethod]
        public void TestGetDoesNotRemoveCacheEntry()
        {
            _cacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);

            Assert.AreEqual(DefaultValue, Encoding.UTF8.GetString(_cacheUnderTest.Get(DefaultKey)));
            Assert.AreEqual(DefaultValue, Encoding.UTF8.GetString(_cacheUnderTest.Get(DefaultKey)));
        }

        [TestMethod]
        public void TestRemove()
        {
            _cacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);
            _cacheUnderTest.Remove(DefaultKey);
            var itemExists = _cacheUnderTest.Contains(DefaultKey);

            Assert.IsFalse(itemExists);
        }

        [TestMethod]
        public void TestRemoveDependents()
        {
            const string key1 = "key1";
            const string key2 = "key2";

            _cacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);
            _cacheUnderTest.Set(key1, _defaultValueBytes, DateTimeOffset.MaxValue, DefaultKey);
            _cacheUnderTest.Set(key2, _defaultValueBytes, DateTimeOffset.MaxValue, DefaultKey);
            _cacheUnderTest.RemoveDependentsOf(DefaultKey);

            var key1Exists = _cacheUnderTest.Contains(key1);
            var key2Exists = _cacheUnderTest.Contains(key2);

            Assert.IsFalse(key1Exists);
            Assert.IsFalse(key2Exists);
        }

        [TestMethod]
        public void TestCacheKeyExpiration()
        {
            _cacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(50));
            Thread.Sleep(100);

            var doesExist = _cacheUnderTest.Contains(DefaultKey);

            Assert.IsFalse(doesExist);
        }
    }
}