using System;
using Microsoft.Practices.Unity;
using StackExchange.Redis;
using WebApi.OutputCache.V2.Demo.CacheProviders;

namespace WebApi.OutputCache.V2.Demo.App_Start
{
    /// <summary>
    /// Specifies the Unity configuration for the main container.
    /// </summary>
    public class UnityConfig
    {
        #region Unity Container
        private static Lazy<IUnityContainer> container = new Lazy<IUnityContainer>(() =>
        {
            var container = new UnityContainer();
            RegisterTypes(container);
            return container;
        });

        /// <summary>
        /// Gets the configured Unity container.
        /// </summary>
        public static IUnityContainer GetConfiguredContainer()
        {
            return container.Value;
        }
        #endregion

        /// <summary>Registers the type mappings with the Unity container.</summary>
        /// <param name="container">The unity container to configure.</param>
        /// <remarks>There is no need to register concrete types such as controllers or API controllers (unless you want to 
        /// change the defaults), as Unity allows resolving a concrete type even if it was not previously registered.</remarks>
        public static void RegisterTypes(IUnityContainer container)
        {
            //            container.RegisterType<IOutputCacheProvider<byte[]>>(
            //                new ContainerControlledLifetimeManager(),
            //                new InjectionFactory(unityContainer => new InMemoryOutputCacheProvider<byte[]>()));


            const string connectionString =
                "localhost:6379,password=,ssl=False,abortConnect=False";

            ConfigurationOptions redisConfig = ConfigurationOptions.Parse(connectionString);
            var connection = ConnectionMultiplexer.Connect(redisConfig);
            var database = connection.GetDatabase();

            container.RegisterType<RedisOutputCacheProvider>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(unityContainer => new RedisOutputCacheProvider(connection, database)));

            container.RegisterType<InMemoryOutputCacheProvider<byte[]>>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(unityContainer => new InMemoryOutputCacheProvider<byte[]>()));

            container.RegisterType<IOutputCacheProvider<byte[]>>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(unityContainer => new TwoLayerOutputCacheProvider(
                    (IOutputCacheProvider<byte[]>) container.Resolve(typeof(InMemoryOutputCacheProvider<byte[]>)),
                    (IOutputCacheProvider<byte[]>) container.Resolve(typeof(RedisOutputCacheProvider)))));

//            container.RegisterType<MemoryCacheDefault>(
//                new ContainerControlledLifetimeManager(),
//                new InjectionFactory(unityContainer => new MemoryCacheDefault()));

            container.RegisterType<ICacheKeyGenerator>(
                new ContainerControlledLifetimeManager(),
                new InjectionFactory(unityContainer => new DefaultCacheKeyGenerator()));
        }
    }
}
