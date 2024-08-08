// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Contains information about the implementation of the test framework
    /// on the nanoFramework platform.
    /// </summary>
    public sealed class TestFrameworkImplementation
    {
        internal PropertyInfo _property_IDataRow_MethodParameters;
        internal PropertyInfo _property_ITestMethod_CanBeRun;
        internal PropertyInfo _property_ITestOnRealHardware_Description;
        internal MethodInfo _method_ITestOnRealHardware_ShouldTestOnDevice;
        internal MethodInfo _method_ITestOnRealHardware_AreDevicesEqual;
        internal PropertyInfo _property_ITraits_Traits;
        internal Type _type_TestDeviceProxy;
    }
}
