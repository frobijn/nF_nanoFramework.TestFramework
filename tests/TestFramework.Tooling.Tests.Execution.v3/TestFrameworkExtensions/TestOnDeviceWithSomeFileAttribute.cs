﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Execution.Tests.TestFrameworkExtensions
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class TestOnDeviceWithSomeFileAttribute : Attribute, ITestOnRealHardware
    {
        private readonly string _fileName;

        public TestOnDeviceWithSomeFileAttribute(string fileName)
        {
            _fileName = fileName;
        }

        public string Description
            => "DeviceWithSomeFile";

        public bool ShouldTestOnDevice(ITestDevice testDevice)
        {
            string content = testDevice.GetStorageFileContentAsString(_fileName);
            return !string.IsNullOrEmpty(content);
        }

        public bool AreDevicesEqual(ITestDevice testDevice1, ITestDevice testDevice2)
        {
            string content1 = testDevice1.GetStorageFileContentAsString(_fileName);
            string content2 = testDevice2.GetStorageFileContentAsString(_fileName);
            return content1 == content2;
        }
    }
}
