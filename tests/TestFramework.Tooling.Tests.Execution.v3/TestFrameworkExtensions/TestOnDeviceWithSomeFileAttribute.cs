// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest.TestFrameworkExtensions
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class TestOnDeviceWithSomeFileAttribute : Attribute, ITestOnRealHardware
    {
        public string Description
            => "DeviceWithSomeFile";

        public bool ShouldTestOnDevice(ITestDevice testDevice)
        {
            string fileName = "xyzzy";
            byte[] content = testDevice.GetStorageFileContent(fileName);
            return !string.IsNullOrEmpty(fileName) && content != null;
        }

        public bool TestOnAllDevices
            => true;
    }
}
