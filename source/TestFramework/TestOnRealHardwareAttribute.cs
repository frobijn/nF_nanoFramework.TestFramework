// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Mark a test, all methods of a test class or all tests in a assembly (when applied to a class implementing the
    /// <see cref="IAssemblyAttributes"/> interface) as intended to be executed on real hardware. This is also visible in the
    /// Visual Studio test explorer via a trait. The test will be executed on at least one
    /// of the available devices that are not the Virtual Device.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
#if NFTF_REFERENCED_SOURCE_FILE
    internal
#else
    public
#endif
        sealed class TestOnRealHardwareAttribute : Attribute, ITestOnRealHardware
    {
        #region Construction
        /// <summary>
        /// Inform the test runner that the test should be run on real hardware. If the available devices
        /// have a different CLR firmware (target) installed, the test is executed for each target on
        /// one of the devices with that firmware.
        /// </summary>
        public TestOnRealHardwareAttribute()
        {
        }
        #endregion

        #region ITestOnRealHardware implementation
        string ITestOnRealHardware.Description
            => "Real hardware";

        bool ITestOnRealHardware.ShouldTestOnDevice(ITestDevice testDevice)
            => true;

        bool ITestOnRealHardware.AreDevicesEqual(ITestDevice testDevice1, ITestDevice testDevice2)
            => testDevice1.TargetName() == testDevice2.TargetName();
        #endregion
    }
}
