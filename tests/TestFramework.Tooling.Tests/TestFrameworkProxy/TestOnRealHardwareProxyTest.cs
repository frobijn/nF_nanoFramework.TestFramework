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

[assembly: TestFramework.Tooling.Tests.TestFrameworkProxy.TestOnRealHardwareProxyTest.TestOnPlatformMock("assembly", true)]

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{
    [TestClass]
    [TestOnPlatformMock("class", false)]
    public sealed class TestOnRealHardwareProxyTest
    {
        [TestMethod]
        [TestCategory("nF test attributes")]
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
            Assert.AreEqual(true, proxy.TestOnAllDevices);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
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
            Assert.AreEqual(false, proxy.TestOnAllDevices);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
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
            Assert.AreEqual(false, proxy.TestOnAllDevices);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TestOnPlatformMock", proxy.Source.Name);
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
        [TestCategory("Source code")]
        [TestOnPlatformMock("mcu", true)]
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
            Assert.AreEqual(true, proxy.TestOnAllDevices);
            Assert.IsNotNull(proxy.Source);
            Assert.AreEqual("TestOnPlatformMock", proxy.Source.Name);

            var testDevice = new TestDeviceProxy(
                new TestDeviceMock("-", "mcu")
            );
            Assert.IsTrue(proxy.ShouldTestOnDevice(testDevice));

            testDevice = new TestDeviceProxy(
                new TestDeviceMock("-", "other")
            );
            Assert.IsFalse(proxy.ShouldTestOnDevice(testDevice));
        }

        [TestMethod]
        [TestCategory("nF test attributes")]
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
            Assert.AreEqual(false, proxy.TestOnAllDevices);

            var testDevice = new TestDeviceProxy(
                new TestDeviceMock("-", "-", new Dictionary<string, string>
                {
                    {  "xyzzy", "Present!" }
                })
            );
            Assert.IsTrue(proxy.ShouldTestOnDevice(testDevice));

            testDevice = new TestDeviceProxy(
                new TestDeviceMock("-", "-", new Dictionary<string, string>
                {
                    {  "not_xyzzy", "Present!" }
                })
            );
            Assert.IsFalse(proxy.ShouldTestOnDevice(testDevice));
        }

        #region ITestOnRealHardware implementations
        /// <summary>
        /// Test implementation of <see cref="nfTest.ITestOnRealHardware"/>, for devices that match a platform
        /// </summary>
        [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
        internal sealed class TestOnPlatformMockAttribute : Attribute, nfTest.ITestOnRealHardware
        {
            #region Construction
            public TestOnPlatformMockAttribute(string platform, bool testOnAllDevices)
            {
                _platform = platform;
                _testOnAllDevices = testOnAllDevices;
            }
            private readonly string _platform;
            private readonly bool _testOnAllDevices;
            #endregion

            #region ITestOnRealHardware implementation
            /// <summary>
            /// Get a (short!) description of the devices that are suitable to execute the test on.
            /// This is added to the name of the test
            /// </summary>
            string nfTest.ITestOnRealHardware.Description
                => _platform;

            /// <summary>
            /// Indicates whether the test should be executed on the device
            /// </summary>
            /// <param name="testDevice">Device that is available to execute the test</param>
            /// <returns>Returns <c>true</c> if the test should be run, <c>false</c> otherwise.</returns>
            bool nfTest.ITestOnRealHardware.ShouldTestOnDevice(nfTest.ITestDevice testDevice)
                => testDevice.Platform() == _platform;

            /// <summary>
            /// Indicates whether the test should be executed on every available devices for which
            /// <see cref="ShouldTestOnDevice(ITestDevice)"/> of this attribute returns <c>true</c>. If the property
            /// is <c>false</c>, the test is executed only on the first of those devices.
            /// </summary>
            bool nfTest.ITestOnRealHardware.TestOnAllDevices
                => _testOnAllDevices;
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
            /// <summary>
            /// Get a (short!) description of the devices that are suitable to execute the test on.
            /// This is added to the name of the test
            /// </summary>
            string nfTest.ITestOnRealHardware.Description
                => _fileThatMustExist;

            /// <summary>
            /// Indicates whether the test should be executed on the device
            /// </summary>
            /// <param name="testDevice">Device that is available to execute the test</param>
            /// <returns>Returns <c>true</c> if the test should be run, <c>false</c> otherwise.</returns>
            bool nfTest.ITestOnRealHardware.ShouldTestOnDevice(nfTest.ITestDevice testDevice)
                => !(testDevice.GetStorageFileContent(_fileThatMustExist) is null);

            /// <summary>
            /// Indicates whether the test should be executed on every available devices for which
            /// <see cref="ShouldTestOnDevice(ITestDevice)"/> of this attribute returns <c>true</c>. If the property
            /// is <c>false</c>, the test is executed only on the first of those devices.
            /// </summary>
            bool nfTest.ITestOnRealHardware.TestOnAllDevices
                => false;
            #endregion
        }
        #endregion
    }
}
