// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using nanoFramework.Runtime.Native;
using nanoFramework.TestFramework;
using static nanoFramework.Runtime.Native.SystemInfo;

namespace NFUnitTest
{
    [TestClass]
    [TestCategory("Asserts")]
    public class SkipFewTest
    {
        [Setup]
        public void SetupMethodWillPass()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void MethodWillPass1()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void MethodWillPass2()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void MethodWillSkip()
        {
            Assert.SkipTest("This is a good reason: testing the test!");
        }

        [TestMethod]
        public void MethodWillSkip2()
        {
            Debug.WriteLine("For no reason");
            Assert.SkipTest();
        }

        [TestMethod]
        public void MethodWillPass3()
        {
            // Method intentionally left empty.
        }


        [TestMethod]
        public void MethodWillSkipIfFloatingPointSupportNotOK()
        {
            FloatingPoint sysInfoFloat = SystemInfo.FloatingPointSupport;
            if ((sysInfoFloat != FloatingPoint.DoublePrecisionHardware) && (sysInfoFloat != FloatingPoint.DoublePrecisionSoftware))
            {
                Assert.SkipTest("Double floating point not supported, skipping the Assert.Double test");
            }

            double on42 = 42.1;
            double maxDouble = double.MaxValue;
            Assert.AreEqual(42.1, on42);
            Assert.AreEqual(double.MaxValue, maxDouble);
        }

        [TestMethod]
        public void MethodWillSkipIfRunningInWin32()
        {
            string sysInfoPlatform = SystemInfo.Platform;
            if (sysInfoPlatform == "WIN32")
            {
                Assert.SkipTest("Skip method because this is running on WIN32 nanoCLR.");
            }
        }


        [TestMethod]
        public void MethodWillSkipIfRunningOnTargetOtherThanWin32()
        {
            string sysInfoPlatform = SystemInfo.Platform;
            if (sysInfoPlatform != "WIN32")
            {
                Assert.SkipTest("Skip method because this is running on a platform other than WIN32.");
            }
        }
    }
}
