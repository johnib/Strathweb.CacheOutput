using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Controllers;

namespace WebApi.OutputCache.V2.Demo.CacheProviders
{
    public class DefaultCacheKeyGenerator : ICacheKeyGenerator
    {
        private static readonly IEnumerable<string> IgnoreInputParams = new[] {"callback"};

        public string GetCacheKey(HttpActionContext actionContext)
        {
            var controllerName = actionContext.ControllerContext.ControllerDescriptor.ControllerName;
            var actionName = actionContext.ActionDescriptor.ActionName;
            var queryParams = string.Join(";", GetActionInputParams(actionContext)
                .Where(kv => kv.Value != null)
                .OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}={kv.Value}"));

            var cacheKey = string.Join("-", controllerName, actionName, queryParams);

            return cacheKey;
        }

        private static IEnumerable<KeyValuePair<string, object>> GetActionInputParams(HttpActionContext actionContext)
        {
            return actionContext.ActionArguments
                .Where(arg => !IgnoreInputParams.Contains(arg.Key))
                .ToList();
        }
    }
}
