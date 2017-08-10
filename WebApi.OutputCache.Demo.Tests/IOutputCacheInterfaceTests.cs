using System;
using System.Runtime.Caching;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebApi.OutputCache.V2.Demo.Core;

namespace WebApi.OutputCache.Demo.Tests
{
    [TestClass]
    public class InMemoryOutputCacheTests
    {
        private const string DefaultKey = "key";
        private const string DefaultValue = "value";

        private IOutputCache<string> _cacheUnderTest;

        [TestInitialize]
        public void Initialize()
        {
            _cacheUnderTest = new InMemoryOutputCache<string>(new MemoryCache("test"));
        }

        [TestMethod]
        public void TestAdd()
        {
            _cacheUnderTest.Set(DefaultKey, DefaultValue, DateTimeOffset.MaxValue);
            var value = _cacheUnderTest.Get(DefaultKey);

            Assert.AreEqual(DefaultValue, value);
        }

        [TestMethod]
        public void TestSetOverridesExistingKey()
        {
            const string existingValue = "existingValue";
            _cacheUnderTest.Set(DefaultKey, existingValue, DateTimeOffset.MaxValue);
            _cacheUnderTest.Set(DefaultKey, DefaultValue, DateTimeOffset.MaxValue);
            var currentValue = _cacheUnderTest.Get(DefaultKey);

            Assert.AreEqual(DefaultValue, currentValue);
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
            _cacheUnderTest.Set(DefaultKey, DefaultValue, DateTimeOffset.MaxValue);
            var itemExists = _cacheUnderTest.Contains(DefaultKey);

            Assert.IsTrue(itemExists);
        }

        [TestMethod]
        public void TestGetDoesNotRemoveCacheEntry()
        {
            _cacheUnderTest.Set(DefaultKey, DefaultValue, DateTimeOffset.MaxValue);

            Assert.AreEqual(DefaultValue, _cacheUnderTest.Get(DefaultKey));
            Assert.AreEqual(DefaultValue, _cacheUnderTest.Get(DefaultKey));
        }

        [TestMethod]
        public void TestRemove()
        {
            _cacheUnderTest.Set(DefaultKey, DefaultValue, DateTimeOffset.MaxValue);
            _cacheUnderTest.Remove(DefaultKey);
            var itemExists = _cacheUnderTest.Contains(DefaultKey);

            Assert.IsFalse(itemExists);
        }

        [TestMethod]
        public void TestRemoveDependents()
        {
            const string key1 = "key1";
            const string key2 = "key2";

            _cacheUnderTest.Set(DefaultKey, DefaultValue, DateTimeOffset.MaxValue);
            _cacheUnderTest.Set(key1, DefaultValue, DateTimeOffset.MaxValue, DefaultKey);
            _cacheUnderTest.Set(key2, DefaultValue, DateTimeOffset.MaxValue, DefaultKey);
            _cacheUnderTest.RemoveDependentsOf(DefaultKey);

            var key1Exists = _cacheUnderTest.Contains(key1);
            var key2Exists = _cacheUnderTest.Contains(key2);

            Assert.IsFalse(key1Exists);
            Assert.IsFalse(key2Exists);
        }

        [TestMethod]
        public void TestCacheKeyExpiration()
        {
            _cacheUnderTest.Set(DefaultKey, DefaultValue, DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(50));
            Thread.Sleep(100);

            var doesExist = _cacheUnderTest.Contains(DefaultKey);

            Assert.IsFalse(doesExist);
        }
    }
}