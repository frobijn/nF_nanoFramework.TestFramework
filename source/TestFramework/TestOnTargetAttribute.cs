// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Mark a test as intended to be executed on real hardware that has the specified firmware installed.
    /// This is also visible in the Visual Studio test explorer via a trait. The test will be
    /// executed on at least one the available devices with the specified firmware. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TestOnTargetAttribute : Attribute, ITestOnRealHardware
    {
        #region Fields
        private readonly string _target;
        private readonly bool _runOnEveryDevice;
        #endregion

        #region Construction
        /// <summary>
        /// Inform the test runner that the test should be executed on real hardware that
        /// has the specified firmware target installed.
        /// </summary>
        /// <param name="target">Target the test should be executed on.</param>
        /// <param name="runOnEveryDevice">Indicates whether to run the test on every available device that has
        /// the <paramref name="target"/> firmware installed, rather than just one of the devices.</param>
        public TestOnTargetAttribute(string target, bool runOnEveryDevice)
        {
            _target = target;
            _runOnEveryDevice = runOnEveryDevice;
        }
        #endregion

        #region ITestOnRealHardware implementation
        string ITestOnRealHardware.Description
            => _target;

        bool ITestOnRealHardware.ShouldTestOnDevice(ITestDevice testDevice)
            => testDevice.TargetName() == _target;

        bool ITestOnRealHardware.TestOnAllDevices
            => _runOnEveryDevice;
        #endregion
    }
}
