// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        /// The result is either a string value, if the configuration is specified as key = value pair,
        /// or the textual content of a configuration file.
        /// </summary>
        /// <param name="configurationKey"></param>
        /// <returns>Returns the content of a text file or a string value if the deployment configuration
        /// contains data for the <paramref name="configurationKey"/>. Returns <c>null</c> if no configuration
        /// data is specified.</returns>
        string GetDeploymentConfigurationValue(string configurationKey);

        /// <summary>
        /// Get the part of the deployment configuration identified by a key.
        /// The result is binary data if a file has been specified for the key in the deployment configuration.
        /// </summary>
        /// <param name="configurationKey"></param>
        /// <returns>Returns the content of a binary file if the deployment configuration has specified a file
        /// for the <paramref name="configurationKey"/>. Returns <c>null</c> otherwise.</returns>
        byte[] GetDeploymentConfigurationFile(string configurationKey);
    }
}
