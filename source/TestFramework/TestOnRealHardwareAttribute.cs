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
        #region Construction
        /// <summary>
        /// Inform the test runner that the test should be run on real hardware. It is sufficient to run the test
        /// on a single device in case multiple real hardware devices are available.
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
            => true;
        #endregion
    }
}
