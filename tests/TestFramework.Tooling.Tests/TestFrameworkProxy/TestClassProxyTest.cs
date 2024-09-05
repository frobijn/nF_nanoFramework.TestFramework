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
    [TestClassProxyTest.TestClassMock] // This is not correct!
    public sealed class TestClassProxyTest_AssemblyAttributes : nfTest.IAssemblyAttributes
    {
    }

    [TestClass]
    [TestCategory("nF test attributes")]
    public sealed class TestClassProxyTest
    {
        [TestMethod]
        public void TestClassProxyCreatedForNonStaticClass()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetClassAttributeProxies(typeof(NonStaticTestClassMock), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(TestClassProxy), actual[0].GetType());

            var proxy = actual[0] as TestClassProxy;
            Assert.AreEqual(true, proxy.CreateInstancePerTestMethod);
            Assert.AreEqual(false, proxy.SetupCleanupPerTestMethod);
        }

        [TestMethod]
        public void TestClassProxyCreatedForNonStaticClassWithSource()
        {
            ProjectSourceInventory.ClassDeclaration source = TestProjectHelper.FindClassDeclaration(typeof(StaticTestClassMock));
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetClassAttributeProxies(typeof(NonStaticTestClassMock), new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(TestClassProxy), actual[0].GetType());

            var proxy = actual[0] as TestClassProxy;
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TestClassMock", proxy.Source.Name);
            Assert.AreEqual(true, proxy.CreateInstancePerTestMethod);
            Assert.AreEqual(false, proxy.SetupCleanupPerTestMethod);
        }



        [TestMethod]
        public void TestClassProxyCreatedForStaticClass()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetClassAttributeProxies(typeof(StaticTestClassMock), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(TestClassProxy), actual[0].GetType());

            var proxy = actual[0] as TestClassProxy;
            Assert.AreEqual(false, proxy.CreateInstancePerTestMethod);
            Assert.AreEqual(true, proxy.SetupCleanupPerTestMethod);
        }

        [TestMethod]
        [TestCategory("Source code")]
        public void TestClassProxyCreatedForStaticClassWithSource()
        {
            ProjectSourceInventory.ClassDeclaration source = TestProjectHelper.FindClassDeclaration(typeof(StaticTestClassMock));
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetClassAttributeProxies(typeof(StaticTestClassMock), new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.AreEqual(1, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
            Assert.AreEqual(typeof(TestClassProxy), actual[0].GetType());

            var proxy = actual[0] as TestClassProxy;
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TestClassMock", proxy.Source.Name);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual(false, proxy.CreateInstancePerTestMethod);
            Assert.AreEqual(true, proxy.SetupCleanupPerTestMethod);

        }

        [TestMethod]
        public void TestClassProxyNotCreatedForAbstractClass()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetClassAttributeProxies(typeof(AbstractTestClassMock), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.AreEqual(0, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
        }
        [TestClassMock]
        private abstract class AbstractTestClassMock
        {
        }


        [TestMethod]
        public void TestClassProxyNotCreatedForGenericTemplate()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetClassAttributeProxies(typeof(TestClassMock<>), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.AreEqual(0, actual?.Count);
            Assert.AreEqual(0, custom?.Count);
        }
        [TestClassMock]
        private class TestClassMock<SomeArgument>
        {
        }


        [TestMethod]
        public void TestClassProxyNotCreatedForAssembly()
        {
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetAssemblyAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.ITestClass))
                 select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.OfType<TestClassProxy>().Count());
            Assert.AreEqual(0, custom?.OfType<TestClassProxy>().Count());
        }

        [TestMethod]
        [TestClassMock] // This is not correct!
        public void TestClassProxyErrorForMethod()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            (List<AttributeProxy> actual, List<AttributeProxy> custom) = AttributeProxy.GetMethodAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual(@"Error: TestFramework.Tooling.Tests:TestFramework.Tooling.Tests.TestFrameworkProxy.TestClassProxyTest.TestClassProxyErrorForMethod: Error: Attribute implementing 'nanoFramework.TestFramework.ITestClass' cannot be applied to a test method. Attribute is ignored.");
            Assert.AreEqual(0, actual?.Count);
        }

        #region TestClassMockAttribute
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class TestClassMockAttribute : Attribute, nfTest.ITestClass
        {
            public TestClassMockAttribute(bool setupCleanupPerTestMethod = false, bool createInstancePerTestMethod = false)
            {
                CreateInstancePerTestMethod = createInstancePerTestMethod;
                SetupCleanupPerTestMethod = setupCleanupPerTestMethod;
            }

            public bool CreateInstancePerTestMethod
            {
                get;
            }

            public bool SetupCleanupPerTestMethod
            {
                get;
            }
        }
        #endregion
    }

    [TestClassProxyTest.TestClassMock(false, true)]
    internal class NonStaticTestClassMock
    {
    }

    /// <summary>
    /// Mock cannot be a nested class as that will not be found by the <see cref="ProjectSourceInventory"/>s
    /// </summary>
    [TestClassProxyTest.TestClassMock(true, false)]
    internal static class StaticTestClassMock
    {
    }
}
