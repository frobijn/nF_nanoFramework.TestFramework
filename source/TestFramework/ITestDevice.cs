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
        /// Get the content of a file that is stored on the device
        /// as an array of bytes.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Returns the content of the file, or <c>null</c> if the file does not exist
        /// or if the device does not support file storage.</returns>
        byte[] GetStorageFileContentAsBytes(string filePath);

        /// <summary>
        /// Get the content of a file that is stored on the device
        /// as a string
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>Returns the content of the file, or <c>null</c> if the file does not exist
        /// or if the device does not support file storage.</returns>
        string GetStorageFileContentAsString(string filePath);
    }
}
