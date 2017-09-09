using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WebApi.OutputCache.V2.Demo.CacheProviders;

namespace WebApi.OutputCache.Demo.Tests
{
    [TestClass]
    public class DefaultCacheKeyGeneratorUnitTests
    {
        #region Constants

        private const string ControllerName = "controller";
        private const string ActionName = "action";

        private readonly string _workspaceId = Guid.NewGuid().ToString();
        private readonly string _workflowId = Guid.NewGuid().ToString();
        private readonly string _customizationTimestmap = DateTimeOffset.UtcNow.ToString("o");
        private const int Id = 1;

        #endregion

        #region Mocks

        private HttpActionContext _actionContext;
        private Mock<ReflectedHttpActionDescriptor> _actionDescriptorMock;
        private Mock<IDependencyResolver> _diResolver;

        #endregion

        protected ICacheKeyGenerator GeneratorUnderTest;

        #region Initialize

        [TestInitialize]
        public void Initialize()
        {
            _actionDescriptorMock = new Mock<ReflectedHttpActionDescriptor>();
            _actionDescriptorMock.SetupGet(a => a.ActionName).Returns(ActionName);

            _diResolver = new Mock<IDependencyResolver>();
            _actionContext = GenerateActionContext();

            GeneratorUnderTest = new DefaultCacheKeyGenerator();
        }

        #endregion

        #region Tests

        [TestMethod]
        public void TestCacheKeyIsGeneratedCorrectly()
        {
            Regex regex = new Regex("^(?<controller>[a-zA-Z0-9]+)-(?<action>[a-zA-Z0-9]+)-(?<params>.*)$");

            string cacheKey = GeneratorUnderTest.GetCacheKey(_actionContext);

            System.Text.RegularExpressions.Match match = regex.Match(cacheKey);

            Assert.IsTrue(match.Success);
            Assert.AreEqual(ControllerName, match.Groups["controller"].Value);
            Assert.AreEqual(ActionName, match.Groups["action"].Value);

            var inputParams = match.Groups["params"].Value;
            var expectedInputParams = string.Join(";", _actionContext.ActionArguments
                .Where(kv => kv.Key != "callback")
                .OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}={kv.Value}"));

            Assert.AreEqual(expectedInputParams, inputParams);
        }

        #endregion

        #region Private

        protected virtual HttpActionContext GenerateActionContext(HttpMethod method = null)
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

        private static HttpRequestMessage GenerateRequestMessage(HttpMethod method = null)
        {
            return new HttpRequestMessage
            {
                Method = method ?? HttpMethod.Get,
            };
        }

        #endregion
    }
}