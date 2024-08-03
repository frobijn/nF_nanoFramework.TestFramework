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

[assembly: TestFramework.Tooling.Tests.TestFrameworkProxy.TestOnVirtualDeviceProxyTest.TestOnVirtualDeviceMock]

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{
    [TestClass]
    [TestOnVirtualDeviceMock]
    public sealed class TestOnVirtualDeviceProxyTest
    {
        [TestMethod]
        [TestCategory("nF test attributes")]
        public void TestOnVirtualDeviceProxyCreatedForAssembly()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType().Assembly, logger);

            CollectionAssert.AreEqual(
                new object[] { },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.ITestDevice))
                 select msg.level).ToList()
            );
            Assert.IsNotNull(actual);

            TestOnVirtualDeviceProxy proxy = actual.OfType<TestOnVirtualDeviceProxy>()
                                                   .FirstOrDefault();
            Assert.IsNotNull(proxy);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        public void TestOnVirtualDeviceProxyCreatedForClass()
        {
            LogMessengerMock logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType(), null, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestOnVirtualDeviceProxy), actual[0].GetType());
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestCategory("Source code")]
        public void TestOnVirtualDeviceProxyCreatedForClassWithSource()
        {
            ProjectSourceInventory.ClassDeclaration source = TestProjectSourceAnalyzer.FindClassDeclaration(GetType());
            LogMessengerMock logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType(), source.Attributes, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestOnVirtualDeviceProxy), actual[0].GetType());

            Assert.IsNotNull(actual[0].Source);
            Assert.AreEqual("TestOnVirtualDeviceMock", actual[0].Source.Name);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestOnVirtualDeviceMock]
        public void TestOnVirtualDeviceCreatedForMethod()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, null, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestOnVirtualDeviceProxy), actual[0].GetType());
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestCategory("Source code")]
        [TestOnVirtualDeviceMock]
        public void TestOnVirtualDeviceProxyCreatedForMethodWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectSourceAnalyzer.FindMethodDeclaration(GetType(), thisMethod.Name);
            LogMessengerMock logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, source.Attributes, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestOnVirtualDeviceProxy), actual[0].GetType());

            Assert.IsNotNull(actual[0].Source);
            Assert.AreEqual("TestOnVirtualDeviceMock", actual[0].Source.Name);
        }

        #region ITestOnVirtualDevice implementations
        /// <summary>
        /// Test implementation of <see cref="nfTest.ITestOnVirtualDevice"/>, for devices that match a platform
        /// </summary>
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class TestOnVirtualDeviceMockAttribute : Attribute, nfTest.ITestOnVirtualDevice
        {
            #region Construction
            public TestOnVirtualDeviceMockAttribute()
            {
            }
            #endregion
        }
        #endregion
    }
}
