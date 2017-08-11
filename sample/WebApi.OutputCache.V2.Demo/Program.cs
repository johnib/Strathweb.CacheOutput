using System;
using System.Runtime.Caching;
using System.Web.Http;
using System.Web.Http.SelfHost;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.WebApi;
using WebApi.OutputCache.V2.Demo.Core;

namespace WebApi.OutputCache.V2.Demo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var container = new UnityContainer())
            {
                container.RegisterType<IOutputCache<byte[]>>(
                    new ContainerControlledLifetimeManager(),
                    new InjectionFactory(unityContainer => new InMemoryOutputCache<byte[]>()));

                HttpSelfHostConfiguration config = new HttpSelfHostConfiguration("http://localhost:8000")
                {
                    DependencyResolver = new UnityDependencyResolver(container)
                };

                config.MapHttpAttributeRoutes();
                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{id}",
                    defaults: new {id = RouteParameter.Optional}
                );

                HttpSelfHostServer server = new HttpSelfHostServer(config);
                //            config.CacheOutputConfiguration().RegisterCacheOutputProvider(() => new MemoryCacheDefault());

                server.OpenAsync().Wait();

                Console.ReadKey();
                server.CloseAsync().Wait();
            }
        }
    }
}