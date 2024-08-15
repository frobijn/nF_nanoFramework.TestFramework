// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests.TestFrameworkProxy
{
    /// <summary>
    /// The other tests use attributes defined in this assembly, which is fine
    /// as they are just as foreign to the <see cref="AttributeProxy"/> implementations as
    /// the attributes in nanoFramework.TestFramework. This test loads an actual
    /// nanoFramework.TestFramework assembly to verify that the mechanism still
    /// work if the attributes are defined on the nanoFramework platform instead
    /// of a .NET platform. It focuses on the <see cref="TestOnRealHardwareProxy"/>
    /// attribute, as the other are already tested in tests that verify the creation
    /// of test cases.
    /// </summary>
    [TestClass]
    [TestCategory("nF test attributes")]
    public sealed class NFTestFramework_TestOnRealHardwareProxyTest
    {
        [TestMethod]
        [TestCategory("Test cases")]
        public void TestOnRealHardwareProxy_nF_TestFramework()
        {
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Execution.v3"));
            var logger = new LogMessengerMock();
            var testCases = new TestCaseCollection(assemblyFilePath, true, null, logger);

            #region TestOnTarget (assembly attribute)
            TestCase actual = (from tc in testCases.TestCases
                               where tc.ShouldRunOnRealHardware
                                     && tc.FullyQualifiedName == "TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware"
                               select tc).FirstOrDefault();
            if (actual is null)
            {
                Assert.Inconclusive();
            }

            var testDevice = new TestDeviceProxy(new TestDeviceMock("any", "esp32"));
            Assert.IsNotNull(actual.SelectDevicesForExecution(new TestDeviceProxy[] { testDevice }).FirstOrDefault());
            #endregion

            #region Custom ITestOnRealHardware implementation, ShouldTestOnDevice has some code for remote devices
            actual = (from tc in testCases.TestCases
                      where tc.ShouldRunOnRealHardware
                            && tc.FullyQualifiedName == "TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile"
                      select tc).FirstOrDefault();
            if (actual is null)
            {
                Assert.Inconclusive();
            }

            testDevice = new TestDeviceProxy(new TestDeviceMock("any", "any", new Dictionary<string, string>()
            {
                { "xyzzy", "Some data" }
            }));
            Assert.IsNotNull(actual.SelectDevicesForExecution(new TestDeviceProxy[] { testDevice }).FirstOrDefault());
            #endregion
        }
    }
}
