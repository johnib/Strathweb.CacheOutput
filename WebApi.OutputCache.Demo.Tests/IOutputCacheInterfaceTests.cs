using System;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebApi.OutputCache.V2.Demo.CacheProviders;

namespace WebApi.OutputCache.Demo.Tests
{
    [TestClass]
    public abstract class OutputCacheInterfaceTests
    {
        private const string DefaultKey = "key";
        private const string DefaultValue = "value";
        private readonly byte[] _defaultValueBytes = Encoding.UTF8.GetBytes(DefaultValue);

        protected IOutputCacheProvider<byte[]> CacheUnderTest;

        [TestInitialize]
        public virtual void Initialize()
        {
        }

        [TestMethod]
        public void TestAdd()
        {
            CacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);

            Assert.AreEqual(DefaultValue, Encoding.UTF8.GetString(CacheUnderTest.Get(DefaultKey)));
        }

        [TestMethod]
        public void TestSetOverridesExistingKey()
        {
            const string existingValue = "existingValue";
            byte[] existingValueBytes = Encoding.UTF8.GetBytes(existingValue);
            CacheUnderTest.Set(DefaultKey, existingValueBytes, DateTimeOffset.MaxValue);
            CacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);
            var currentValue = CacheUnderTest.Get(DefaultKey);

            Assert.AreEqual(DefaultValue, Encoding.UTF8.GetString(currentValue));
        }

        [TestMethod]
        public void TestContainsWhenKeyDoesNotExist()
        {
            var itemExists = CacheUnderTest.Contains(DefaultKey);

            Assert.IsFalse(itemExists);
        }

        [TestMethod]
        public void TestContainsWhenKeyDoesExist()
        {
            CacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);
            var itemExists = CacheUnderTest.Contains(DefaultKey);

            Assert.IsTrue(itemExists);
        }

        [TestMethod]
        public void TestGetDoesNotRemoveCacheEntry()
        {
            CacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);

            Assert.AreEqual(DefaultValue, Encoding.UTF8.GetString(CacheUnderTest.Get(DefaultKey)));
            Assert.AreEqual(DefaultValue, Encoding.UTF8.GetString(CacheUnderTest.Get(DefaultKey)));
        }

        [TestMethod]
        public void TestRemove()
        {
            CacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);
            CacheUnderTest.Remove(DefaultKey);
            var itemExists = CacheUnderTest.Contains(DefaultKey);

            Assert.IsFalse(itemExists);
        }

        [TestMethod]
        public void TestRemoveDependents()
        {
            const string key1 = "key1";
            const string key2 = "key2";

            CacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.MaxValue);
            CacheUnderTest.Set(key1, _defaultValueBytes, DateTimeOffset.MaxValue, DefaultKey);
            CacheUnderTest.Set(key2, _defaultValueBytes, DateTimeOffset.MaxValue, DefaultKey);
            CacheUnderTest.RemoveDependentsOf(DefaultKey);

            var key1Exists = CacheUnderTest.Contains(key1);
            var key2Exists = CacheUnderTest.Contains(key2);

            Assert.IsFalse(key1Exists);
            Assert.IsFalse(key2Exists);
        }

        [TestMethod]
        public void TestCacheKeyExpiration()
        {
            CacheUnderTest.Set(DefaultKey, _defaultValueBytes, DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(50));
            Thread.Sleep(100);

            var doesExist = CacheUnderTest.Contains(DefaultKey);

            Assert.IsFalse(doesExist);
        }
    }
}