using System.Web.Http.Controllers;

namespace WebApi.OutputCache.V2.Demo.CacheProviders
{
    public interface ICacheKeyGenerator
    {
        string GetCacheKey(HttpActionContext actionContext);
    }
}
