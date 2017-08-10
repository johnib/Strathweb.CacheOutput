using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace WebApi.OutputCache.V2.Demo
{
    public class SimpleCacheFilter : ActionFilterAttribute
    {
        private static readonly MemoryCache MemoryCache = MemoryCache.Default;
        private readonly TimeSpan _cacheTime;

        public SimpleCacheFilter(TimeSpan cacheTime)
        {
            _cacheTime = cacheTime;
        }

        public SimpleCacheFilter(long cacheTimeSeconds) : this(TimeSpan.FromSeconds(cacheTimeSeconds))
        {
        }

        /// <summary>
        /// Check if the response is already cached and return response if it does.
        /// </summary>
        /// <param name="actionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task OnActionExecutingAsync(HttpActionContext actionContext,
            CancellationToken cancellationToken)
        {
            string cacheKey = GetCacheKey(actionContext);

            if (MemoryCache.Contains(cacheKey))
            {
                string cachedResponse = (string) MemoryCache.Get(cacheKey);
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.NotModified, cachedResponse);
            }
        }

        /// <summary>
        /// Cache new response
        /// </summary>
        /// <param name="actionExecutedContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext,
            CancellationToken cancellationToken)
        {
            string cacheKey = GetCacheKey(actionExecutedContext.ActionContext);
            string content = await actionExecutedContext.Response.Content.ReadAsStringAsync();
            DateTimeOffset cacheExpiration = GetAbsoluteExpiration(_cacheTime);

            MemoryCache.Set(cacheKey, content, cacheExpiration);
        }

        private static string GetCacheKey(HttpActionContext actionContext)
        {
            var controllerName = actionContext.ControllerContext.ControllerDescriptor.ControllerName;
            var actionName = actionContext.ActionDescriptor.ActionName;
            var queryParams = string.Join(";", GetActionInputParams(actionContext)
                .Where(kv => kv.Value != null)
                .OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}={kv.Value}"));

            var cacheKey = string.Join("-", controllerName, actionName, queryParams);
            Console.WriteLine(cacheKey);

            return cacheKey;
        }

        private static readonly IEnumerable<string> IgnoreInputParams = new[] {"callback"};
        private static IEnumerable<KeyValuePair<string, object>> GetActionInputParams(HttpActionContext actionContext)
        {
            return actionContext.ActionArguments.Where(arg => !IgnoreInputParams.Contains(arg.Key));
        }

        private static DateTimeOffset GetAbsoluteExpiration(TimeSpan cacheTime)
        {
            return DateTimeOffset.UtcNow + cacheTime;
        }
    }
}