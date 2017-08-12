using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WebApi.OutputCache.V2.Demo.Attributes;
using WebApi.OutputCache.V2.Demo.CacheProviders;
using Match = System.Text.RegularExpressions.Match;

namespace WebApi.OutputCache.Demo.Tests
{
    [TestClass]
    public class SimpleCacheFilterTests
    {
        #region Constants

        private const string ControllerName = "controller";
        private const string ActionName = "action";
        private const int CacheTimeMs = 1000;
        private const string DefaultCacheValue = "value";
        private readonly byte[] _defaultValueBytes = Encoding.UTF8.GetBytes(DefaultCacheValue);

        private readonly string _workspaceId = Guid.NewGuid().ToString();
        private readonly string _workflowId = Guid.NewGuid().ToString();
        private readonly string _customizationTimestmap = DateTimeOffset.UtcNow.ToString("o");
        private const int Id = 1;

        #endregion

        #region Mocks

        private Mock<IOutputCacheProvider<byte[]>> _cacheMock;
        private Mock<IDependencyResolver> _diResolver;
        private Mock<ReflectedHttpActionDescriptor> _actionDescriptorMock;

        #endregion

        private ActionFilterAttribute _filterUnderTest;

        #region Initialize

        [TestInitialize]
        public void Initialize()
        {
            _cacheMock = new Mock<IOutputCacheProvider<byte[]>>();
            _cacheMock.Setup(c => c.Get(It.IsAny<string>()));
            _cacheMock.Setup(c => c.Contains(It.IsAny<string>()));
            _cacheMock.Setup(c => c.Remove(It.IsAny<string>()));
            _cacheMock.Setup(c => c.RemoveDependentsOf(It.IsAny<string>()));
            _cacheMock.Setup(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()));

            _diResolver = new Mock<IDependencyResolver>();
            _diResolver.Setup(d => d.GetService(typeof(IOutputCacheProvider<byte[]>))).Returns(_cacheMock.Object);

            // Relevant for the IgnoreCache attribute check
            _actionDescriptorMock = new Mock<ReflectedHttpActionDescriptor>();
            _actionDescriptorMock
                .Setup(r => r.GetCustomAttributes<IgnoreCache>())
                .Returns(new Collection<IgnoreCache>());

            _actionDescriptorMock.SetupGet(a => a.ActionName).Returns(ActionName);

            _filterUnderTest = new SimpleOutputCache {Milliseconds = CacheTimeMs};
        }

        #endregion

        #region OnActionExecuting Tests

        [TestMethod]
        public void OnHttpGetRequestThenCacheLookupOccurs()
        {
            _filterUnderTest.OnActionExecuting(GenerateActionContext());
            _cacheMock.Verify(c => c.Get(It.IsAny<string>()), Times.Once());
        }

        [TestMethod]
        public void OnHttpNotGetRequestThenActionIsExecuted()
        {
            var context = GenerateActionContext();
            context.Request.Method = HttpMethod.Post;

            _filterUnderTest.OnActionExecuting(context);

            _cacheMock.Verify(c => c.Get(It.IsAny<string>()), Times.Never());
            Assert.IsNull(context.Response);
        }

        [TestMethod]
        public async Task WhenResponseExistsInCacheThenActionIsNotExecuted()
        {
            _cacheMock.Setup(c => c.Get(It.IsAny<string>())).Returns(_defaultValueBytes);

            var context = GenerateActionContext();
            _filterUnderTest.OnActionExecuting(context);

            Assert.IsNotNull(context.Response);

            var content = await context.Response.Content.ReadAsStringAsync();
            Assert.AreEqual(DefaultCacheValue, content);
            Assert.AreEqual(
                MediaTypeHeaderValue.Parse("application/json; charset=utf-8"), 
                context.Response.Content.Headers.ContentType);
        }

        [TestMethod]
        public void WhenResponseDoesNotExistInCacheThenActionIsExecuted()
        {
            _cacheMock.Setup(c => c.Get(It.IsAny<string>())).Returns((byte[]) null);

            var context = GenerateActionContext();
            _filterUnderTest.OnActionExecuting(context);

            Assert.IsNull(context.Response);
        }

        [TestMethod]
        public void WhenActionHasIgnoreCacheAttributeThenCacheLookupIsSkipped()
        {
            _actionDescriptorMock
                .Setup(r => r.GetCustomAttributes<IgnoreCache>())
                .Returns(new Collection<IgnoreCache> {new IgnoreCache()});

            _filterUnderTest.OnActionExecuting(GenerateActionContext());
            _cacheMock.Verify(c => c.Get(It.IsAny<string>()), Times.Never());
        }

        #endregion OnActionExecuting Tests

        #region OnActionExecuted Tests

        [TestMethod]
        public async Task OnHttpGetRequestThenResponseIsCached()
        {
            var executedContext = GenerateActionExecutedContext();

            await _filterUnderTest.OnActionExecutedAsync(executedContext, CancellationToken.None);

            _cacheMock.Verify(c => c.Set(It.IsAny<string>(), _defaultValueBytes, It.IsAny<DateTimeOffset>(), null), Times.Once());
        }

        [TestMethod]
        public async Task OnHttpNotGetRequestThenResponseIsNotCached()
        {
            var executedContext = GenerateActionExecutedContext(HttpMethod.Post);

            await _filterUnderTest.OnActionExecutedAsync(executedContext, CancellationToken.None);

            _cacheMock.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public async Task OnUnsuccessfulHttpGetRequestThenResponseIsNotCached()
        {
            var executedContext = GenerateActionExecutedContext(HttpMethod.Get, HttpStatusCode.BadRequest);

            await _filterUnderTest.OnActionExecutedAsync(executedContext, CancellationToken.None);

            _cacheMock.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()), Times.Never());

        }

        [TestMethod]
        public async Task OnNullOrEmptyResponseThenResponseIsNotCached()
        {
            var executedContext = GenerateActionExecutedContext(HttpMethod.Get, HttpStatusCode.OK, new byte[0]);

            await _filterUnderTest.OnActionExecutedAsync(executedContext, CancellationToken.None);

            _cacheMock.Verify(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void TestCacheExpirationTimestampIsCorrect()
        {
        }

        [TestMethod]
        public async Task WhenActionHasIgnoreCacheAttributeThenValueIsNotCached()
        {
            _actionDescriptorMock
                .Setup(r => r.GetCustomAttributes<IgnoreCache>())
                .Returns(new Collection<IgnoreCache> { new IgnoreCache() });

            var executedContext = GenerateActionExecutedContext(HttpMethod.Get, HttpStatusCode.OK, new byte[0]);

            await _filterUnderTest.OnActionExecutedAsync(executedContext, CancellationToken.None);

            _cacheMock.Verify(c => c.Get(It.IsAny<string>()), Times.Never());
        }

        #endregion OnActionExecuted Tests

        #region Cache Key Tests

        [TestMethod]
        public async Task TestCacheKeyIsGeneratedCorrectly()
        {
            Regex regex = new Regex("^(?<controller>[a-zA-Z0-9]+)-(?<action>[a-zA-Z0-9]+)-(?<params>.*)$");

            string cacheKey = null;
            _cacheMock
                .Setup(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()))
                .Callback<string, byte[], DateTimeOffset, string>((s, bytes, arg3, arg4) =>
                {
                    cacheKey = s;
                });

            var executedContext = GenerateActionExecutedContext();

            await _filterUnderTest.OnActionExecutedAsync(executedContext, CancellationToken.None);

            Match match = regex.Match(cacheKey);

            Assert.IsTrue(match.Success);
            Assert.AreEqual(ControllerName, match.Groups["controller"].Value);
            Assert.AreEqual(ActionName, match.Groups["action"].Value);

            var inputParams = match.Groups["params"].Value;
            var expectedInputParams = string.Join(";", executedContext.ActionContext.ActionArguments
                .Where(kv => kv.Key != "callback")
                .OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}={kv.Value}"));

            Assert.AreEqual(expectedInputParams, inputParams);
        }

        #endregion Cache Key Tests

        #region Private

        private HttpActionContext GenerateActionContext(HttpMethod method = null)
        {
            var httpConfig = new HttpConfiguration
            {
                Formatters = {new JsonMediaTypeFormatter()},
                DependencyResolver = _diResolver.Object,
            };

            var request = GenerateRequestMessage(method);

            return new HttpActionContext
            {
                ActionDescriptor = _actionDescriptorMock.Object,
                ActionArguments =
                {
                    {"workspaceId", _workspaceId},
                    {"workflowId", _workflowId},
                    {"CustomizationTimestamp", _customizationTimestmap},
                    {"id", Id},
                    {"callback", "someValue"},
                },
                ControllerContext = new HttpControllerContext
                {
                    Request = request,
                    Configuration = httpConfig,
                    ControllerDescriptor = new HttpControllerDescriptor
                    {
                        ControllerName = ControllerName,
                        Configuration = httpConfig,
                    },
                },
            };
        }

        private HttpActionExecutedContext GenerateActionExecutedContext(HttpMethod method = null, HttpStatusCode statusCode = HttpStatusCode.OK, byte[] content = null)
        {
            var context = GenerateActionContext(method);
            var response = GenerateResponseMessage(context.Request, statusCode, content);

            return new HttpActionExecutedContext
            {
                ActionContext = context,
                Response = response,
            };
        }

        private static HttpRequestMessage GenerateRequestMessage(HttpMethod method = null)
        {
            return new HttpRequestMessage
            {
                Method = method ?? HttpMethod.Get,
            };
        }

        private HttpResponseMessage GenerateResponseMessage(HttpRequestMessage request, HttpStatusCode statusCode = HttpStatusCode.OK, byte[] content = null)
        {
            return new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new ByteArrayContent(content ?? _defaultValueBytes),
                RequestMessage = request,
            };
        }

        #endregion
    }
}