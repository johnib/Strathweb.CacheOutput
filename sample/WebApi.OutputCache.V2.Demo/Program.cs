using System;
using System.Web.Http;
using System.Web.Http.SelfHost;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.WebApi;
using WebApi.OutputCache.V2.Demo.App_Start;

namespace WebApi.OutputCache.V2.Demo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            HttpSelfHostConfiguration config = new HttpSelfHostConfiguration("http://localhost:8000")
            {
                DependencyResolver = new UnityDependencyResolver(UnityConfig.GetConfiguredContainer())
            };

            config.MapHttpAttributeRoutes();
            HttpSelfHostServer server = new HttpSelfHostServer(config);
            //            config.CacheOutputConfiguration().RegisterCacheOutputProvider(() => new MemoryCacheDefault());

            server.OpenAsync().Wait();

            Console.ReadKey();
            server.CloseAsync().Wait();
        }
    }
}