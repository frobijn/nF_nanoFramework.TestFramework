// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Mark a test as intended to be executed on real hardware. This is also visible in the
    /// Visual Studio test explorer via a trait. The test will be executed on at least one
    /// of the available devices that are not the Virtual Device.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TestOnRealHardwareAttribute : Attribute, ITestOnRealHardware, ITraits
    {
        #region Construction
        /// <summary>
        /// Inform the test runner that the test should be run on the virtual nanoDevice.
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

        bool ITestOnRealHardware.TestOnAllDevices
            => false;
        #endregion

        #region ITrait implementation
        string[] ITraits.Traits
            => new string[] { "For: Real hardware" };
        #endregion
    }
}
