// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest.TestFrameworkExtensions
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class TestOnDeviceWithProgrammingErrorAttribute : Attribute, ITestOnRealHardware
    {
        private readonly bool _exceptionInAreDevicesEqual;

        public TestOnDeviceWithProgrammingErrorAttribute(bool exceptionInAreDevicesEqual)
        {
            _exceptionInAreDevicesEqual = exceptionInAreDevicesEqual;
        }

        public string Description
            => "TestOnDeviceWithProgrammingError";

        public bool ShouldTestOnDevice(ITestDevice testDevice)
        {
            if (!_exceptionInAreDevicesEqual)
            {
                throw new Exception("Simulation of programming error");
            }
            return true;
        }

        public bool AreDevicesEqual(ITestDevice testDevice1, ITestDevice testDevice2)
        {
            if (_exceptionInAreDevicesEqual)
            {
                throw new Exception("Simulation of programming error");
            }
            return true;
        }
    }
}
