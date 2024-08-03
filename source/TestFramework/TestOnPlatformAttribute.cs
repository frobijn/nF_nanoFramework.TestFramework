// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Mark a test, all methods of a test class or all tests in a assembly as intended to be executed
    /// on real hardware based on the specified platform. For each platform a separate test case is created, and the
    /// test case is selectable via its trait. The test will be executed on at least one the available devices
    /// that are based on the specified platform.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TestOnPlatformAttribute : Attribute, ITestOnRealHardware, ITraits
    {
        #region Fields
        private readonly string _platform;
        private readonly bool _runOnEveryDevice;
        #endregion

        #region Construction
        /// <summary>
        /// Inform the test runner that the test should be executed on real hardware based on the specified platform.
        /// </summary>
        /// <param name="platform">Platform the test should be executed on.</param>
        /// <param name="runOnEveryDevice">Indicates whether to run the test on every available device based on the <paramref name="platform"/>,
        /// rather than just one of the devices.</param>
        public TestOnPlatformAttribute(string platform, bool runOnEveryDevice)
        {
            _platform = platform;
            _runOnEveryDevice = runOnEveryDevice;
        }
        #endregion

        #region ITestOnRealHardware implementation
        string ITestOnRealHardware.Description
            => _platform;

        bool ITestOnRealHardware.ShouldTestOnDevice(ITestDevice testDevice)
            => testDevice.Platform() == _platform;

        bool ITestOnRealHardware.TestOnAllDevices
            => _runOnEveryDevice;
        #endregion

        #region ITrait implementation
        string[] ITraits.Traits
            => new string[] { $"For: platform {_platform}" };
        #endregion
    }
}
