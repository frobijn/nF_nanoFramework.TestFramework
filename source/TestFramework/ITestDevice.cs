// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace nanoFramework.TestFramework
{
    // The interface uses methods for TargetName, Platform and IsRealHardware
    // because reflection in the nanoFramework does not support properties
    // at the time of writing. Reflection is needed for Tools.TestDeviceProxy.

    /// <summary>
    /// Access to the properties of the device that can be used to run a test on.
    /// </summary>
    public interface ITestDevice
    {
        /// <summary>
        /// Target name.
        /// </summary>
        string TargetName();

        /// <summary>
        /// Target platform.
        /// </summary>
        string Platform();

        /// <summary>
        /// Get the content of a file that is stored on the device.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Returns the content of the file, or <c>null</c> if the file does not exist
        /// or if the device does not support file storage.</returns>
        byte[] GetStorageFileContent(string filePath);

#if !REFERENCED_IN_NFUNITMETADATA
        /// <summary>
        /// Indicates whether this device is a remote device that is different
        /// from the device the code is running on. If <c>false</c>, the attribute
        /// investigating the device can only use the information provided via this
        /// interface to decide whether a test can or should be executed on the device.
        /// If <c>true</c>, the attribute can also use the capabilities as reported by
        /// the nanoFramework runtime/firmware.
        /// </summary>
        bool IsRemoteDevice();
#endif
    }
}
