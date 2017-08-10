using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApi.OutputCache.V2.Demo.Core
{
    public interface IOutputCache<T>
    {
        /// <summary>
        /// Adds a new cache entry to the internal cache
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="content">The cache payload</param>
        /// <param name="expiration">Absolute expiration timestamp</param>
        /// <param name="dependsOnKey">A key to be depended on for cache invalidation (i.e. sentinel)</param>
        void Set(string key, T content, DateTimeOffset expiration, string dependsOnKey = null);

        /// <summary>
        /// Checks if a cache entry with the provided key exists
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <returns>True if key exists, false otherise</returns>
        bool Contains(string key);

        /// <summary>
        /// Returns the payload of the cache entry of the provided key
        /// Throws <see cref="KeyNotFoundException"/> if key does not exist
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <returns></returns>
        Task<T> Get(string key);

        /// <summary>
        /// Removes the cache entry of the provided key
        /// </summary>
        /// <param name="key">The cache key</param>
        void Remove(string key);

        /// <summary>
        /// Removes any cache key that depends on the provided key
        /// </summary>
        /// <param name="key">The base cache key (i.e. sentinel)</param>
        void RemoveDependentsOf(string key);
    }
}