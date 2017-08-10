using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace WebApi.OutputCache.V2.Demo.Core
{
    public class InMemoryOutputCache : IOutputCache<byte[]>
    {
        #region Members

        private readonly MemoryCache _memoryCache;

        #endregion

        #region Constructors

        public InMemoryOutputCache() : this(MemoryCache.Default)
        {
        }

        public InMemoryOutputCache(MemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        #endregion

        #region Interface

        public void Add(string key, byte[] content, DateTimeOffset expiration, string dependsOnKey = null)
        {
            CacheItemPolicy cachePolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = expiration,
            };

            if (!string.IsNullOrEmpty(dependsOnKey))
            {
                cachePolicy.ChangeMonitors.Add(_memoryCache.CreateCacheEntryChangeMonitor(new[] {dependsOnKey}));
            }

            _memoryCache.Add(key, content, cachePolicy);
        }

        public bool Contains(string key)
        {
            return _memoryCache.Contains(key);
        }

        public Task<byte[]> Get(string key)
        {
            if (Contains(key))
            {
                return Task.FromResult((byte[]) _memoryCache.Get(key));
            }

            throw new KeyNotFoundException(key);
        }

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
        }

        public void RemoveDependentsOf(string key)
        {
            // In this implementation, the dependencies removal occurs when this key is removed
            // This works this way because any dependents that were added have the ChangeMonitors
            this.Remove(key);
        }

        #endregion
    }
}