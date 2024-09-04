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

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{
    [TestMethodProxyTest.TestMethodMock(true)] // This is not correct!
    public sealed class TestMethodProxyTest_AssemblyAttributes : nfTest.IAssemblyAttributes
    {
    }

    [TestClass]
    [TestCategory("nF test attributes")]
    [TestMethodMock(false)] // This is not correct!
    public sealed class TestMethodProxyTest
    {
        [TestMethod]
        [TestMethodMock(true)]
        public void TestMethodProxy_CreatedForMethod()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(TestMethodProxy), actual[0].GetType());

            var proxy = actual[0] as TestMethodProxy;
            Assert.AreEqual(true, proxy.CanBeRun);
        }

        [TestMethod]
        [TestCategory("Source code")]
        [TestMethodMock(true)]
        public void TestMethodProxy_CreatedForMethodWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectHelper.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(TestMethodProxy), actual[0].GetType());

            var proxy = actual[0] as TestMethodProxy;
            Assert.AreEqual(true, proxy.CanBeRun);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TestMethodMock", proxy.Source.Name);
        }

        [TestMethod]
        public void TestMethodProxy_ErrorForAssembly()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetAssemblyAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.ITestMethod))
                 select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.OfType<TestMethodProxy>().Count());
            Assert.AreEqual(0, custom?.Count);
        }

        [TestMethod]
        public void TestMethodProxy_ErrorForClass()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetClassAttributeProxies(GetType(), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual(@"Error: TestFramework.Tooling.Tests:TestFramework.Tooling.Tests.TestFrameworkProxy.TestMethodProxyTest: Error: Attribute implementing 'nanoFramework.TestFramework.ITestMethod' cannot be applied to a test class. Attribute is ignored.");
            Assert.AreEqual(0, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
        }

        #region TestMethodMockAttribute
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
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
