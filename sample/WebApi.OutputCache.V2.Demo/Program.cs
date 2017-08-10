using System;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace WebApi.OutputCache.V2.Demo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            HttpSelfHostConfiguration config = new HttpSelfHostConfiguration("http://localhost:8000");
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