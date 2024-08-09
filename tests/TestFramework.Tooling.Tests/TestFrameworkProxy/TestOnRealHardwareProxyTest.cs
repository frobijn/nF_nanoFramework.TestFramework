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

[assembly: TestFramework.Tooling.Tests.TestFrameworkProxy.TestOnRealHardwareProxyTest.TestOnPlatformMock("assembly")]

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{
    [TestClass]
    [TestCategory("nF test attributes")]
    [TestOnPlatformMock("class")]
    public sealed class TestOnRealHardwareProxyTest
    {
        [TestMethod]
        public void TestOnRealHardwareProxyCreatedForAssembly()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType().Assembly, new TestFrameworkImplementation(), logger);

            CollectionAssert.AreEqual(
                new object[] { },
                (from msg in logger.Messages
                 where msg.message.Contains(nameof(nfTest.ITestDevice))
                 select msg.level).ToList()
            );
            Assert.IsNotNull(actual);

            TestOnRealHardwareProxy proxy = actual.OfType<TestOnRealHardwareProxy>()
                              .FirstOrDefault();
            Assert.IsNotNull(proxy);
            Assert.AreEqual("assembly", proxy.Description);
        }

        [TestMethod]
        public void TestOnRealHardwareProxyCreatedForClass()
        {
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType(), new TestFrameworkImplementation(), null, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestOnRealHardwareProxy), actual[0].GetType());

            var proxy = actual[0] as TestOnRealHardwareProxy;
            Assert.AreEqual("class", proxy.Description);
        }

        [TestMethod]
        [TestCategory("Source code")]
        public void TestOnRealHardwareProxyCreatedForClassWithSource()
        {
            ProjectSourceInventory.ClassDeclaration source = TestProjectHelper.FindClassDeclaration(GetType());
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(GetType(), new TestFrameworkImplementation(), source.Attributes, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestOnRealHardwareProxy), actual[0].GetType());

            var proxy = actual[0] as TestOnRealHardwareProxy;
            Assert.AreEqual("class", proxy.Description);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TestOnPlatformMock", proxy.Source.Name);
        }

        [TestMethod]
        [TestCategory("Source code")]
        [TestOnPlatformMock("mcu")]
        public void TestOnRealHardwareProxyCreatedForMethodWithSource()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            ProjectSourceInventory.MethodDeclaration source = TestProjectHelper.FindMethodDeclaration(GetType(), thisMethod.Name);
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, new TestFrameworkImplementation(), source.Attributes, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestOnRealHardwareProxy), actual[0].GetType());

            var proxy = actual[0] as TestOnRealHardwareProxy;
            Assert.AreEqual("mcu", proxy.Description);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TestOnPlatformMock", proxy.Source.Name);

            var testDevice1 = new TestDeviceProxy(
                new TestDeviceMock("target1", "mcu")
            );
            Assert.IsTrue(proxy.ShouldTestOnDevice(testDevice1));
            Assert.IsTrue(proxy.AreDevicesEqual(testDevice1, testDevice1));

            var testDevice2 = new TestDeviceProxy(
                new TestDeviceMock("target2", "mcu")
            );
            Assert.IsTrue(proxy.ShouldTestOnDevice(testDevice2));
            Assert.IsFalse(proxy.AreDevicesEqual(testDevice1, testDevice2));

            var testDevice3 = new TestDeviceProxy(
                new TestDeviceMock("-", "other")
            );
            Assert.IsFalse(proxy.ShouldTestOnDevice(testDevice3));
        }

        [TestMethod]
        [TestForExistingStorageMock("xyzzy")]
        public void TestOnRealHardwareProxyUsingStorage()
        {
            var thisMethod = System.Reflection.MethodBase.GetCurrentMethod();
            var logger = new LogMessengerMock();
            List<AttributeProxy> actual = AttributeProxy.GetAttributeProxies(thisMethod, new TestFrameworkImplementation(), null, logger);

            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(typeof(TestOnRealHardwareProxy), actual[0].GetType());

            var proxy = actual[0] as TestOnRealHardwareProxy;
            Assert.AreEqual("xyzzy", proxy.Description);

            var testDevice1 = new TestDeviceProxy(
                new TestDeviceMock("-", "-", new Dictionary<string, string>
                {
                    {  "xyzzy", "Present!" }
                })
            );
            Assert.IsTrue(proxy.ShouldTestOnDevice(testDevice1));
            Assert.IsTrue(proxy.AreDevicesEqual(testDevice1, testDevice1));

            var testDevice2 = new TestDeviceProxy(
                new TestDeviceMock("-", "-", new Dictionary<string, string>
                {
                    {  "xyzzy", "Also present!" }
                })
            );
            Assert.IsTrue(proxy.ShouldTestOnDevice(testDevice1));
            Assert.IsFalse(proxy.AreDevicesEqual(testDevice1, testDevice2));

            var testDevice3 = new TestDeviceProxy(
                new TestDeviceMock("-", "-", new Dictionary<string, string>
                {
                    {  "not_xyzzy", "Not present!" }
                })
            );
            Assert.IsFalse(proxy.ShouldTestOnDevice(testDevice3));
        }

        #region ITestOnRealHardware implementations
        /// <summary>
        /// Test implementation of <see cref="nfTest.ITestOnRealHardware"/>, for devices that match a platform
        /// </summary>
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class TestOnPlatformMockAttribute : Attribute, nfTest.ITestOnRealHardware
        {
            #region Construction
            public TestOnPlatformMockAttribute(string platform)
            {
                _platform = platform;
            }
            private readonly string _platform;
            #endregion

            #region ITestOnRealHardware implementation
            string nfTest.ITestOnRealHardware.Description
                => _platform;

            bool nfTest.ITestOnRealHardware.ShouldTestOnDevice(nfTest.ITestDevice testDevice)
                => testDevice.Platform() == _platform;

            bool nfTest.ITestOnRealHardware.AreDevicesEqual(nfTest.ITestDevice testDevice1, nfTest.ITestDevice testDevice2)
                => testDevice1.TargetName() == testDevice2.TargetName();
            #endregion
        }

        /// <summary>
        /// Test implementation of <see cref="nfTest.ITestOnRealHardware"/>, for devices
        /// that have the file "xyzzy" in storage.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        private sealed class TestForExistingStorageMockAttribute : Attribute, nfTest.ITestOnRealHardware
        {
            #region Construction
            public TestForExistingStorageMockAttribute(string fileThatMustExist)
            {
                _fileThatMustExist = fileThatMustExist;
            }
            private readonly string _fileThatMustExist;
            #endregion

            #region ITestOnRealHardware implementation
            string nfTest.ITestOnRealHardware.Description
                => _fileThatMustExist;

            bool nfTest.ITestOnRealHardware.ShouldTestOnDevice(nfTest.ITestDevice testDevice)
                => !(testDevice.GetStorageFileContentAsBytes(_fileThatMustExist) is null);

            bool nfTest.ITestOnRealHardware.AreDevicesEqual(nfTest.ITestDevice testDevice1, nfTest.ITestDevice testDevice2)
                => testDevice1.GetStorageFileContentAsString(_fileThatMustExist) == testDevice2.GetStorageFileContentAsString(_fileThatMustExist);
            #endregion
        }
        #endregion
    }
}
