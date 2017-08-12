using System;
using System.Text;

namespace WebApi.OutputCache.V2.Demo.Core
{
    public class TwoLayerOutputCache : IOutputCache<byte[]>
    {
        internal const string ExpirationCacheKey = "ts_exp:";

        private readonly IOutputCache<byte[]> _firstCache;
        private readonly IOutputCache<byte[]> _secondCache;

        public TwoLayerOutputCache(IOutputCache<byte[]> firstLayer, IOutputCache<byte[]> secondLayer)
        {
            _firstCache = firstLayer;
            _secondCache = secondLayer;
        }

        public void Set(string key, byte[] content, DateTimeOffset expiration, string dependsOnKey = null)
        {
            _firstCache.Set(key, content, expiration, dependsOnKey);
            _secondCache.Set(key, content, expiration, dependsOnKey);

            byte[] expirationBytes = Encoding.UTF8.GetBytes(expiration.ToString("o"));
            _secondCache.Set($"{ExpirationCacheKey}{key}", expirationBytes, expiration, key);
        }

        public bool Contains(string key)
        {
            return _firstCache.Contains(key) || _secondCache.Contains(key);
        }

        public byte[] Get(string key)
        {
            byte[] result;
            if ((result = _firstCache.Get(key)) == null)
            {
                // If key is known by second-layer then update the first layer
                if ((result = _secondCache.Get(key)) != null)
                {
                    byte[] expirationBytes;

                    // On rare cases the expiration cache entry may be deleted
                    // In that case we just return the cached value, but do not update the first layer
                    if ((expirationBytes = _secondCache.Get($"{ExpirationCacheKey}{key}")) != null)
                    {
                        DateTimeOffset expiration = DateTimeOffset.Parse(Encoding.UTF8.GetString(expirationBytes));
                        _firstCache.Set(key, result, expiration);
                    }
                }
            }

            return result;
        }

        public void Remove(string key)
        {
            _firstCache.Remove(key);
            _secondCache.Remove(key);
        }

        public void RemoveDependentsOf(string key)
        {
            _firstCache.RemoveDependentsOf(key);
            _secondCache.RemoveDependentsOf(key);
        }
    }
}