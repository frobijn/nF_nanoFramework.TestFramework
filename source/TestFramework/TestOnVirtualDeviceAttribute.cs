// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Mark a test method, all test methods of a test class or all tests in an assembly as intended to be executed on
    /// the Virtual Device.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TestOnVirtualDeviceAttribute : Attribute, ITestOnVirtualDevice
    {
        #region Construction
        /// <summary>
        /// Inform the test runner that the test should be run on the virtual nanoDevice.
        /// </summary>
        public TestOnVirtualDeviceAttribute()
        {
        }
        #endregion
    }
}
