using System.Threading.Tasks;
using System.Web.Http;
using WebApi.OutputCache.V2.Demo.Core;

namespace WebApi.OutputCache.V2.Demo
{
    [RoutePrefix("api/echo")]
    [SimpleOutputCache(Seconds = 15)]
    public class EchoController : ApiController
    {
        [HttpGet]
        [Route("{userId}/{message}")]
        [SimpleOutputCache(Seconds = 3)]
        public async Task<IHttpActionResult> Echo(string userId, string message, string queryString)
        {
            await Task.Delay(150);
            return Ok(new {Action = "Echo", UserId = userId, Message = message, QueryString = queryString});
        }

        [HttpGet]
        [Route("echo2/{userId}/{message}")]
        public async Task<IHttpActionResult> Echo2(string userId, string message, string queryString)
        {
            await Task.Delay(150);
            return Ok(new {Action = "Echo2", UserId = userId, Message = message, QueryString = queryString});
        }

        [HttpGet]
        [Route("ignore/{userId}/{message}")]
        [IgnoreCache]
        public async Task<IHttpActionResult> EchoIgnore(string userId, string message, string queryString)
        {
            await Task.Delay(150);
            return Ok(new {Action = "EchoIgnore", UserId = userId, Message = message, QueryString = queryString});
        }

        [HttpGet]
        [Route("outputcache/{userId}/{message}")]
        [CacheOutput(ServerTimeSpan = 10, ClientTimeSpan = 30)]
        public async Task<IHttpActionResult> EchoOutputCache(string userId, string message, string queryString)
        {
            await Task.Delay(150);
            return Ok(new {Action = "EchoOutputCache", UserId = userId, Message = message, QueryString = queryString});
        }
    }
}