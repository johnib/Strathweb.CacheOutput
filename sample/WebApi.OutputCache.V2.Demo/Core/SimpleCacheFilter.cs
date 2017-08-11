using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Filters;

namespace WebApi.OutputCache.V2.Demo.Core
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SimpleCacheFilter : ActionFilterAttribute
    {
        private static readonly MediaTypeHeaderValue ContentType =
            MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

        public TimeSpan CacheTime => new TimeSpan(Days, Hours, Minutes, Seconds, Milliseconds);

        public int Milliseconds { get; set; } = 0;
        public int Seconds { get; set; } = 0;
        public int Minutes { get; set; } = 0;
        public int Hours { get; set; } = 0;
        public int Days { get; set; } = 0;

        /// <summary>
        /// Check if the response is already cached and return response if it does.
        /// </summary>
        /// <param name="actionContext"></param>
        /// <returns></returns>
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            // TODO: validate request method is GET
            IOutputCache<byte[]> cache = ResolveCacheDependency(actionContext);
            if (cache == null) return;

            string cacheKey = GetCacheKey(actionContext);
            if (cache.Contains(cacheKey))
            {
                byte[] cachedResponse = cache.Get(cacheKey);
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
            IOutputCache<byte[]> cache = ResolveCacheDependency(actionExecutedContext.ActionContext);
            if (cache == null) return;

            if (ShouldCacheResponse(actionExecutedContext))
            {
                string cacheKey = GetCacheKey(actionExecutedContext.ActionContext);
                byte[] content = await actionExecutedContext.Response.Content.ReadAsByteArrayAsync();
                DateTimeOffset cacheExpiration = GetAbsoluteExpiration(CacheTime);

                cache.Set(cacheKey, content, cacheExpiration);
            }
        }

        private static IOutputCache<byte[]> ResolveCacheDependency(HttpActionContext context)
        {
            IDependencyResolver dependencyResolver = context.ControllerContext.Configuration.DependencyResolver;
            return dependencyResolver.GetService(typeof(IOutputCache<byte[]>)) as IOutputCache<byte[]>;
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
            return actionContext.ActionArguments.Where(arg => !IgnoreInputParams.Contains(arg.Key)).ToList();
        }

        private static DateTimeOffset GetAbsoluteExpiration(TimeSpan cacheTime)
        {
            return DateTimeOffset.UtcNow + cacheTime;
        }
    }
}