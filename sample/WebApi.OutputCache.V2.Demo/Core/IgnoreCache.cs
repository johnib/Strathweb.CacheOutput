using System;
using System.Web.Http.Filters;

namespace WebApi.OutputCache.V2.Demo.Core
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class IgnoreCache : ActionFilterAttribute
    {
        // No logic
    }
}