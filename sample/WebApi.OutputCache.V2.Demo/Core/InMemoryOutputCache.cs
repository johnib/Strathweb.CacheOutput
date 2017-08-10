using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace WebApi.OutputCache.V2.Demo.Core
{
    public class InMemoryOutputCache<T> : IOutputCache<T>
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

        public void Set(string key, T content, DateTimeOffset expiration, string dependsOnKey = null)
        {
            CacheItemPolicy cachePolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = expiration,
            };

            if (!string.IsNullOrEmpty(dependsOnKey))
            {
                cachePolicy.ChangeMonitors.Add(_memoryCache.CreateCacheEntryChangeMonitor(new[] {dependsOnKey}));
            }

            _memoryCache.Set(key, content, cachePolicy);
        }

        public bool Contains(string key)
        {
            return _memoryCache.Contains(key);
        }

        public Task<T> Get(string key)
        {
            if (Contains(key))
            {
                return Task.FromResult((T) _memoryCache.Get(key));
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