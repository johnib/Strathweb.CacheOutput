using System;
using System.Collections.Generic;
using System.Web.Http.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WebApi.OutputCache.V2.Demo.Core;

namespace WebApi.OutputCache.Demo.Tests
{
    [TestClass]
    public class SimpleCacheFilterTests
    {
        private const string ControllerName = "controller";
        private const string ActionName = "action";
        private const long CacheTimeMs = 1000;

        private Dictionary<string, object> _actionArguments = new Dictionary<string, object>();

        private Mock<IOutputCache<byte[]>> _cacheMock;
        private ActionFilterAttribute _filterUnderTest;

        #region Initialize

        [TestInitialize]
        public void Initialize()
        {
            _cacheMock = new Mock<IOutputCache<byte[]>>();
            _cacheMock.Setup(c => c.Get(It.IsAny<string>()));
            _cacheMock.Setup(c => c.Contains(It.IsAny<string>()));
            _cacheMock.Setup(c => c.Remove(It.IsAny<string>()));
            _cacheMock.Setup(c => c.RemoveDependentsOf(It.IsAny<string>()));
            _cacheMock.Setup(c => c.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()));

//            _filterUnderTest = new SimpleCacheFilter(_cacheMock.Object, TimeSpan.FromMilliseconds(CacheTimeMs));
        }

        #endregion

        #region OnActionExecuting Tests

        [TestMethod]
        public void OnHttpGetRequestThenCacheLookupOccurs()
        {
//            var controllerDescriptorMock = new Mock<HttpControllerDescriptor>();
//            controllerDescriptorMock.SetupGet(d => d.ControllerName).Returns(ControllerName);
//
//            var controllerContextMock = new Mock<HttpControllerContext>();
//            controllerContextMock.SetupGet(d => d.ControllerDescriptor).Returns(controllerDescriptorMock.Object);
//
//            var actionDescriptorMock = new Mock<HttpActionDescriptor>();
//            actionDescriptorMock.SetupGet(d => d.ActionName).Returns(ActionName);
//
//            var contextMock = new Mock<HttpActionContext>();
//            contextMock.SetupGet(c => c.ControllerContext).Returns(controllerContextMock.Object);
//            contextMock.SetupGet(c => c.ActionDescriptor).Returns(actionDescriptorMock.Object);
//            contextMock.SetupGet(c => c.ActionArguments).Returns(_actionArguments);
        }

        [TestMethod]
        public void OnHttpNotGetRequestThenActionIsExecuted()
        {
        }

        [TestMethod]
        public void WhenResponseExistsInCacheThenActionIsNotExecuted()
        {
        }

        [TestMethod]
        public void WhenResponseDoesNotExistInCacheThenActionIsExecuted()
        {
        }

        #endregion OnActionExecuting Tests

        #region OnActionExecuted Tests

        [TestMethod]
        public void OnHttpGetRequestThenResponseIsCached()
        {
        }

        [TestMethod]
        public void OnUnsuccessfulHttpGetRequestThenResponseIsNotCached()
        {
        }

        [TestMethod]
        public void OnNullOrEmptyResponseThenResponseIsNotCached()
        {
        }

        [TestMethod]
        public void TestCacheExpirationTimestampIsCorrect()
        {
        }

        #endregion OnActionExecuted Tests

        #region Cache Key Tests

        [TestMethod]
        public void TestCacheKeyContainsAllInputParamsSorted()
        {
        }

        [TestMethod]
        public void TestCacheKeyDoesNotContainCallbackParam()
        {
        }

        #endregion Cache Key Tests
    }
}