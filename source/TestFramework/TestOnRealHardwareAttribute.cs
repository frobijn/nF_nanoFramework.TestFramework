// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Mark a test, all methods of a test class or all tests in a assembly (when applied to a class implementing the
    /// <see cref="IAssemblyAttributes"/> interface) as intended to be executed on real hardware. This is also visible in the
    /// Visual Studio test explorer via a trait. The test will be executed on one
    /// of the available real hardware devices.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
#if NFTF_REFERENCED_SOURCE_FILE
    internal
#else
    public
#endif
        sealed class TestOnRealHardwareAttribute : Attribute, ITestOnRealHardware
    {
        #region Fields
        private readonly bool _differentTargetIsDifferentDevice;
        #endregion

        #region Construction
        /// <summary>
        /// Inform the test runner that the test should be run on real hardware. It is sufficient to run the test
        /// on a single device in case multiple real hardware devices are available.
        /// </summary>
        /// <param name="differentTargetIsDifferentDevice">Indicates whether two devices with different target/firmware
        /// are considered different devices. If <c>true</c> and if two devices with different firmware are simultaneously
        /// available to run unit tests on, the test will be executed on both devices.</param>
        public TestOnRealHardwareAttribute(bool differentTargetIsDifferentDevice = false)
        {
            _differentTargetIsDifferentDevice = differentTargetIsDifferentDevice;
        }
        #endregion

        #region ITestOnRealHardware implementation
        string ITestOnRealHardware.Description
            => "Real hardware";

        bool ITestOnRealHardware.ShouldTestOnDevice(ITestDevice testDevice)
            => true;

        bool ITestOnRealHardware.AreDevicesEqual(ITestDevice testDevice1, ITestDevice testDevice2)
            => !_differentTargetIsDifferentDevice
                || testDevice1.TargetName().ToUpper() == testDevice2.TargetName().ToUpper();
        #endregion
    }
}
