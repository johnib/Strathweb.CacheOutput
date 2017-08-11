using System;

namespace WebApi.OutputCache.V2.Demo.Core
{
    [Obsolete("With multiple instances, this is just as good as the second layer, as the first layer is not updated with values existing in the secon layer")]
    //
    // This implementation won't work. it is as good as the second layer alone as the first layer is not updated of values known by the second layer.
    // The current IOutputCache interface does not provide enough functionality for making this possible.
    //
    public class TwoLayerOutputCache : IOutputCache<byte[]>
    {
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
                result = _secondCache.Get(key);
                
                // Should set the first layer with the result - but with what expiration?
                // Consider creating a new interface, that the Get method also returns the TTL
                // The new interface should conform to the Try___ convention
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