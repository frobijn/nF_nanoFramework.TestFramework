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

[assembly: TestFramework.Tooling.Tests.TestFrameworkProxy.TestClassProxyTest.TestClassMock] // This is not correct!

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{

    [TestClass]
    [TestCategory("nF test attributes")]
    public sealed class TestClassProxyTest
    {
        [TestMethod]
        public void TestClassProxyCreatedForNonStaticClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(typeof(NonStaticTestClassMock), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestClassProxy), actual[0].GetType());
        }

        [TestMethod]
        public void TestClassProxyCreatedForNonStaticClassWithSource()
        {
            ProjectSourceInventory.ClassDeclaration source = TestProjectHelper.FindClassDeclaration(typeof(StaticTestClassMock));
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(typeof(NonStaticTestClassMock), new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestClassProxy), actual[0].GetType());

            var proxy = actual[0] as TestClassProxy;
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TestClassMock", proxy.Source.Name);
        }



        [TestMethod]
        [TestCategory("Source code")]
        public void TestClassProxyCreatedForStaticClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(typeof(StaticTestClassMock), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestClassProxy), actual[0].GetType());
        }

        [TestMethod]
        [TestCategory("Source code")]
        public void TestClassProxyCreatedForStaticClassWithSource()
        {
            ProjectSourceInventory.ClassDeclaration source = TestProjectHelper.FindClassDeclaration(typeof(StaticTestClassMock));
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(typeof(StaticTestClassMock), new TestFrameworkImplementation(), source.Attributes, logger);

            logger.AssertEqual("");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestClassProxy), actual[0].GetType());

            var proxy = actual[0] as TestClassProxy;
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TestClassMock", proxy.Source.Name);
        }



        [TestMethod]
        public void TestClassProxyNotCreatedForAbstractClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(typeof(AbstractTestClassMock), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.AreEqual(0, actual?.Count ?? -1);
        }
        [TestClassMock]
        private abstract class AbstractTestClassMock
        {
        }


        [TestMethod]
        public void TestClassProxyNotCreatedForGenericTemplate()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(typeof(TestClassMock<>), new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual("");
            Assert.AreEqual(0, actual?.Count ?? -1);
        }
        [TestClassMock]
        private class TestClassMock<SomeArgument>
        {
        }


        [TestMethod]
        public void TestClassProxyErrorForAssembly()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { LoggingLevel.Error },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.ITestClass))
                 select msg.level).ToList()
            );
            Assert.AreEqual(0, actual?.OfType<TestClassProxy>().Count() ?? -1);
        }

        [TestMethod]
        [TestClassMock] // This is not correct!
        public void TestClassProxyErrorForMethod()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            logger.AssertEqual(@"Error: TestFramework.Tooling.Tests:TestFramework.Tooling.Tests.TestFrameworkProxy.TestClassProxyTest.TestClassProxyErrorForMethod: Attribute implementing 'ITestClass' can only be applied to a class. Attribute is ignored.");
            Assert.AreEqual(0, actual?.Count ?? -1);
        }

        #region TestClassMockAttribute
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class TestClassMockAttribute : Attribute, nfTest.ITestClass
        {
            public TestClassMockAttribute()
            {
            }
        }
        #endregion
    }

    [TestClassProxyTest.TestClassMock]
    internal class NonStaticTestClassMock
    {
    }

    /// <summary>
    /// Mock cannot be a nested class as that will not be found by the <see cref="ProjectSourceInventory"/>s
    /// </summary>
    [TestClassProxyTest.TestClassMock]
    internal static class StaticTestClassMock
    {
    }
}
