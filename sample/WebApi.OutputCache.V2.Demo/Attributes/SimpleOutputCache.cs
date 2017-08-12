using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Filters;
using WebApi.OutputCache.V2.Demo.CacheProviders;

namespace WebApi.OutputCache.V2.Demo.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SimpleOutputCache : ActionFilterAttribute
    {
        #region Constants

        private static readonly MediaTypeHeaderValue ContentType =
            MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

        private static readonly IEnumerable<string> IgnoreInputParams = new[] {"callback"};

        #endregion

        #region Properties

        public TimeSpan CacheTime => new TimeSpan(Days, Hours, Minutes, Seconds, Milliseconds);

        public int Milliseconds { get; set; }
        public int Seconds { get; set; }
        public int Minutes { get; set; }
        public int Hours { get; set; }
        public int Days { get; set; }

        #endregion

        #region Public

        /// <summary>
        /// Check if the response is already cached and return response if it does.
        /// </summary>
        /// <param name="actionContext"></param>
        /// <returns></returns>
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (actionContext.ActionDescriptor.GetCustomAttributes<IgnoreCache>().Any()) return;
            if (actionContext.Request.Method != HttpMethod.Get) return;

            IOutputCacheProvider<byte[]> cache = ResolveCacheDependency(actionContext);
            if (cache == null)
            {
                Trace.TraceWarning($"{nameof(SimpleOutputCache)}: no cache provider found, handling request normally");
                return;
            }

            string cacheKey = GetCacheKey(actionContext);
            byte[] cachedResponse;
            if ((cachedResponse = cache.Get(cacheKey)) != null)
            {
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
            HttpActionContext actionContext = actionExecutedContext.ActionContext;
            if (actionContext.ActionDescriptor.GetCustomAttributes<IgnoreCache>().Any()) return;

            IOutputCacheProvider<byte[]> cache = ResolveCacheDependency(actionContext);
            if (cache == null)
            {
                Trace.TraceWarning($"{nameof(SimpleOutputCache)}: no cache provider found, handling request normally");
                return;
            }

            if (ShouldCacheResponse(actionExecutedContext))
            {
                string cacheKey = GetCacheKey(actionContext);
                byte[] content = await actionExecutedContext.Response.Content.ReadAsByteArrayAsync();
                DateTimeOffset cacheExpiration = GetAbsoluteExpiration(CacheTime);

                cache.Set(cacheKey, content, cacheExpiration);
            }
        }

        #endregion

        #region Private

        private static IOutputCacheProvider<byte[]> ResolveCacheDependency(HttpActionContext context)
        {
            IDependencyResolver dependencyResolver = context.ControllerContext.Configuration.DependencyResolver;
            return dependencyResolver.GetService(typeof(IOutputCacheProvider<byte[]>)) as IOutputCacheProvider<byte[]>;
        }

        private static bool ShouldCacheResponse(HttpActionExecutedContext actionExecutedContext)
        {
            var response = actionExecutedContext.Response;
            var mustHave = actionExecutedContext.Request.Method == HttpMethod.Get &&
                           response.IsSuccessStatusCode &&
                           response.Content != null;

            // If response.Content is ObjectContent then the content length is unknown at this stage
            // Thus we do not enforce contentLength > 0
            return response.Content is ObjectContent
                ? mustHave
                : mustHave && response.Content.Headers.ContentLength > 0;
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

        private static IEnumerable<KeyValuePair<string, object>> GetActionInputParams(HttpActionContext actionContext)
        {
            return actionContext.ActionArguments
                .Where(arg => !IgnoreInputParams.Contains(arg.Key))
                .ToList();
        }

        private static DateTimeOffset GetAbsoluteExpiration(TimeSpan cacheTime)
        {
            return DateTimeOffset.UtcNow + cacheTime;
        }

        #endregion
    }
}