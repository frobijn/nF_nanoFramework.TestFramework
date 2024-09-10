// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace nanoFramework.TestFramework
{
    // Developer's note:
    // The interface uses methods for TargetName, Platform and IsRealHardware
    // because reflection in the nanoFramework does not support properties
    // at the time of writing. Reflection is needed for Tools.TestDeviceProxy.

    /// <summary>
    /// Access to selected properties and deployment configuration of the device
    /// that is available to run a tests on. 
    /// </summary>
    public interface ITestDevice
    {
        /// <summary>
        /// Target name that denotes the firmware installed on the device.
        /// </summary>
        string TargetName();

        /// <summary>
        /// Platform that describes the family the device belongs to.
        /// </summary>
        string Platform();

        /// <summary>
        /// Get the part of the deployment configuration identified by a key.
        /// </summary>
        /// <param name="configurationKey">Key as used in the deployment configuration</param>
        /// <param name="resultType">Required return type. Allowed types are <c>byte[]</c>, <c>int</c>, <c>long</c> and <c>string</c></param>
        /// <returns>Returns the deployment configuration identified by the <paramref name="configurationKey"/>. Returns <c>null</c>
        /// (-1 for integer types) if no configuration data is specified, if the deployment configuration cannot be presented as
        /// <paramref name="resultType"/> or if the <paramref name="configurationKey"/> is <c>null</c>.</returns>
        object GetDeploymentConfigurationValue(string configurationKey, Type resultType);
    }
}
