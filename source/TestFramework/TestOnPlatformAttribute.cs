// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Mark a test, all methods of a test class or all tests in a assembly (when applied to a class implementing the
    /// <see cref="IAssemblyAttributes"/> interface) as intended to be executed
    /// on real hardware based on the specified platform. For each platform a separate test case is created, and the
    /// test case is selectable via its trait. The test will be executed on the available devices
    /// that are based on the specified platform. If the available devices have a different CLR firmware (target)
    /// installed, the test is executed for each target on one of the devices with that firmware.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TestOnPlatformAttribute : Attribute, ITestOnRealHardware
    {
        #region Fields
        private readonly string _platform;
        private readonly bool _differentTargetIsDifferentDevice;
        #endregion

        #region Construction
        /// <summary>
        /// Inform the test runner that the test should be executed on real hardware based on the specified platform.
        /// </summary>
        /// <param name="platform">Platform the test should be executed on.</param>
        /// <param name="differentTargetIsDifferentDevice">Indicates whether two devices with different target/firmware
        /// are considered different devices. If <c>true</c> and if two devices with different firmware are simultaneously
        /// available to run unit tests on, the test will be executed on both devices.</param>
        public TestOnPlatformAttribute(string platform, bool differentTargetIsDifferentDevice = false)
        {
            _platform = platform?.ToUpper();
            _differentTargetIsDifferentDevice = differentTargetIsDifferentDevice;
        }
        #endregion

        #region ITestOnRealHardware implementation
        string ITestOnRealHardware.Description
            => _platform;

        bool ITestOnRealHardware.ShouldTestOnDevice(ITestDevice testDevice)
            => testDevice.Platform().ToUpper() == _platform;

        bool ITestOnRealHardware.AreDevicesEqual(ITestDevice testDevice1, ITestDevice testDevice2)
            => !_differentTargetIsDifferentDevice
                || testDevice1.TargetName().ToUpper() == testDevice2.TargetName().ToUpper();
        #endregion
    }
}
