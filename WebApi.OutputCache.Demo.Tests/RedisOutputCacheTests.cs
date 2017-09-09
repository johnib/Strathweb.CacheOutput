using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;
using WebApi.OutputCache.V2.Demo.CacheProviders;

namespace WebApi.OutputCache.Demo.Tests
{
    /// <summary>
    /// In order to run these UTs, you need to have a running Redis server (localy or remotely).
    /// Update the connection string below.
    /// 
    /// Run a Redis server localy on Windows:
    /// -------------------------------------
    /// 
    /// 1. Enable linux on your windows:
    /// 1.1 Open elevated Powershell
    /// 1.2 Execute: Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
    /// 1.3 Restart your compute
    /// 
    /// 2 Open Linux shell (bash)
    /// 2.1 Open Powershell
    /// 2.2 Execute: bash
    /// 
    /// 3 Install Redis server
    /// 3.1 Execute: sudo apt-get update
    /// 3.2 Execute: sudo apt-get upgrade
    /// 3.3 Execute: sudo apt-get install redis-server
    /// 
    /// 4 Run Redis server
    /// 4.1 Execute: sudo service redis-server start
    /// </summary>
    [TestClass]
    public class RedisOutputCacheTests : OutputCacheInterfaceTests
    {
        private const string ConnectionString =
            "localhost:6379,password=,ssl=False,abortConnect=False";

        private static ConnectionMultiplexer _connection;
        private static IDatabase _database;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ConfigurationOptions redisConfig = ConfigurationOptions.Parse(ConnectionString);
            redisConfig.AllowAdmin = true;
            _connection = ConnectionMultiplexer.Connect(redisConfig);
            _database = _connection.GetDatabase();
        }

        [TestInitialize]
        public override void Initialize()
        {
            _connection.GetServer(_connection.GetEndPoints().First()).FlushDatabase();
            CacheUnderTest = new RedisOutputCacheProvider(_connection, _database);

            base.Initialize();
        }
    }
}