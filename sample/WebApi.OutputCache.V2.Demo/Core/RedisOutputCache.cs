using System;
using StackExchange.Redis;

namespace WebApi.OutputCache.V2.Demo.Core
{
    public class RedisOutputCache : IOutputCache<byte[]>
    {
        private const string DependencySetPrefix = "set:";
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly IDatabase _redis;

        public RedisOutputCache(IConnectionMultiplexer connection, int database = -1)
            : this(connection, connection.GetDatabase())
        {
        }

        public RedisOutputCache(IConnectionMultiplexer connection, IDatabase database)
        {
            _multiplexer = connection;
            _redis = database;
        }

        public void Set(string key, byte[] content, DateTimeOffset expiration, string dependsOnKey = null)
        {
            bool wasAdded = _redis.StringSet(key, content, expiration.Subtract(DateTimeOffset.UtcNow));

            if (wasAdded && !string.IsNullOrEmpty(dependsOnKey))
            {
                // TODO: retry on failures
                var setKey = $"{DependencySetPrefix}{dependsOnKey}";
                _redis.SetAdd(setKey, key, CommandFlags.HighPriority);
                _redis.KeyExpire(setKey, expiration.Subtract(DateTimeOffset.UtcNow), CommandFlags.HighPriority);
            }
        }

        public bool Contains(string key)
        {
            return _redis.KeyExists(key);
        }

        public byte[] Get(string key)
        {
            return _redis.StringGet(key);
        }

        public void Remove(string key)
        {
            _redis.KeyDelete(key);
        }

        public void RemoveDependentsOf(string key)
        {
            Remove(key);

            var setKey = $"{DependencySetPrefix}{key}";
            RedisValue[] dependents = _redis.SetMembers(setKey);
            foreach (var dependentKey in dependents)
            {
                Remove(dependentKey);
            }
            
            // This is the way to remove the whole set
            _redis.KeyExpire(setKey, TimeSpan.Zero);
        }
    }
}