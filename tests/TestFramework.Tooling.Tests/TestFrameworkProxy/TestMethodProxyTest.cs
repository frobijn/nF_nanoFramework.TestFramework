// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;
using TestFramework.Tooling.Tests.Helpers;
using nfTest = nanoFramework.TestFramework;

[assembly: TestFramework.Tooling.Tests.TestFrameworkProxy.TestMethodProxyTest.TestMethodMock(true)] // This is not correct!

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{

    [TestClass]
    [TestMethodMock(false)] // This is not correct!
    public sealed class TestMethodProxyTest
    {
        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestMethodMock(true)]
        public void TestMethodProxyCreatedForMethod()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, null, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestMethodProxy), actual[0].GetType());

            var proxy = actual[0] as TestMethodProxy;
            Assert.AreEqual(true, proxy.CanBeRun);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestCategory("Source code")]
        [TestMethodMock(true)]
        public void TestMethodProxyCreatedForMethodWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectSourceAnalyzer.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, source.Attributes, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestMethodProxy), actual[0].GetType());

            var proxy = actual[0] as TestMethodProxy;
            Assert.AreEqual(true, proxy.CanBeRun);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TestMethodMock", proxy.Source.Name);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        public void TestMethodProxyErrorForAssembly()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType().Assembly, logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.ITestMethod))
                 select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.OfType<TestMethodProxy>().Count() ?? -1);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        public void TestMethodProxyErrorForClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType(), null, logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.Count ?? -1);
        }

        #region TestMethodMockAttribute
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class TestMethodMockAttribute : Attribute, nfTest.ITestMethod
        {
            public TestMethodMockAttribute(bool canBeRun)
            {
                CanBeRun = canBeRun;
            }

            public bool CanBeRun
            {
                get;
            }
        }
        #endregion
    }
}
