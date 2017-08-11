using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using WebApi.OutputCache.V2.Demo.Core;

namespace WebApi.OutputCache.Demo.Tests
{
    [TestClass]
    public class RedisOutputCacheTests : OutputCacheInterfaceTests
    {
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
        public override void Initialize()
        {
            _connection.GetServer(_connection.GetEndPoints().First()).FlushDatabase();
            CacheUnderTest = new RedisOutputCache(_connection, _database);

            base.Initialize();
        }
    }
}