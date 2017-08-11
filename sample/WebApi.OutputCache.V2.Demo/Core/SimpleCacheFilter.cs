using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace WebApi.OutputCache.V2.Demo.Core
{
    public class SimpleCacheFilter : ActionFilterAttribute
    {
        private static readonly MediaTypeHeaderValue ContentType =
            MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

        private static readonly InMemoryOutputCache<byte[]> InMemoryCache = new InMemoryOutputCache<byte[]>();

        private readonly TimeSpan _cacheTime;
        private readonly IOutputCache<byte[]> _cache;

        public SimpleCacheFilter(TimeSpan cacheTime)
        {
            _cacheTime = cacheTime;
            _cache = InMemoryCache;
        }

        public SimpleCacheFilter(long cacheTimeSeconds) : this(TimeSpan.FromSeconds(cacheTimeSeconds))
        {
        }

        /// <summary>
        /// Check if the response is already cached and return response if it does.
        /// </summary>
        /// <param name="actionContext"></param>
        /// <returns></returns>
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            string cacheKey = GetCacheKey(actionContext);

            if (_cache.Contains(cacheKey))
            {
                byte[] cachedResponse = _cache.Get(cacheKey);
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.NotModified);
                actionContext.Response.Content = new ByteArrayContent(cachedResponse);
                actionContext.Response.Content.Headers.ContentType = ContentType;
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
            if (ShouldCacheResponse(actionExecutedContext))
            {
                string cacheKey = GetCacheKey(actionExecutedContext.ActionContext);
                byte[] content = await actionExecutedContext.Response.Content.ReadAsByteArrayAsync();
                DateTimeOffset cacheExpiration = GetAbsoluteExpiration(_cacheTime);

                _cache.Set(cacheKey, content, cacheExpiration);
            }
        }

        private static bool ShouldCacheResponse(HttpActionExecutedContext actionExecutedContext)
        {
            return actionExecutedContext.Request.Method == HttpMethod.Get &&
                   actionExecutedContext.Response.IsSuccessStatusCode &&
                   actionExecutedContext.Response.Content != null;
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