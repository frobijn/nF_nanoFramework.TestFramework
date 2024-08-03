// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.Runtime.Native;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest.TestFrameworkExtensions
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class TestOnDoublePrecisionDeviceAttribute : Attribute, ITestOnDevice, ITraits
    {
        public string[] Traits
            => new string[] { "On: double precision device" };

        public bool CanTestOnDevice(ITestDevice testDevice)
        {
            if (testDevice.IsRemoteDevice())
            {
                // Cannot determine whether the device satisfies the conditions
                return true;
            }
            else
            {
                // Attribute is executed on the same device as the test
                SystemInfo.FloatingPoint sysInfoFloat = SystemInfo.FloatingPointSupport;
                return sysInfoFloat == SystemInfo.FloatingPoint.DoublePrecisionHardware
                    || sysInfoFloat == SystemInfo.FloatingPoint.DoublePrecisionSoftware;
            }
        }
    }
}
